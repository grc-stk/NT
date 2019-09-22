//
// Copyright (C) 2018, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;

using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

using System.Threading;
#endregion




//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{


    public class Trade_Executor4 : Strategy//, INotifyPropertyChanged
    {
        
        List<String> symbol_list;
        List<String> company_name_list;
        List<String> Trade_Status;
        List<int> DaysSincePurchase;
        List<int> NumShares;
        Dictionary<string, int> symbol_to_index;
        List<String> scanner_results;
        private List<List<TradeTriggerByDate>> trade_triggers = new List<List<TradeTriggerByDate>>();


        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                symbol_list = new List<String> { "DUMMY" };
                company_name_list = new List<String> { "DUMMY" };
                Trade_Status = new List<String> { "DUMMY" };
                DaysSincePurchase = new List<int> { -1 };
                NumShares = new List<int> { -1 };
                scanner_results = new List<String> { };
                symbol_to_index = new Dictionary<string, int>();

                LoadTickers(); //load symbols from file

                LoadTriggers(); //load trade triggers from file
                
                Description = "test";
                Name = "Trade_Executor4";
                
                SimulateTrades = true;

                AmountPerTrade = 1000.0f;
                StopLossPercent = 5;  //use int so we can potentially change during optimization

                DaysToHold = 5;

                //Allows multiple stocks to be traded on same day
                EntryHandling = EntryHandling.UniqueEntries;

                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                //IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                //AddDataSeries(Data.BarsPeriodType.Tick, 1);

                for (int i = 0; i < symbol_list.Count; i++)
                {
                    //Don't add the dummy instrument (but we still need a placeholder list which is added below)
                    if (i != 0)
                        AddDataSeries(symbol_list[i], Data.BarsPeriodType.Day, 1);

                }

                SetStopLoss(CalculationMode.Percent, StopLossPercent/100.0f);

                // Sets a 20 tick trailing stop for an open position
                //SetTrailStop(CalculationMode.Ticks, 20);
            }
            else if (State == State.DataLoaded)
            {
                
            }
            else if ( ((State == State.Transition) || (State == State.Terminated)) )  //finished processing historical data (and ready for real-time)
            {
                
            }
        }

        protected void LoadTickers()
        {
            String sp500_csv_file_path = "c:\\temp\\knn\\sp500.csv";
            String sp500_config_file_path = "c:\\temp\\knn\\sp500_config_file.txt";
            System.IO.StreamReader file1 = new System.IO.StreamReader(sp500_config_file_path);
            String line1 = file1.ReadLine();
            if (line1 != null)
            {
                sp500_csv_file_path = line1;
            }
            file1.Close();

            String line2;

            int count = 1;  //skip first DUMMY item

            
            // Read the file and display it line by line.  
            System.IO.StreamReader file2 = new System.IO.StreamReader(sp500_csv_file_path);
            while ((line2 = file2.ReadLine()) != null)
            {
                string[] values = line2.Split(',');
                if (values.Length == 2)
                {
                    if (!symbol_list.Contains(values[0]))
                    {
                        symbol_list.Add(values[0]);
                        company_name_list.Add(values[1]);
                        Trade_Status.Add("Idle");
                        DaysSincePurchase.Add(-1);
                        NumShares.Add(-1);
                        symbol_to_index.Add(values[0], count);
                        count++;
                    }
                }
            }
            file2.Close();
        }
        
        protected void LoadTriggers()
        {
            for (int i = 0; i < symbol_list.Count; i++)
            {
                trade_triggers.Add(new List<TradeTriggerByDate>());
            }

            string trigger_file_path = "c:\\temp\\knn\\triggers.csv";
            String trigger_config_file_path = "c:\\temp\\knn\\triggers_config.txt";
            System.IO.StreamReader file3 = new System.IO.StreamReader(trigger_config_file_path);
            String line3 = file3.ReadLine();
            if (line3 != null)
            {
                trigger_file_path = line3;
            }
            file3.Close();
            
            System.IO.StreamReader file4 = new System.IO.StreamReader(trigger_file_path);
            string line4;
            while ((line4 = file4.ReadLine()) != null)
            {
                string[] values = line4.Split(';');
                if (values.Length == 3)
                {
                    TradeTriggerByDate trig = new TradeTriggerByDate();
                    trig.Symbol = values[0];
                    trig.TradeDate = Convert.ToDateTime(values[1]);
                    trig.DaysToHold = Convert.ToInt32(values[2]);
                    trig.Status = "NotExecuted";
                    
                    int symbol_index = symbol_to_index[trig.Symbol];

                    trade_triggers[symbol_index].Add(trig);
                }
            }
            file4.Close();
        }

        protected override void OnBarUpdate()
        {
            //if (BarsInProgress == 0)
            //{
                int BarIndex = BarsInProgress;

                bool bFinishedProcessingAllData = false;

                if (((CurrentBar + 1) == Count) && BarIndex == (symbol_list.Count - 1))  //we are on the last bar to process
                {
                    bFinishedProcessingAllData = true;
                }

                if ((BarIndex >= 1) && (BarIndex < (symbol_list.Count)))  //Start at index=1 since we want to ignore the primary/dummy instrument
                {
                    DateTime date_to_process = Times[BarIndex][0].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                    string symbol_name = symbol_list[BarIndex];
                    double current_price = Closes[BarIndex][0];

                    List<TradeTriggerByDate> triggers = trade_triggers[BarIndex];
                    for (int i = 0; i < triggers.Count; i++)
                    {
                        TradeTriggerByDate trig = triggers[i];
                        string trade_id_str = symbol_name;
                        string trade_exit_str = "Exit " + trade_id_str;

                        //if (DaysSincePurchase[BarIndex] >= 0)  //Trade in-progress
                        if (trig.Status == "InProgress")
                        {
                            if ((date_to_process > trig.TradeDate)) // && (date_to_process <= trig.TradeDate.AddDays(trig.DaysToHold)))
                            {
                                DaysSincePurchase[BarIndex]++;


                                if (DaysSincePurchase[BarIndex] == DaysToHold) //Use input parameter rather than reading from file trig.DaysToHold //Set Exit for end of nth day 
                                {
                                    trig.Status = "Completed";
                                    Trade_Status[BarIndex] = "Idle";
                                    ExitLong(BarIndex, NumShares[BarIndex], trade_exit_str, trade_id_str);
                                    DaysSincePurchase[BarIndex] = -1;
                                }
                            }

                        }
                        //else if ((trig.TradeDate == date_to_process) && (Trade_Status[BarIndex] != "InProgress"))  //prevent multiple entries for same symbol
                        else if ((trig.TradeDate == date_to_process) && (Trade_Status[BarIndex] != "InProgress"))  //prevent multiple entries for same symbol
                        {
                            if (current_price < AmountPerTrade)  //if price is greater than max per trade, then we cannot purchase
                            {
                                Trade_Status[BarIndex] = "InProgress";
                                DaysSincePurchase[BarIndex] = 0;
                                trig.Status = "InProgress";
                                NumShares[BarIndex] = (int)(AmountPerTrade / current_price);
                                EnterLong(BarIndex, NumShares[BarIndex], trade_id_str);
                                scanner_results.Add(trig.Symbol + " " + trig.TradeDate.ToShortDateString() + " " + DaysToHold.ToString());
                            }
                        }
                    }
                }

                if (bFinishedProcessingAllData)
                {
                    WriteStringListToCSV(scanner_results, "c:\\temp\\knn\\scanner_results.txt");
                }
            //}
        }

        protected void WriteStringListToCSV(List<String> list_to_write, String filePath)
        {
            int count = list_to_write.Count;

            var csv = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                csv.AppendLine(list_to_write[i]);
            }

            //mut.WaitOne(); //wait until safe to enter; prior thread has completed writing.
            try
            {
                File.AppendAllText(filePath, csv.ToString());
                //mut.ReleaseMutex();
            }
            catch (System.Exception exp)
            {
                Log("File write error for file name '" + filePath + "' Error '" + exp.Message + "'", LogLevel.Warning);
            }

        }


        [Range(1, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Amount Per Trade", GroupName = "NinjaScriptStrategyParameters", Order = 2)]
        public float AmountPerTrade
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Stop Loss %", GroupName = "NinjaScriptStrategyParameters", Order = 3)]
        public int StopLossPercent
        { get; set; }

        [TypeConverter(typeof(FriendlyBoolConverter))] // Converts the bool to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
        [Display(Name = "Simulate Trades", GroupName = "NinjaScriptStrategyParameters", Order = 4)]
        public bool SimulateTrades
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Days to Hold", GroupName = "NinjaScriptStrategyParameters", Order = 5)]
        public int DaysToHold
        { get; set; }

    }
        

    


   

}
