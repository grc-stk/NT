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


    public class Trade_Scanner6 : Strategy//, INotifyPropertyChanged
    {
              		
        private static Random rnd;
        static Trade_Scanner6()
        {
            rnd = new Random();
        }

        private static Mutex mut_python = new Mutex(false, "Python");

        NT nt = new NT();

        string description = "";
        String Strategy_Description = "";

        List<String> symbol_list;
        List<String> company_name_list;

        List<Date_Price_Pair> SP500_historical_price_list = new List<Date_Price_Pair>();

        List<String> Trade_Status;
        List<int> DaysSincePurchase;
        List<int> NumShares;

        private double Buy_Price = 0.0;
        private double BarBought = 0;

        private Order entryOrder = null;

        public List<TradeScannerResult> trade_scanner_results = new List<TradeScannerResult>();

        public List<TradeTriggerByDate> trade_triggers = new List<TradeTriggerByDate>();

        public List<List<List<Indicator_FutureValueChange_Pair>>> master_list_training = new List<List<List<Indicator_FutureValueChange_Pair>>>();
        public List<List<List<Indicator_FutureValueChange_Pair>>> master_list_testing = new List<List<List<Indicator_FutureValueChange_Pair>>>();

        public List<IndicatorEnum> indicator_list = new List<IndicatorEnum>() { IndicatorEnum.indicator_BOLLINGER };

        List<int> strategies_to_check = new List<int>();

        private int count = 0;

        private bool bDataLoaded = false;
        private bool bFirst = true;
        private bool bExecutedPython = false;

        String my_id = "default";
        String uid = "";
        String indicator = "";
        String master_symbol_name = "";
        String output_folder_with_date = "";
        String results_folder = "";
        DateTime TrainingStartDate = DateTime.Today;
        DateTime TrainingEndDate = DateTime.Today;
       //DateTime TestingStartDate =  DateTime.Today;

        int Param1 = -1;
        int Param2 = -1;
        int Param3 = -1;
       
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                if (bFirst)
                {
                    bFirst = false;
                }

                symbol_list = new List<String> { "DUMMY", "^SP500" };
                company_name_list = new List<String> { "DUMMY", "S&P 500" };

                Trade_Status = new List<String> { "DUMMY", "DUMMY"};
                DaysSincePurchase = new List<int> { -1, -1 };
                NumShares = new List<int> { -1, -1 };

                output_folder = "C:\\temp\\knn\\";

                LoadTickers(); //load symbols from file

                
                Description = "test";
                Name = "Trade_Scanner6";
                Calculate = Calculate.OnEachTick;  //This will let us get the last Bar
                
                TestingStartDate = DateTime.Today;
                TestingEndDate = DateTime.Today;


                //Trading params
                SimulateTrades = true;
                AmountPerTrade = 500.0f;
                StopLossPercent = 5;  //use int so we can potentially change during optimization
                DaysToHold = 5;

                //Allows multiple stocks to be traded on same day
                EntryHandling = EntryHandling.UniqueEntries;

                //Custom params
                uid = "";
                indicator = "";

                master_symbol_name = "";

                bDataLoaded = false;

                BOLL_param_string = "2;14;-1";

                Strategies_string = "7;12;16;31;34";

                Strategy_Num = 1;



                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                //IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                //Set our stop loss here
                SetStopLoss(CalculationMode.Percent, StopLossPercent / 100.0f);

                //System.Windows.Forms.MessageBox.Show("Configure");
                bDataLoaded = false;

                for (int i = 0; i < symbol_list.Count; i++)
                {
                    //Don't add the dummy instrument (but we still need a placeholder list which is added below)
                    if (i != 0)
                        AddDataSeries(symbol_list[i], Data.BarsPeriodType.Day, 1);
                }

                for (int i = 0; i < indicator_list.Count; i++)
                {
                    master_list_training.Add(new List<List<Indicator_FutureValueChange_Pair>>());
                    master_list_testing.Add(new List<List<Indicator_FutureValueChange_Pair>>());
                    for (int j = 0; j < symbol_list.Count; j++)
                    {
                        master_list_training[i].Add(new List<Indicator_FutureValueChange_Pair>());
                        master_list_testing[i].Add(new List<Indicator_FutureValueChange_Pair>());
                    }
                }

                
            }
            else if (State == State.DataLoaded)
            {
                //master_symbol_name = Instrument.MasterInstrument.Name;

                bDataLoaded = true;

                TrainingStartDate = TestingEndDate.AddYears(-1);
                TrainingEndDate = TestingEndDate.AddDays(-91);
                //TestingStartDate = TestingEndDate.AddDays(-90);

                output_folder_with_date = output_folder + DateTime.Today.ToString("ddMMMyyyy") + "\\";

                DirectoryInfo di1 = Directory.CreateDirectory(output_folder_with_date);

                //3-2d-10%-01Jan2018-01Jul2018

                Strategy_Description = Strategy_Num.ToString() + "-" + DaysToHold.ToString() + "d-" + StopLossPercent.ToString() + "%-" +
                                         TestingStartDate.ToString("ddMMMyyyy") + "-" + TestingEndDate.ToString("ddMMMyyyy");

                if (SimulateTrades == true)
                {
                    results_folder = output_folder_with_date + Strategy_Description;
                }
                else
                {
                    String output_folder_for_scanner_results = output_folder + "scanner_results\\" + DateTime.Today.ToString("ddMMMyyyy") + "\\" + Strategies_string.Replace(';', '-');
                    results_folder = output_folder_for_scanner_results;
                }

                if (SimulateTrades == true)
                {
                    strategies_to_check.Add(Strategy_Num);
                }
                else
                {
                    List<int> strategyList = new List<int>();
                    strategyList = PopulateListOfStrategies();
                    strategies_to_check = strategyList;
                }

            }
            else if ((bDataLoaded == true) && ((State == State.Transition) || (State == State.Terminated)))  //finished processing historical data (and ready for real-time)
            {
                
            }
        }

        protected void LoadTickers()
        {
            String sp500_csv_file_path = output_folder + "sp500.csv";
            String sp500_config_file_path = output_folder + "sp500_config_file.txt";
            System.IO.StreamReader file1 = new System.IO.StreamReader(sp500_config_file_path);
            String line1 = file1.ReadLine();
            if (line1 != null)
            {
                sp500_csv_file_path = line1;
            }
            file1.Close();

            String line2;

           
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
                    }
                }
            }
            file2.Close();
        }
        private void CallPythonScript(string script_file_path, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            //string Python_Path = "C:/Python27/python.exe";
            string Python_Path = "C:/ProgramData/Anaconda3/python.exe";
            start.FileName = "cmd.exe";
            start.Arguments = "/C " + Python_Path + string.Format(" {0} {1}", script_file_path, args) + " >>" + output_folder_with_date + "python_log.txt 2>&1";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }
            }
        }

        protected void WriteListToCSV(List<Indicator_FutureValueChange_Pair> list_to_write, String filePath)
        {
            int count = list_to_write.Count;

            var csv = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                String date = list_to_write[i].Date.ToShortDateString();
                float price = list_to_write[i].Price;
                float indicator_value = list_to_write[i].Indicator;
                float future_value_change = list_to_write[i].FutureValueChange;
                float SP500_price = list_to_write[i].SP500_Price;

                var newLine = string.Format("{0},{1},{2},{3},{4}", date, price, indicator_value, future_value_change, SP500_price);

                csv.AppendLine(newLine);
            }

            //mut.WaitOne(); //wait until safe to enter; prior thread has completed writing.
            try
            {
                File.WriteAllText(filePath, csv.ToString());
                //mut.ReleaseMutex();
            }
            catch (System.Exception exp)
            {
                Log("File write error for file name '" + filePath + "' Error '" + exp.Message + "'", LogLevel.Warning);
            }




        }

        
        public List<int> ParseParamString(string input_string)
        {
            string[] string_array = input_string.Split(';');
            List<int> param_list = new List<int>();
            for (int i = 0; i < string_array.Length; i++)
            {
                param_list.Add(Convert.ToInt32(string_array[i]));
            }

            return param_list;
        }

        public void PopulateParamatersForIndicator(IndicatorEnum this_ind)
        {
            String param_string = "-1;-1;-1";

            switch (this_ind)
            {
                case IndicatorEnum.indicator_BOLLINGER:
                    param_string = BOLL_param_string;
                    break;
                default:
                    break;
            }

            List<int> param_list = ParseParamString(param_string);
            if (param_list.Count == 3)
            {
                Param1 = param_list[0];
                Param2 = param_list[1];
                Param3 = param_list[2];
            }
        }

        public List<int> PopulateListOfStrategies()
        {
            String param_string = Strategies_string;

            List<int> param_list = ParseParamString(param_string);
            return (param_list);
        }

        protected double PctChange(int bar_index, int days_ago)
        {
            double pct_change = 0.0;
            try
            {
                pct_change = (((Closes[bar_index][0] / Closes[bar_index][days_ago]) - 1.0) * 100.0);
            }
            catch
            {

            }
            
            
            return pct_change;
        }

        public double PctChangeOnDay(int index, int day)
        {
            double pct_change_on_day = 0.0;
            try
            {
                pct_change_on_day = (((Closes[index][day] / Closes[index][day+1]) - 1.0) * 100.0);
            }
            catch
            {

            }

            return pct_change_on_day;
        }

        protected double ComputeBollinger(int bar_index, int days_ago)
        {
            double upper_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Upper[days_ago];  //get the indicator val from n days ago
            double middle_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Middle[days_ago];  //get the indicator val from n days ago
            double lower_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Lower[days_ago];  //get the indicator val from n days ago
            double current_price = Closes[bar_index][days_ago];
            double diff = current_price - middle_band;
            double band_range = upper_band - middle_band;
            double indicator_value = diff / band_range; //how far current price is from the middle band (-1.0 means we're at the lower band, +1 means we're at the upper band)

            return indicator_value;
        }

        protected double GetPercentOfGreenDays(int bar_index, int num_days)
        {
            int green_day_count = 0;
            int total_day_count = 0;

            for (int i=0; i<num_days; i++)
            {
                double open_price = Opens[bar_index][i];
                double close_price = Closes[bar_index][i];
                if (close_price > open_price)
                {
                    green_day_count++;
                }

                total_day_count++;
            }

            double pct_green = Convert.ToDouble(green_day_count) / Convert.ToDouble(total_day_count) * 100.0;

            return pct_green;
        }

        protected void WriteTradeScannerResults()
        {
            int count = trade_scanner_results.Count;

            var csv = new StringBuilder();
            //csv.AppendLine("Date,Sym,Co.,Open,Close,%Change,SP500%Change,CompSP500,%Change-1Day, %Change-2Day,%Change-5Day," +
            //               "%Change-10Day,%Change-15Day,%Change-20Day,CompSP500-1Day,CompSP500-2Day,CompSP500-5Day,CompSP500-10Day," +
            //               "CompSP500-15Day,CompSP500-20Day,IndName,IndToday,IndChange-1Day,IndChange-2Day,IndChange-5Day," +
            //               "IndChange-10Day,IndChange-15Day,IndChange-20Day,RSI,Strategy,P,SPP,Diff,Avg,Median,Low,High,%Good,%Bad");
            //csv.AppendLine("Date,Sym,Co.,O,C,%Change,SPchg,CompSP,%Change-1,%Change-2,%Change-5,%Change-10,%Change-15,%Change-20," +
            //               "Comp-1,Comp-2,Comp-5,Comp-10,Comp-15,Comp-20,IndName,IndToday,IndChange-1,IndChange-2,IndChange-5," +
            //               "IndChange-10,IndChange-15,IndChange-20,RSI,Strategy,P,SPP,Diff,NumTr,PrDoll,DollPerTr,Avg,Median,Low,High," +
            //               "%Good,%Bad");
            csv.AppendLine("Date,Sym,Co.,StrategyMatches,O,C,%Change,SPchg,CompSP," +
                           "%Change-1Ago,%Change-2Ago,%Change-3Ago,%Change-4Ago,%Change-5Ago," +
                           "SP%Change-1Ago,SP%Change-2Ago,SP%Change-3Ago,SP%Change-4Ago,SP%Change-5Ago," +
                           "%Change-1,%Change-2,%Change-5,%Change-10,%Change-15,%Change-20," +
                           "SP%Change-1,SP%Change-2,SP%Change-5,SP%Change-10,%SPChange-15,SP%Change-20," +
                           "Comp-1,Comp-2,Comp-5,Comp-10,Comp-15,Comp-20,IndName,IndToday,IndChange-1,IndChange-2,IndChange-5," +
                           "IndChange-10,IndChange-15,IndChange-20,RSI,MACD,STOCH,STOCH_RSI,Strategy,OrderDelta,B,S,P,SPP,Diff,NumTr,PrDoll,DollPerTr,Avg,Median,Low,High," +
                           "%Good,%Bad");

            for (int i = 0; i < count; i++)
            {
                TradeScannerResult res = trade_scanner_results[i];

                var newLine = string.Format("{0},{1},{2},{3},{4},{5:0.00},{6:0.00},{7:0.00},{8:0.00}",
                                            res.Date.ToShortDateString(),
                                            res.Symbol,
                                            res.Description,
                                            res.StrategyMatches,
                                            res.Open,
                                            res.Close,
                                            res.PercentChange_Today,
                                            res.SP500_PercentChange_Today,
                                            res.Compare_SP500_Today);

                newLine = newLine + string.Format(",{0:0.00},{1:0.00},{2:0.00},{3:0.00},{4:0.00}",
                                           res.PercentChange_1DayAgo,
                                           res.PercentChange_2DayAgo,
                                           res.PercentChange_3DayAgo,
                                           res.PercentChange_4DayAgo,
                                           res.PercentChange_5DayAgo);

                newLine = newLine + string.Format(",{0:0.00},{1:0.00},{2:0.00},{3:0.00},{4:0.00}",
                                            res.SP_PercentChange_1DayAgo,
                                            res.SP_PercentChange_2DayAgo,
                                            res.SP_PercentChange_3DayAgo,
                                            res.SP_PercentChange_4DayAgo,
                                            res.SP_PercentChange_5DayAgo);

                newLine = newLine + string.Format(",{0:0.00},{1:0.00},{2:0.00},{3:0.00},{4:0.00},{5:0.00}",
                                            res.PercentChange_1Day,
                                            res.PercentChange_2Day,
                                            res.PercentChange_5Day,
                                            res.PercentChange_10Day,
                                            res.PercentChange_15Day,
                                            res.PercentChange_20Day);

                newLine = newLine + string.Format(",{0:0.00},{1:0.00},{2:0.00},{3:0.00},{4:0.00},{5:0.00}",
                                            res.SP_PercentChange_1Day,
                                            res.SP_PercentChange_2Day,
                                            res.SP_PercentChange_5Day,
                                            res.SP_PercentChange_10Day,
                                            res.SP_PercentChange_15Day,
                                            res.SP_PercentChange_20Day);

                newLine = newLine + string.Format(",{0:0.00},{1:0.00},{2:0.00},{3:0.00},{4:0.00},{5:0.00}",
                                            res.Compare_SP_1Day,
                                            res.Compare_SP_2Day,
                                            res.Compare_SP_5Day,
                                            res.Compare_SP_10Day,
                                            res.Compare_SP_15Day,
                                            res.Compare_SP_20Day);

                newLine = newLine + string.Format(",{0},{1:0.00},{2:0.00},{3:0.00},{4:0.00},{5:0.00},{6:0.00},{7:0.00},{8:0.00},{9:0.00},{10:0.00},{11:0.00},{12}",
                                            res.Indicator_Name,
                                            res.Indicator_Today,
                                            res.Indicator_Change_1Day,
                                            res.Indicator_Change_2Day,
                                            res.Indicator_Change_5Day,
                                            res.Indicator_Change_10Day,
                                            res.Indicator_Change_15Day,
                                            res.Indicator_Change_20Day,
                                            res.RSI,
                                            res.MACD,
                                            res.STOCHASTIC,
                                            res.STOCH_RSI,
                                            res.Strategy_Name);

            
                csv.AppendLine(newLine);
            }

            //mut.WaitOne(); //wait until safe to enter; prior thread has completed writing.
            try
            {
                String file_name = results_folder + "scanner_results.csv"; // _strategy" + Strategy_Num.ToString() + "_" + TestingStartDate.ToString("ddMMMyyyy") + "to" + TestingEndDate.ToString("ddMMMyyyy") + "_" + uid + ".csv";
                File.WriteAllText(file_name, csv.ToString());
                //mut.ReleaseMutex();
            }
            catch (System.Exception exp)
            {
                Log("File write error for file name '" + "' Error '" + exp.Message + "'", LogLevel.Warning);
            }

        }

        protected void WriteSP500HistoricalData()
        {
            int count = SP500_historical_price_list.Count;

            var csv = new StringBuilder();
            csv.AppendLine("Date,P");

            for (int i = 0; i < count; i++)
            {
                Date_Price_Pair dp = SP500_historical_price_list[i];

                var newLine = string.Format("{0},{1:0.00}", dp.Date.ToShortDateString(), dp.Price);
                 
                csv.AppendLine(newLine);
            }

            //mut.WaitOne(); //wait until safe to enter; prior thread has completed writing.
            try
            {
                String file_name = results_folder + "sp.csv"; 
                File.WriteAllText(file_name, csv.ToString());
                //mut.ReleaseMutex();
            }
            catch (System.Exception exp)
            {
                Log("File write error for file name '" + "' Error '" + exp.Message + "'", LogLevel.Warning);
            }

        }

        protected void WriteTradeTriggers()
        {
            int count = trade_triggers.Count;

            var csv = new StringBuilder();
            
            for (int i = 0; i < count; i++)
            {
                TradeTriggerByDate res = trade_triggers[i];

                var newLine = string.Format("{0};{1};{2}",
                                            res.Symbol,
                                            res.TradeDate.ToShortDateString(),
                                            res.DaysToHold);

                csv.AppendLine(newLine);
            }

            //mut.WaitOne(); //wait until safe to enter; prior thread has completed writing.
            try
            {
                String file_name = output_folder + "triggers_strategy" + Strategy_Num.ToString() + "_" + TestingStartDate.ToString("ddMMMyyyy") + "to" + TestingEndDate.ToString("ddMMMyyyy") + "_" + uid + ".csv";
                File.WriteAllText(file_name, csv.ToString());
                //mut.ReleaseMutex();
            }
            catch (System.Exception exp)
            {
                Log("File write error for file name '" + "' Error '" + exp.Message + "'", LogLevel.Warning);
            }

        }


        protected override void OnBarUpdate()
        {
            int BarIndex = BarsInProgress;

            bool bFinishedProcessingAllData = false;

            Boolean bLastSymbol = BarIndex == (symbol_list.Count - 1);

            DateTime current_bar_date = Times[BarIndex][0].Date;

            if (bLastSymbol &&
                (((CurrentBar + 1) == Count) || (current_bar_date == TestingEndDate)))  //we are on the last bar to process
            {
                Debug.WriteLine("bFinished trigger");
                bFinishedProcessingAllData = true;
            }

            String debug_txt1 = "Date=" + Times[BarIndex][0].Date.ToShortDateString() + "BarIndex=" + BarIndex.ToString() + "CurrentBar=" + CurrentBar.ToString() + "Count=" +Count.ToString();
            Debug.WriteLine(debug_txt1);

            //NOTE: Index 1 is always S&P 500
            if ( (BarIndex >= 1/*1*/) && (BarIndex < (symbol_list.Count)) )  //Start at index=1 since we want to ignore the primary/dummy instrument
            {
                DateTime date_to_process = Times[BarIndex][0].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                if ((date_to_process >= TestingStartDate) && (date_to_process <= TestingEndDate))//  && (CurrentBar >= (20)))  //give 20 days of buffer
                {
                    int SP_index = 1;
                    if (BarIndex == SP_index)
                    {
                        Date_Price_Pair date_price = new Date_Price_Pair();
                        date_price.Date = date_to_process;
                        date_price.Price = Closes[BarIndex][0];
                        SP500_historical_price_list.Add(date_price);

                    }
                    else  //non S&P500 symbols
                    {
                        TradeScannerResult scan_result = new TradeScannerResult();

                        double OpenPriceToday = Opens[BarIndex][0];
                        double ClosePriceToday = Closes[BarIndex][0];
                        double SP500_Open = Opens[SP_index][0];
                        double SP500_Close = Closes[SP_index][0];

                        try
                        {
                            scan_result.Date = date_to_process;
                            scan_result.Symbol = symbol_list[BarIndex];
                            scan_result.Description = company_name_list[BarIndex];
                            scan_result.Open = OpenPriceToday;
                            scan_result.Close = ClosePriceToday;
                            scan_result.PercentChange_Today = (((ClosePriceToday / OpenPriceToday) - 1.0) * 100.0);
                            scan_result.SP500_PercentChange_Today = (((SP500_Close / SP500_Open) - 1.0) * 100.0);
                            scan_result.Compare_SP500_Today = scan_result.PercentChange_Today - scan_result.SP500_PercentChange_Today;

                            scan_result.PercentChange_1Day = PctChange(BarIndex, 1);
                            scan_result.PercentChange_2Day = PctChange(BarIndex, 2);
                            scan_result.PercentChange_5Day = PctChange(BarIndex, 5);
                            scan_result.PercentChange_10Day = PctChange(BarIndex, 10);
                            scan_result.PercentChange_15Day = PctChange(BarIndex, 15);
                            scan_result.PercentChange_20Day = PctChange(BarIndex, 20);

                            scan_result.PercentChange_1DayAgo = PctChangeOnDay(BarIndex, 1);
                            scan_result.PercentChange_2DayAgo = PctChangeOnDay(BarIndex, 2);
                            scan_result.PercentChange_3DayAgo = PctChangeOnDay(BarIndex, 3);
                            scan_result.PercentChange_4DayAgo = PctChangeOnDay(BarIndex, 4);
                            scan_result.PercentChange_5DayAgo = PctChangeOnDay(BarIndex, 5);

                            scan_result.SP_PercentChange_1Day = PctChange(SP_index, 1);
                            scan_result.SP_PercentChange_2Day = PctChange(SP_index, 2);
                            scan_result.SP_PercentChange_5Day = PctChange(SP_index, 5);
                            scan_result.SP_PercentChange_10Day = PctChange(SP_index, 10);
                            scan_result.SP_PercentChange_15Day = PctChange(SP_index, 15);
                            scan_result.SP_PercentChange_20Day = PctChange(SP_index, 20);

                            scan_result.SP_PercentChange_1DayAgo = PctChangeOnDay(SP_index, 1);
                            scan_result.SP_PercentChange_2DayAgo = PctChangeOnDay(SP_index, 2);
                            scan_result.SP_PercentChange_3DayAgo = PctChangeOnDay(SP_index, 3);
                            scan_result.SP_PercentChange_4DayAgo = PctChangeOnDay(SP_index, 4);
                            scan_result.SP_PercentChange_5DayAgo = PctChangeOnDay(SP_index, 5);

                            scan_result.Compare_SP_1Day = scan_result.PercentChange_1Day - scan_result.SP_PercentChange_1Day;
                            scan_result.Compare_SP_2Day = scan_result.PercentChange_2Day - scan_result.SP_PercentChange_2Day;
                            scan_result.Compare_SP_5Day = scan_result.PercentChange_5Day - scan_result.SP_PercentChange_5Day;
                            scan_result.Compare_SP_10Day = scan_result.PercentChange_10Day - scan_result.SP_PercentChange_10Day;
                            scan_result.Compare_SP_15Day = scan_result.PercentChange_15Day - scan_result.SP_PercentChange_15Day;
                            scan_result.Compare_SP_20Day = scan_result.PercentChange_20Day - scan_result.SP_PercentChange_20Day;

                            scan_result.Green_5Day = GetPercentOfGreenDays(BarIndex, 5);
                            scan_result.Green_10Day = GetPercentOfGreenDays(BarIndex, 10);
                            scan_result.Green_15Day = GetPercentOfGreenDays(BarIndex, 15);
                            scan_result.Green_20Day = GetPercentOfGreenDays(BarIndex, 20);


                            PopulateParamatersForIndicator(IndicatorEnum.indicator_BOLLINGER);
                            //Indicator_FutureValueChange_Pair indicator_pair = GetIndicatorValue(indicator_list[i], BarIndex, 0, date_to_process);

                            double indicator_value_today = 0.0;

                            scan_result.Indicator_Name = "BOLL";
                            scan_result.Strategy_Name = Strategy_Description; // Strategy_Num.ToString();

                            double boll_0 = ComputeBollinger(BarIndex, 0);
                            double boll_1 = ComputeBollinger(BarIndex, 1);
                            double boll_2 = ComputeBollinger(BarIndex, 2);
                            double boll_3 = ComputeBollinger(BarIndex, 3);
                            double boll_4 = ComputeBollinger(BarIndex, 4);
                            double boll_5 = ComputeBollinger(BarIndex, 5);
                            double boll_10 = ComputeBollinger(BarIndex, 10);
                            double boll_15 = ComputeBollinger(BarIndex, 15);
                            double boll_20 = ComputeBollinger(BarIndex, 20);

                            indicator_value_today = boll_0;
                            scan_result.Indicator_Today = indicator_value_today;

                            double boll_0_chg = boll_1 - boll_0;

                            double boll_min = -1.0;
                            double boll_max = -0.75;
                            bool boll_in_range = (boll_0 > boll_min) && (boll_0 < boll_max);
                            double boll_min_change = 0.05;
                            double boll_max_change = 0.25;
                            bool boll_change_in_range = (boll_0_chg > boll_min_change) && (boll_0_chg < boll_max_change);
                            bool boll_declining_3dys = (boll_3 > boll_2) && (boll_2 > boll_1) && (boll_0 > boll_1);

                            scan_result.Indicator_Change_1Day = indicator_value_today - boll_1;
                            scan_result.Indicator_Change_2Day = indicator_value_today - boll_2;
                            scan_result.Indicator_Change_5Day = indicator_value_today - boll_5;
                            scan_result.Indicator_Change_10Day = indicator_value_today - boll_10;
                            scan_result.Indicator_Change_15Day = indicator_value_today - boll_15;
                            scan_result.Indicator_Change_20Day = indicator_value_today - boll_20;

                            double RSI_val = RSI(BarsArray[BarIndex], 14, 3)[0];  //get the indicator val from n days ago
                            scan_result.RSI = RSI_val;

                            double STOCHASTIC_val = Stochastics(BarsArray[BarIndex], 7, 14, 3).D[0];
                            scan_result.STOCHASTIC = STOCHASTIC_val;

                            double STOCHASTIC_RSI_val = StochRSI(BarsArray[BarIndex], 14)[0];
                            scan_result.STOCH_RSI = STOCHASTIC_RSI_val;

                            double MACD_val = MACD(BarsArray[BarIndex], 12, 26, 9)[0];
                            scan_result.MACD = MACD_val;

                            bool bBuy = false;

                            int num_matching_strategies = 0;
                            for (int i = 0; i < strategies_to_check.Count; i++)
                            {
                                int strat_num = strategies_to_check[i];
                                bBuy = false;

                                switch (strat_num)
                                {
                                    case 1: //declining boll for 3, then up spike
                                        if (boll_in_range &&
                                            boll_change_in_range &&
                                            boll_declining_3dys)
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 2:  //boll up spike
                                        if (boll_in_range &&
                                            boll_change_in_range)
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 3:  //boll crossing -1 threshold, but not too far
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 4:  //boll in range, today's change < -0.5% and SP500 change > 0%
                                        if (boll_in_range &&
                                            (scan_result.PercentChange_Today < -0.5) &&
                                            (scan_result.SP500_PercentChange_Today > 0.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 5:  //strat3 + Today 0.75-1.25 and SP500Change 0.5 - 1.0
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.75, 1.25) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 1.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 6:  //boll up spike + Today < -1.5 + SP500Change < -0.75
                                        if (boll_in_range &&
                                            boll_change_in_range &&
                                            (scan_result.PercentChange_Today < -1.5) &&
                                            (scan_result.SP500_PercentChange_Today < -0.75))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 7:  //strat3 + Today 0.5-2.0 and SP500Change 0.5 - 3.0
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 8:  //strat7 + 5-day indicator change -1.5 to -0.5
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0) &&
                                            nt.InRange(scan_result.Indicator_Change_5Day, -1.5, -0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 9:  //strat7 but use 0.0 for min of %ChangeToday and SP500ChangeToday
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.0, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.0, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 10:  //strat7 but remove SP500%Change condition
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 11:  //strat7 but change boll range max to -0.5
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 12:  //strat7 but change boll range min/max to -0.95/-0.75
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.95, -0.75) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 13:  //strat12 but also add condition for CompSP500_today > 0.15
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.95, -0.75) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0) &&
                                            (scan_result.Compare_SP500_Today > 0.15))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 14:  //Just testing SP500%change range
                                        if (nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 15: //strat7, but use 0.0 for SP500 min
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.0, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 16: //strat7, but use -1.0 for boll_1 min and -0.5 for boll_0 max
                                        if ((boll_1 < -1.0) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 17: //strat7 but but use -1.0 for boll_1 min and -0.5 for boll_0 max, use 0.25 for today min, and remove SP500 change
                                        if ((boll_1 < -1.0) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.25, 2.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 18: //strat7 but but use -1.2 for boll_1 min and -0.95 for boll_0 max, use 0.25 for today min, and remove SP500 change
                                        if ((boll_1 < -1.2) &&
                                            nt.InRange(boll_0, -0.95, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.25, 2.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 19:  //just dropped to range of -1.0 to -0.8  
                                        if ((boll_1 > -0.5) &&
                                            nt.InRange(boll_0, -1.0, -0.8))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 20:  //recovery from below -0.9
                                        if ((boll_1 < -0.9) &&
                                            nt.InRange(boll_0, -0.7, -0.3))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 21:  //drop 1 d ago, bump today
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 22: //drop 2 d ago, bump today
                                        if ((boll_2 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -0.5, 0.5) &&
                                            nt.InRange(scan_result.SP_PercentChange_2DayAgo, -2.0, -0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 23: //drop 3 d ago, bump today
                                        if ((boll_3 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -0.5, 0.5) &&
                                            nt.InRange(scan_result.SP_PercentChange_2DayAgo, -0.5, 0.5) &&
                                            nt.InRange(scan_result.SP_PercentChange_3DayAgo, -2.0, -0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 24:  //boll 2 days ago > -0.9, boll 1 day ago < -1.1, boll today > -0.9
                                        if ((boll_2 > -0.9) &&
                                            (boll_1 < -1.1) &&
                                            (boll_0 > -0.9))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 25:  //today % change > 0.5 (really just to get data points)
                                        if (scan_result.PercentChange_Today > 0.5)
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 26:  //yesterday % change < -0.5, today % change > 0.5
                                        if ((scan_result.PercentChange_1DayAgo < -0.5) &&
                                            (scan_result.PercentChange_Today > 0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 27:  //21 mod to -0.9 to -0.5
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.5, 3.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 28:  //27 mod max Pct change 2.0 -> 5.0
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 3.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 29:  //28 mod max SP500 change today to 5.0
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.5))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 30:  //29 mod max SP500 change yesterday to -0.25
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.25))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 31:  //30 mod min SP500 change yesterday to -5.0
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -5.0, -0.25))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 32:  //31 mod min SP500 change yesterday to -3.5
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -3.5, -0.25))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 33:  //30 mod min PctChange today to 0.25 
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.25))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 34:  //33 mod min boll_1 min to -1.0
                                        if ((boll_1 < -1.0) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.25))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 35:  //34 mod add check for SP%5day negative
                                        if ((boll_1 < -1.0) &&
                                            nt.InRange(boll_0, -0.9, -0.5) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.25) &&
                                            (scan_result.SP_PercentChange_5Day < 0.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 36:  //34 mod min boll_0 max to -0.0
                                        if ((boll_1 < -1.0) &&
                                            nt.InRange(boll_0, -0.9, -0.0) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.25, 5.0) &&
                                            nt.InRange(scan_result.SP_PercentChange_1DayAgo, -2.0, -0.25))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    case 37:  //strat7 but lower SP500 change to 0.14
                                        if ((boll_1 < -1.1) &&
                                            nt.InRange(boll_0, -0.9, -0.7) &&
                                            nt.InRange(scan_result.PercentChange_Today, 0.5, 2.0) &&
                                            nt.InRange(scan_result.SP500_PercentChange_Today, 0.14, 3.0))
                                        {
                                            bBuy = true;
                                        }
                                        break;

                                    default:
                                        break;
                                }

                                if (bBuy == true) 
                                {
                                    num_matching_strategies++;
                                    scan_result.StrategyMatches = scan_result.StrategyMatches + strat_num.ToString() + ";";
                                }

                                
                            }

                            if (SimulateTrades == false)
                            {
                                if (num_matching_strategies > 0)  //we found at least once matching strategy
                                {
                                    trade_scanner_results.Add(scan_result);
                                }
                            }
                            else 
                            {
                                String signal_name = scan_result.Symbol + "-" + Strategy_Num.ToString() + "-" + DaysToHold.ToString() + "-" + StopLossPercent.ToString();

                                //Check to see if time to sell
                                if (Trade_Status[BarIndex] == "InProgress")
                                {
                                    //if ((date_to_process > trig.TradeDate)) // && (date_to_process <= trig.TradeDate.AddDays(trig.DaysToHold)))
                                    //{
                                    DaysSincePurchase[BarIndex]++;


                                    if (DaysSincePurchase[BarIndex] == DaysToHold) //Use input parameter rather than reading from file trig.DaysToHold //Set Exit for end of nth day 
                                    {
                                        Trade_Status[BarIndex] = "Idle";
                                        ExitLong(BarIndex, NumShares[BarIndex], signal_name, signal_name);
                                        DaysSincePurchase[BarIndex] = -1;
                                    }
                                    //}

                                }
                                else if (bBuy)
                                {
                                    if (scan_result.Close < AmountPerTrade)  //if price is greater than max per trade, then we cannot purchase
                                    {
                                        TradeTriggerByDate trig = new TradeTriggerByDate();
                                        trig.Symbol = scan_result.Symbol;
                                        trig.TradeDate = scan_result.Date;
                                        trig.DaysToHold = DaysToHold;
                                        trade_triggers.Add(trig);


                                        Trade_Status[BarIndex] = "InProgress";
                                        DaysSincePurchase[BarIndex] = 0;
                                        //trig.Status = "InProgress";
                                        NumShares[BarIndex] = (int)(AmountPerTrade / scan_result.Close);
                                        EnterLong(BarIndex, NumShares[BarIndex], signal_name);

                                        trade_scanner_results.Add(scan_result);

                                    }
                                }
                            }
                            
                        }
                        catch
                        {
                            Console.WriteLine("Caught exception");
                        }
                    }
                }
            }

            if (bFinishedProcessingAllData == true)
            {
                if (uid == "")  //avoid some rare cases where this gets called twice.  instead, just overwrite files
                {
                    uid = rnd.Next(100000, 999999).ToString();
                    results_folder = results_folder + "_" + uid + "\\";
                    DirectoryInfo di2 = Directory.CreateDirectory(results_folder);
                }
                WriteTradeScannerResults();
                //WriteTradeTriggers();
                if (SimulateTrades == true)
                {
                    WriteSP500HistoricalData();
                }
                    
            }
        }

        private Indicator_FutureValueChange_Pair GetIndicatorValue(IndicatorEnum indicator, int bar_index, int day_index, DateTime date_to_process)
        {
            double indicator_value = 0.0;
            switch (indicator)
            {
                case IndicatorEnum.indicator_MACD:
                    indicator_value = MACD(BarsArray[bar_index], Param1, Param2, Param3)[day_index];  //get the indicator val from n days ago
                    break;
                case IndicatorEnum.indicator_RSI:
                    indicator_value = RSI(BarsArray[bar_index], Param1, Param2)[day_index];  //get the indicator val from n days ago
                    break;
                case IndicatorEnum.indicator_BOLLINGER:
                    double upper_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Upper[day_index];  //get the indicator val from n days ago
                    double middle_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Middle[day_index];  //get the indicator val from n days ago
                    double lower_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Lower[day_index];  //get the indicator val from n days ago
                    double current_price = Closes[bar_index][day_index];
                    double diff = current_price - middle_band;
                    double band_range = upper_band - middle_band;
                    indicator_value = diff / band_range; //how far current price is from the middle band (-1.0 means we're at the lower band, +1 means we're at the upper band)
                    break;
                case IndicatorEnum.indicator_STOCHASTIC:
                    //use the "D" value
                    indicator_value = Stochastics(BarsArray[bar_index], Param1, Param2, Param3).D[day_index];  //get the indicator val from n days ago
                    break;
                case IndicatorEnum.indicator_STOCHASTIC_RSI:
                    indicator_value = StochRSI(BarsArray[bar_index], Param1)[day_index];  //get the indicator val from n days ago
                    break;
                case IndicatorEnum.indicator_GREG:
                    indicator_value = -999.999; // GregIndicator1(BarsArray[BarIndex], (float)Param1)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                    break;
                default:
                    indicator_value = -999.99;
                    break;
            }

            if (double.IsNaN(indicator_value))
                indicator_value = 0.0;

            //S&P500 is always BarIndex 1
            int SP500_Bar_Index = 1;
            double SP500_future_price = Closes[SP500_Bar_Index][0];
            double SP500_start_price = Closes[SP500_Bar_Index][day_index];
            double SP500_future_price_change = ((SP500_future_price / SP500_start_price) - 1.0) * 100.0;

            double future_price = Closes[bar_index][0];
            double start_price = Closes[bar_index][day_index];
            double future_price_change = ((future_price / start_price) - 1.0) * 100.0;

            double price_change_compared_to_SP500 = future_price_change - SP500_future_price_change;

            Indicator_FutureValueChange_Pair indicator_pair = new Indicator_FutureValueChange_Pair();
            indicator_pair.Date = date_to_process;
            indicator_pair.Price = Convert.ToSingle(start_price);
            indicator_pair.Indicator = Convert.ToSingle(indicator_value);
            indicator_pair.FutureValueChange = Convert.ToSingle(price_change_compared_to_SP500); // future_price_change;
            indicator_pair.SP500_Price = Convert.ToSingle(SP500_start_price);

            return indicator_pair;
        }




        #region Properties
        [Display(ResourceType = typeof(Custom.Resource), Name = "TestingStartDate", GroupName = "NinjaScriptStrategyParameters", Order = 2)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime TestingStartDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "TestingEndDate", GroupName = "NinjaScriptStrategyParameters", Order = 3)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime TestingEndDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "output_folder", GroupName = "NinjaScriptStrategyParameters", Order = 11)]
        public String output_folder
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Strategy #", GroupName = "Strategy", Order = 0)]
        public int Strategy_Num
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "BOLLINGER Params", GroupName = "Strategy", Order = 1)]
        public String BOLL_param_string
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Strategies", GroupName = "Strategy", Order = 1)]
        public String Strategies_string
        { get; set; }

        [Range(1, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Amount Per Trade", GroupName = "Trade Params", Order = 0)]
        public float AmountPerTrade
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Stop Loss %", GroupName = "Trade Params", Order = 1)]
        public int StopLossPercent
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Days to Hold", GroupName = "Trade Params", Order = 2)]
        public int DaysToHold
        { get; set; }

        [TypeConverter(typeof(FriendlyBoolConverter))] // Converts the bool to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
        [Display(Name = "Simulate Trades", GroupName = "Trade Params", Order = 3)]
        public bool SimulateTrades
        { get; set; }


    }
    //{ get; set; }

    #endregion




}
