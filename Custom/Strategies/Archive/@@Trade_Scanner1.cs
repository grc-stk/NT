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


    public class Trade_Scanner1 : Strategy//, INotifyPropertyChanged
    {
              		
        private static Random rnd;
        static Trade_Scanner1()
        {
            rnd = new Random();
        }

        private static Mutex mut_python = new Mutex(false, "Python");

        string description = "";

        List<String> symbol_list;
        List<String> company_name_list;

        private double Buy_Price = 0.0;
        private double BarBought = 0;

        private Order entryOrder = null;

        public List<TradeScannerResult> trade_scanner_results = new List<TradeScannerResult>();

        public List<TradeTriggerByDate> trade_triggers = new List<TradeTriggerByDate>();

        public List<List<List<Indicator_FutureValueChange_Pair>>> master_list_training = new List<List<List<Indicator_FutureValueChange_Pair>>>();
        public List<List<List<Indicator_FutureValueChange_Pair>>> master_list_testing = new List<List<List<Indicator_FutureValueChange_Pair>>>();

        public List<IndicatorEnum> indicator_list = new List<IndicatorEnum>() { IndicatorEnum.indicator_BOLLINGER };
    
        private int count = 0;

        private bool bDataLoaded = false;
        private bool bFirst = true;
        private bool bExecutedPython = false;

        String my_id = "default";
        String uid = "";
        String indicator = "";
        String master_symbol_name = "";
        String output_folder_with_date = "";
        DateTime TrainingStartDate = DateTime.Today;
        DateTime TrainingEndDate = DateTime.Today;
        DateTime TestingStartDate =  DateTime.Today;

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

                output_folder = "C:\\temp\\knn\\";

                LoadTickers(); //load symbols from file

                
                Description = "test";
                Name = "Trade_Scanner1";
                Calculate = Calculate.OnEachTick;  //This will let us get the last Bar
                                
                TestingEndDate = DateTime.Today;

                //Custom params
                uid = "";
                indicator = "";
                thresh1 = 1.0f;
                thresh2 = -1.0f;
                

                master_symbol_name = "";

                bDataLoaded = false;

                BOLL_param_string = "2;14;-1";
                
                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                //IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
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
                TestingStartDate = TestingEndDate.AddDays(-90);

                output_folder_with_date = output_folder + TestingEndDate.ToString("ddMMMyyyy") + "\\";

                DirectoryInfo di = Directory.CreateDirectory(output_folder_with_date);

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

        protected double PctChange(int bar_index, int days_ago)
        {
            double pct_change = (((Closes[bar_index][0] / Closes[bar_index][days_ago]) - 1.0) * 100.0);
            return pct_change;
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
            csv.AppendLine("Date,Symbol,Co.,Open,Close,%Change,SP500%Change,CompSP500,%Change-1Day, %Change-2Day,%Change-5Day," +
                           "%Change-10Day,%Change-15Day,%Change-20Day,CompSP500-1Day,CompSP500-2Day,CompSP500-5Day,CompSP500-10Day," +
                           "CompSP500-15Day,CompSP500-20Day,IndName,IndToday,IndChange-1Day,IndChange-2Day,IndChange-5Day," + 
                           "IndChange-10Day,IndChange-15Day,IndChange-20Day");
            
            for (int i = 0; i < count; i++)
            {
                TradeScannerResult res = trade_scanner_results[i];

                var newLine = string.Format("{0},{1},{2},{3},{4:0.00},{5:0.00},{6:0.00},{7:0.00}",
                                            res.Date.ToShortDateString(),
                                            res.Symbol,
                                            res.Description,
                                            res.Open,
                                            res.Close,
                                            res.PercentChange_Today,
                                            res.SP500_PercentChange_Today,
                                            res.Compare_SP500_Today);

                newLine = newLine + string.Format(",{0:0.00},{1:0.00},{2:0.00},{3:0.00},{4:0.00},{5:0.00}",
                                            res.PercentChange_1Day,
                                            res.PercentChange_2Day,
                                            res.PercentChange_5Day,
                                            res.PercentChange_10Day,
                                            res.PercentChange_15Day,
                                            res.PercentChange_20Day);

                newLine = newLine + string.Format(",{0:0.00},{1:0.00},{2:0.00},{3:0.00},{4:0.00},{5:0.00}",
                                            res.Compare_SP500_1Day,
                                            res.Compare_SP500_2Day,
                                            res.Compare_SP500_5Day,
                                            res.Compare_SP500_10Day,
                                            res.Compare_SP500_15Day,
                                            res.Compare_SP500_20Day);

                newLine = newLine + string.Format(",{0},{1:0.00},{2:0.00},{3:0.00},{4:0.00},{5:0.00},{6:0.00},{7:0.00}",
                                            res.Indicator_Name,
                                            res.Indicator_Today,
                                            res.Indicator_Change_1Day,
                                            res.Indicator_Change_2Day,
                                            res.Indicator_Change_5Day,
                                            res.Indicator_Change_10Day,
                                            res.Indicator_Change_15Day,
                                            res.Indicator_Change_20Day);

            
                csv.AppendLine(newLine);
            }

            //mut.WaitOne(); //wait until safe to enter; prior thread has completed writing.
            try
            {
                File.WriteAllText("c:\\temp\\knn\\scanner_results.csv", csv.ToString());
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
                File.WriteAllText("c:\\temp\\knn\\triggers.csv", csv.ToString());
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
            if ( (BarIndex >= 2/*1*/) && (BarIndex < (symbol_list.Count)) )  //Start at index=1 since we want to ignore the primary/dummy instrument
            {
                DateTime date_to_process = Times[BarIndex][0].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                //if (date_to_process == TestingEndDate)
                if ( (date_to_process <= TestingEndDate) && (CurrentBar >= (20)) )  //give 20 days of buffer
                {
                    //Convert.ToSingle

                    int SP500_index = 1;

                    TradeScannerResult scan_result = new TradeScannerResult();

                    double OpenPriceToday = Opens[BarIndex][0];
                    double ClosePriceToday = Closes[BarIndex][0];
                    double SP500_Open = Opens[SP500_index][0];    
                    double SP500_Close = Closes[SP500_index][0];
                    
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

                    scan_result.Compare_SP500_1Day = scan_result.PercentChange_1Day - PctChange(SP500_index, 1);
                    scan_result.Compare_SP500_2Day = scan_result.PercentChange_2Day - PctChange(SP500_index, 2);
                    scan_result.Compare_SP500_5Day = scan_result.PercentChange_5Day - PctChange(SP500_index, 5);
                    scan_result.Compare_SP500_10Day = scan_result.PercentChange_10Day - PctChange(SP500_index, 10);
                    scan_result.Compare_SP500_15Day = scan_result.PercentChange_15Day - PctChange(SP500_index, 15);
                    scan_result.Compare_SP500_20Day = scan_result.PercentChange_20Day - PctChange(SP500_index, 20);

                    scan_result.Green_5Day = GetPercentOfGreenDays(BarIndex, 5);
                    scan_result.Green_10Day = GetPercentOfGreenDays(BarIndex, 10);
                    scan_result.Green_15Day = GetPercentOfGreenDays(BarIndex, 15);
                    scan_result.Green_20Day = GetPercentOfGreenDays(BarIndex, 20);


                    //public string Indicator_Name { get; set; }
                    //public float Indicator_Today { get; set; }
                    //public float Indicator_Change_1Day { get; set; }    //See how indicator has changed over past N days
                    //public float Indicator_Change_2Day { get; set; }
                    //public float Indicator_Change_5Day { get; set; }
                    //public float Indicator_Change_10Day { get; set; }
                    //public float Indicator_Change_15Day { get; set; }
                    //public float Indicator_Change_20Day { get; set; }
                    //public int Green_5Day { get; set; }  //% of Green days in the past N days
                    //public int Green_10Day { get; set; }
                    //public int Green_15Day { get; set; }
                    //public int Green_20Day { get; set; }

                    for (int i = 0; i < indicator_list.Count; i++)
                    {

                        PopulateParamatersForIndicator(indicator_list[i]);
                        //Indicator_FutureValueChange_Pair indicator_pair = GetIndicatorValue(indicator_list[i], BarIndex, 0, date_to_process);

                        double indicator_value_today = 0.0;

                        switch (indicator_list[i])
                        {
                            case IndicatorEnum.indicator_BOLLINGER:
                                scan_result.Indicator_Name = "BOLL";
                                

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

                                //if (boll_in_range && boll_change_in_range)
                                //{
                                //    //strat1 (declining boll for 3, then up spike)
                                //    if (boll_declining_3dys)
                                //    {
                                //        Console.WriteLine("\nstrat1\n");

                                //        TradeTriggerByDate trig = new TradeTriggerByDate();
                                //        trig.Symbol = scan_result.Symbol;
                                //        trig.TradeDate = scan_result.Date;
                                //        trig.DaysToHold = 5;
                                //        trade_triggers.Add(trig);

                                //    }
                                //    //strat2 (just boll up spike)
                                //    else
                                //    {
                                //        Console.WriteLine("\nstrat2\n");
                                //        TradeTriggerByDate trig = new TradeTriggerByDate();
                                //        trig.Symbol = scan_result.Symbol;
                                //        trig.TradeDate = scan_result.Date;
                                //        trig.DaysToHold = 5;
                                //        trade_triggers.Add(trig);
                                //    }
                                //}

                                if ((boll_1 < -1.1) && (boll_0 > -0.9) && (boll_0 < -0.7))
                                {
                                    Console.WriteLine("\nstrat3\n");
                                    TradeTriggerByDate trig = new TradeTriggerByDate();
                                    trig.Symbol = scan_result.Symbol;
                                    trig.TradeDate = scan_result.Date;
                                    trig.DaysToHold = 5;
                                    trade_triggers.Add(trig);
                                }



                                break;
                            default:
                                break;
                        }


                    }

                    

                    trade_scanner_results.Add(scan_result);
                }
            }

            if (bFinishedProcessingAllData == true)
            {
                WriteTradeScannerResults();
                WriteTradeTriggers();
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
        [Display(ResourceType = typeof(Custom.Resource), Name = "TestingEndDate", GroupName = "NinjaScriptStrategyParameters", Order = 3)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime TestingEndDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "BOLLINGER Params", GroupName = "Indicator", Order = 6)]
        public String BOLL_param_string
        { get; set; }

        [Range(-1.0f, 1.0f), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "thresh1", GroupName = "Custom", Order = 9)]
        public float thresh1
        { get; set; }

        [Range(-1.0f, 1.0f), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "thresh2", GroupName = "Custom", Order = 10)]
        public float thresh2
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "output_folder", GroupName = "Custom", Order = 11)]
        public String output_folder
        { get; set; }


    }
    //{ get; set; }

    #endregion




}
