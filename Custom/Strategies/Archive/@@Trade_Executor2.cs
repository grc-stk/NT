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


    public class Trade_Executor2 : Strategy//, INotifyPropertyChanged
    {
        
        List<String> symbol_list;
        List<String> company_name_list;
        List<int> DaysSincePurchase;
        List<int> NumShares;
        Dictionary<string, int> symbol_to_index;
        List<String> scanner_results;
        private List<List<TradeTrigger>> trade_triggers = new List<List<TradeTrigger>>();


        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                symbol_list = new List<String> { "DUMMY" };
                company_name_list = new List<String> { "DUMMY" };
                DaysSincePurchase = new List<int> { -1 };
                NumShares = new List<int> { -1 };
                scanner_results = new List<String> { };
                symbol_to_index = new Dictionary<string, int>();

                LoadTickers(); //load symbols from file

                LoadTriggers(); //load trade triggers from file
                
                Description = "test";
                Name = "Trade_Executor2";
                
                StartDate = new DateTime(2018, 01, 01);
                EndDate = DateTime.Today;

                SimulateTrades = true;

                AmountPerTrade = 10000.0f;
                StopLossPercent = 5;  //use int so we can potentially change during optimization

                //Allows multiple stocks to be traded on same day
                EntryHandling = EntryHandling.UniqueEntries;

                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                //IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                
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
                    symbol_list.Add(values[0]);
                    company_name_list.Add(values[1]);
                    DaysSincePurchase.Add(-1);
                    NumShares.Add(-1);
                    symbol_to_index.Add(values[0], count);
                    count++;
                }
            }
            file2.Close();
        }
        
        protected void LoadTriggers()
        {
            for (int i = 0; i < symbol_list.Count; i++)
            {
                trade_triggers.Add(new List<TradeTrigger>());
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
                if (values.Length == 9)
                {
                    TradeTrigger trig = new TradeTrigger();
                    trig.Symbol = values[0];

                    IndicatorEnum newIndicator;
                    Enum.TryParse(values[1], out newIndicator);
                    string indicator_str = newIndicator.ToString().Substring(10, newIndicator.ToString().Length - 10);

                    trig.Indicator = newIndicator;
                    trig.StartRange = Convert.ToDouble(values[2]);
                    trig.EndRange = Convert.ToDouble(values[3]);
                    trig.DaysToHold = Convert.ToInt32(values[4]);
                    trig.Param1 = Convert.ToInt32(values[5]);
                    trig.Param2 = Convert.ToInt32(values[6]);
                    trig.Param3 = Convert.ToInt32(values[7]);
                    trig.Identifier = values[8];

                    int symbol_index = symbol_to_index[trig.Symbol];

                    trade_triggers[symbol_index].Add(trig);
                }
            }
            file4.Close();
        }

        protected override void OnBarUpdate()
        {
            int BarIndex = BarsInProgress;

            double indicator_value = 0.0;

            bool bFinishedProcessingAllData = false;

            if (((CurrentBar + 2) == Count) && BarIndex == (symbol_list.Count - 1))  //we are on the last bar to process
            {
                bFinishedProcessingAllData = true;
            }

            if ((BarIndex >= 1) && (BarIndex < (symbol_list.Count)))  //Start at index=1 since we want to ignore the primary/dummy instrument
            {
                DateTime date_to_process = Times[BarIndex][0].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                bool bWithinPeriod = (date_to_process >= StartDate) && (date_to_process <= EndDate);

                if (bWithinPeriod)
                {
                    string symbol_name = symbol_list[BarIndex];

                    double current_price = Closes[BarIndex][0];

                    List<TradeTrigger> triggers = trade_triggers[BarIndex];
                    for (int i = 0; i < triggers.Count; i++)
                    {
                        TradeTrigger trig = triggers[i];
                        string indicator_str = trig.Indicator.ToString().Substring(10, trig.Indicator.ToString().Length - 10);
                        string trade_id_str = indicator_str + " " + symbol_name;
                        string trade_exit_str = "Exit " + trade_id_str;

                        switch (trig.Indicator)
                        {
                            case IndicatorEnum.indicator_MACD:
                                indicator_value = MACD(BarsArray[BarIndex], trig.Param1, trig.Param2, trig.Param3)[0];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_RSI:
                                indicator_value = RSI(BarsArray[BarIndex], trig.Param1, trig.Param2)[0];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_BOLLINGER:
                                double upper_band = Bollinger(BarsArray[BarIndex], (double)trig.Param1, trig.Param2).Upper[0];  //get the indicator val from n days ago
                                double middle_band = Bollinger(BarsArray[BarIndex], (double)trig.Param1, trig.Param2).Middle[0];  //get the indicator val from n days ago
                                double lower_band = Bollinger(BarsArray[BarIndex], (double)trig.Param1, trig.Param2).Lower[0];  //get the indicator val from n days ago
                                double diff = current_price - middle_band;
                                double band_range = upper_band - middle_band;
                                indicator_value = diff / band_range; //how far current price is from the middle band (-1.0 means we're at the lower band, +1 means we're at the upper band)
                                break;
                            case IndicatorEnum.indicator_STOCHASTIC:
                                //use the "D" value
                                indicator_value = Stochastics(BarsArray[BarIndex], trig.Param1, trig.Param2, trig.Param3).D[0];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_STOCHASTIC_RSI:
                                indicator_value = StochRSI(BarsArray[BarIndex], trig.Param1)[0];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_GREG:
                                indicator_value = -999.999; // GregIndicator1(BarsArray[BarIndex], (float)Param1)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                                break;
                            default:
                                indicator_value = -999.99;
                                break;
                        }

                        bool bIndicatorInRange = false;
                        if ((indicator_value > trig.StartRange) && (indicator_value < trig.EndRange))
                        {
                            bIndicatorInRange = true;
                            String results_string = date_to_process.ToShortDateString() + " " + trig.Symbol + " " + indicator_str +
                                                    " " + trig.Identifier + " " + trig.StartRange.ToString() + " " + trig.EndRange.ToString() +
                                                    " " + trig.Param1.ToString() + " " + trig.Param2.ToString() + " " + trig.Param3.ToString();
                            scanner_results.Add(results_string);
                        }

                        if (SimulateTrades == true)
                        {

                            if (DaysSincePurchase[BarIndex] >= 0)  //Trade in-progress
                            {
                                DaysSincePurchase[BarIndex]++;
                                if (DaysSincePurchase[BarIndex] == trig.DaysToHold)
                                {
                                    ExitLong(BarIndex, NumShares[BarIndex], trade_exit_str, trade_id_str);
                                    DaysSincePurchase[BarIndex] = -1;
                                }
                            }
                            else if (bIndicatorInRange)
                            {
                                DaysSincePurchase[BarIndex] = 0;
                                NumShares[BarIndex] = (int)(AmountPerTrade / current_price);
                                EnterLong(BarIndex, NumShares[BarIndex], trade_id_str);
                            }
                        }
                    }
                }
            }

            if (bFinishedProcessingAllData)
            {
                WriteStringListToCSV(scanner_results, "c:\\temp\\knn\\scanner_results.txt");
            }
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


        #region Properties
        [Display(ResourceType = typeof(Custom.Resource), Name = "StartDate", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime StartDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "EndDate", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime EndDate { get; set; }

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

    }
        

        #endregion


   

}
