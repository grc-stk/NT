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


    public class KNN_Generator12 : Strategy//, INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        protected void OnIndicatorChanged(IndicatorEnum my_enum)
        {
            switch (my_enum)
            {
                case IndicatorEnum.indicator_MACD:
                    Param1 = 12;
                    Param2 = 26;
                    Param3 = 9;
                    break;
                case IndicatorEnum.indicator_RSI:
                    Param1 = 14;
                    Param2 = 3;
                    Param3 = -1;
                    break;
                case IndicatorEnum.indicator_BOLLINGER:
                    Param1 = 2;
                    Param2 = 14;
                    Param3 = -1;
                    break;
                case IndicatorEnum.indicator_STOCHASTIC:
                    Param1 = 7;
                    Param2 = 14;
                    Param3 = 3;
                    break;
                case IndicatorEnum.indicator_STOCHASTIC_RSI:
                    Param1 = 14;
                    Param2 = -1;
                    Param3 = -1;
                    break;
                case IndicatorEnum.indicator_GREG:
                    Param1 = 1;
                    Param2 = -1;
                    Param3 = -1;
                    break;
                default:
                    Param1 = -1;
                    Param2 = -1;
                    Param3 = -1;
                    break;
            }
        }
        //		
        private static Random rnd;
        static KNN_Generator12()
        {
            rnd = new Random();
        }

        private static Mutex mut = new Mutex(false, "NTWriteToFile");

        string description = "";

        //String[] symbol_list = new String[] { "DUMMY", "GOOG"};  //Add "DUMMY" to account for primary instrument
        //String[] symbol_list = new String[] { "DUMMY", "SLB", "MSFT", "AAPL", "GOOG" };  //Add "DUMMY" to account for primary instrument
        //String[] symbol_list = new String[] { "DUMMY", "SLB", "MSFT", "AAPL", "GOOG", "BAC", "T", "XOM", "JNJ", "FB", "NEM", "RRC", "O", "ULTA", "VMC", "HAL" };
        List<String> symbol_list;
        List<String> company_name_list;

        private double Buy_Price = 0.0;
        private double BarBought = 0;

        private Order entryOrder = null;

        private List<List<Indicator_FutureValueChange_Pair>> list_Indicator_FutureValueChange_Training_ALL = new List<List<Indicator_FutureValueChange_Pair>>();
        private List<List<Indicator_FutureValueChange_Pair>> list_Indicator_FutureValueChange_Testing_ALL = new List<List<Indicator_FutureValueChange_Pair>>();

        private List<Indicator_FutureValueChange_Pair> list_Indicator_FutureValueChange_Training = new List<Indicator_FutureValueChange_Pair>();
        private List<Indicator_FutureValueChange_Pair> list_Indicator_FutureValueChange_Testing = new List<Indicator_FutureValueChange_Pair>();

        private int count = 0;

        private bool bDataLoaded = false;
        private bool bFirst = true;
        private bool bExecutedPython = false;

        String my_id = "default";
        String uid = "";
        String indicator = "";
        String master_symbol_name = "";

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                if (bFirst)
                {
                    bFirst = false;
                }

                symbol_list = new List<String> { "DUMMY" };
                company_name_list = new List<String> { "DUMMY" };

                LoadTickers(); //load symbols from file

                
                Description = "test";
                Name = "KNN_Generator12";
                DaysToLookBack = 1;
                FutureValueDaysToLookAhead = 5;

                //TrainingStartDate = new DateTime(2017, 01, 01);
                //TrainingEndDate = new DateTime(2017, 12, 31);
                //TestingStartDate = new DateTime(2018, 1, 1);
                TrainingStartDate = DateTime.Today.AddYears(-1);  //1 year ago
                TrainingEndDate = DateTime.Today.AddDays(-91); //3 months ago 
                TestingStartDate = DateTime.Today.AddDays(-90); //3 months ago
                TestingEndDate = DateTime.Today;

                //Custom params
                indicator_to_use = IndicatorEnum.indicator_MACD;
                uid = "";
                indicator = "";
                KNN_num_neighbors = 3;
                num_training_pts = 200;
                num_groups = 20;
                stagger_factor = 0;
                window_size = 1;
                avg_target = 1.0f;
                good_target = 0.5f;
                bad_target = 0.20f;
                min_samples_pct = 0.06f;
                thresh1 = 1.0f;
                thresh2 = -1.0f;
                output_folder = "C:\\temp\\knn\\";

                Param1 = 12;  //Defaults for MACD
                Param2 = 26;
                Param3 = 9;

                Sanitize = false;
                ProcessAllTickers = true;
                FindBestSolutions = false;

                master_symbol_name = "";

                bDataLoaded = false;


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

                    list_Indicator_FutureValueChange_Training_ALL.Add(new List<Indicator_FutureValueChange_Pair>());
                    list_Indicator_FutureValueChange_Testing_ALL.Add(new List<Indicator_FutureValueChange_Pair>());

                }

                SetStopLoss(CalculationMode.Percent, 0.05);

                // Sets a 20 tick trailing stop for an open position
                //SetTrailStop(CalculationMode.Ticks, 20);
            }
            else if (State == State.DataLoaded)
            {
                //master_symbol_name = Instrument.MasterInstrument.Name;

                bDataLoaded = true;
            }
            else if ((bDataLoaded == true) && ((State == State.Transition) || (State == State.Terminated)))  //finished processing historical data (and ready for real-time)
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

           
            // Read the file and display it line by line.  
            System.IO.StreamReader file2 = new System.IO.StreamReader(sp500_csv_file_path);
            while ((line2 = file2.ReadLine()) != null)
            {
                string[] values = line2.Split(',');
                if (values.Length == 2)
                {
                    symbol_list.Add(values[0]);
                    company_name_list.Add(values[1]);
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
            start.Arguments = "/C " + Python_Path + string.Format(" {0} {1}", script_file_path, args) + ">>c:\\temp\\knn\\python_log.txt 2>&1";
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
                double price = list_to_write[i].Price;
                double indicator_value = list_to_write[i].Indicator;
                double future_value_change = list_to_write[i].FutureValueChange;

                var newLine = string.Format("{0},{1},{2},{3}", date, price, indicator_value, future_value_change);

                if (Sanitize == false)
                    newLine = string.Format("{0},{1},{2},{3}", date, price, indicator_value, future_value_change);

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

        protected String GenerateConfigFile(String filePath, String symbol, String company, String training_file_path, String testing_file_path, String unique_id)
        {

            String output_file_base = filePath + "_results_";
            String config_file_name = filePath + "_config" + unique_id + ".txt";

            List<String> labels = new List<String>
                                    {   "uid",
                                        "KNN_num_neighbors",
                                        "num_training_pts",
                                        "num_groups",
                                        "stagger_factor",
                                        "window_size",
                                        "avg_target",
                                        "good_target",
                                        "bad_target",
                                        "min_samples_pct",
                                        "thresh1",
                                        "thresh2",
                                        "future_lookahead",
                                        "s_name",
                                        "co_name",
                                        "description",
                                        "indicator",
                                        "param1",
                                        "param2",
                                        "param3",
                                        "training_file",
                                        "training_start",
                                        "training_end",
                                        "testing_file",
                                        "testing_start",
                                        "testing_end",
                                        "output_csv_file",
                                        "output_txt_file",
                                        "output_pdf_file"};

            List<String> values = new List<String>
                                    {   uid,
                                        KNN_num_neighbors.ToString(),
                                        num_training_pts.ToString(),
                                        num_groups.ToString(),
                                        stagger_factor.ToString(),
                                        window_size.ToString(),
                                        avg_target.ToString(),
                                        good_target.ToString(),
                                        bad_target.ToString(),
                                        min_samples_pct.ToString(),
                                        thresh1.ToString(),
                                        thresh2.ToString(),
                                        FutureValueDaysToLookAhead.ToString(),
                                        symbol,
                                        company,
                                        description,
                                        indicator,
                                        Param1.ToString(),
                                        Param2.ToString(),
                                        Param3.ToString(),
                                        training_file_path,
                                        TrainingStartDate.ToShortDateString(),
                                        TrainingEndDate.ToShortDateString(),
                                        testing_file_path,
                                        TestingStartDate.ToShortDateString(),
                                        TestingEndDate.ToShortDateString(),
                                        output_file_base + "data" + unique_id + ".csv",
                                        output_file_base + "summary" + unique_id + ".txt",
                                        output_file_base + "plot" + unique_id + ".pdf",
                                        };

            List<String> types = new List<String>
                                    {   "str",
                                        "int",
                                        "int",
                                        "int",
                                        "int",
                                        "int",
                                        "float",
                                        "float",
                                        "float",
                                        "float",
                                        "float",
                                        "float",
                                        "int",
                                        "str",
                                        "str",
                                        "str",
                                        "str",
                                        "int",
                                        "int",
                                        "int",
                                        "str",
                                        "str",
                                        "str",
                                        "str",
                                        "str",
                                        "str",
                                        "str",
                                        "str",
                                        "str"};

            var cfg = new StringBuilder();

            for (int i = 0; i < labels.Count; i++)
            {
                cfg.AppendLine(labels[i] + "=" + values[i] + "#" + types[i]);
            }

            //mut.WaitOne(); //wait until safe to enter; prior thread has completed writing.
            try
            {
                File.WriteAllText(config_file_name, cfg.ToString());
                //mut.ReleaseMutex();
            }
            catch (System.Exception exp)
            {
                Log("File write error for file name '" + filePath + "' Error '" + exp.Message + "'", LogLevel.Warning);
            }



            return (config_file_name);

        }
        protected override void OnBarUpdate()
        {
            
            int BarIndex = BarsInProgress;

            bool bFinishedProcessingAllData = false;

            if (((CurrentBar + 2) == Count) && BarIndex == (symbol_list.Count - 1))  //we are on the last bar to process
            {
                bFinishedProcessingAllData = true;
            }

            if ( ((ProcessAllTickers == true) && (BarIndex >= 1) && (BarIndex < (symbol_list.Count))) ||  //Start at index=1 since we want to ignore the primary/dummy instrument
                    ((ProcessAllTickers == false) && (BarIndex == 0)) )
            {

                if (CurrentBar >= (FutureValueDaysToLookAhead))  //just make sure we avoid out of bounds error in case we don't yet have enough data
                {
                    DateTime current_bar_date = Times[BarIndex][0].Date;


                    DateTime date_to_process = Times[BarIndex][FutureValueDaysToLookAhead].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                    bool bWithinTrainingPeriod = (date_to_process >= TrainingStartDate) && (date_to_process <= TrainingEndDate);
                    bool bWithinTestingPeriod = (date_to_process >= TestingStartDate) && (date_to_process <= TestingEndDate);

                    if ((bWithinTrainingPeriod) || (bWithinTestingPeriod))
                    {
                        //TimeSpan diff = TrainingEndDate - TrainingStartDate;
                        //int training_days = (int)Math.Abs(Math.Round(diff.TotalDays));
                        double indicator_value = 0.0;
                        switch (indicator_to_use)
                        {
                            case IndicatorEnum.indicator_MACD:
                                indicator_value = MACD(BarsArray[BarIndex], Param1, Param2, Param3)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_RSI:
                                indicator_value = RSI(BarsArray[BarIndex], Param1, Param2)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_BOLLINGER:
                                double upper_band = Bollinger(BarsArray[BarIndex], (double)Param1, Param2).Upper[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                                double middle_band = Bollinger(BarsArray[BarIndex], (double)Param1, Param2).Middle[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                                double lower_band = Bollinger(BarsArray[BarIndex], (double)Param1, Param2).Lower[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                                double current_price = Closes[BarIndex][FutureValueDaysToLookAhead];
                                double diff = current_price - middle_band;
                                double band_range = upper_band - middle_band;
                                indicator_value = diff / band_range; //how far current price is from the middle band (-1.0 means we're at the lower band, +1 means we're at the upper band)
                                break;
                            case IndicatorEnum.indicator_STOCHASTIC:
                                //use the "D" value
                                indicator_value = Stochastics(BarsArray[BarIndex], Param1, Param2, Param3).D[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_STOCHASTIC_RSI:
                                indicator_value = StochRSI(BarsArray[BarIndex], Param1)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
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

                        double future_price_change = 0.0;
                        double future_price = Closes[BarIndex][0];
                        double start_price = Closes[BarIndex][FutureValueDaysToLookAhead];
                        future_price_change = ((future_price / start_price) - 1.0) * 100.0;

                        Indicator_FutureValueChange_Pair indicator_pair = new Indicator_FutureValueChange_Pair();
                        indicator_pair.Date = date_to_process;
                        indicator_pair.Price = start_price;
                        indicator_pair.Indicator = indicator_value;
                        indicator_pair.FutureValueChange = future_price_change;

                        if (bWithinTrainingPeriod)
                        {
                            list_Indicator_FutureValueChange_Training.Add(indicator_pair);
                            list_Indicator_FutureValueChange_Training_ALL[BarIndex].Add(indicator_pair);
                            int count_training = list_Indicator_FutureValueChange_Training.Count;
                        }
                        else if (bWithinTestingPeriod)
                        {
                            list_Indicator_FutureValueChange_Testing.Add(indicator_pair);
                            list_Indicator_FutureValueChange_Testing_ALL[BarIndex].Add(indicator_pair);
                            int count_testing = list_Indicator_FutureValueChange_Testing.Count;
                        }
                    }
                 }
            }
            else
            {


            }
            if (bFinishedProcessingAllData)
            {

                for (int bar_index=1; bar_index<symbol_list.Count; bar_index++)
                {
                    for (int j= FutureValueDaysToLookAhead; j>=0; j--)
                    {
                        DateTime date_to_process = Times[bar_index][j].Date;
                        double indicator_value = 0.0;
                        switch (indicator_to_use)
                        {
                            case IndicatorEnum.indicator_MACD:
                                indicator_value = MACD(BarsArray[bar_index], Param1, Param2, Param3)[j];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_RSI:
                                indicator_value = RSI(BarsArray[bar_index], Param1, Param2)[0];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_BOLLINGER:
                                double upper_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Upper[j];  //get the indicator val from n days ago
                                double middle_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Middle[j];  //get the indicator val from n days ago
                                double lower_band = Bollinger(BarsArray[bar_index], (double)Param1, Param2).Lower[j];  //get the indicator val from n days ago
                                double current_price = Closes[bar_index][j];
                                double diff = current_price - middle_band;
                                double band_range = upper_band - middle_band;
                                indicator_value = diff / band_range; //how far current price is from the middle band (-1.0 means we're at the lower band, +1 means we're at the upper band)
                                break;
                            case IndicatorEnum.indicator_STOCHASTIC:
                                //use the "D" value
                                indicator_value = Stochastics(BarsArray[bar_index], Param1, Param2, Param3).D[j];  //get the indicator val from n days ago
                                break;
                            case IndicatorEnum.indicator_STOCHASTIC_RSI:
                                indicator_value = StochRSI(BarsArray[bar_index], Param1)[j];  //get the indicator val from n days ago
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

                        double future_price_change = 0.0;
                        double future_price = Closes[bar_index][j];
                        double start_price = Closes[bar_index][j];
                        future_price_change = ((future_price / start_price) - 1.0) * 100.0;

                        Indicator_FutureValueChange_Pair indicator_pair = new Indicator_FutureValueChange_Pair();
                        indicator_pair.Date = date_to_process;
                        indicator_pair.Price = start_price;
                        indicator_pair.Indicator = indicator_value;
                        indicator_pair.FutureValueChange = future_price_change;

                        //if (bWithinTrainingPeriod)
                        //{
                        //    list_Indicator_FutureValueChange_Training.Add(indicator_pair);
                        //    list_Indicator_FutureValueChange_Training_ALL[BarIndex].Add(indicator_pair);
                        //    int count_training = list_Indicator_FutureValueChange_Training.Count;
                        //}
                        //else if (bWithinTestingPeriod)
                        //{
                            list_Indicator_FutureValueChange_Testing.Add(indicator_pair);
                            list_Indicator_FutureValueChange_Testing_ALL[bar_index].Add(indicator_pair);
                            int count_testing = list_Indicator_FutureValueChange_Testing.Count;
                        //}
                    }
                        
                }
                
                //if (CurrentBar >= (FutureValueDaysToLookAhead))  //just make sure we avoid out of bounds error in case we don't yet have enough data
                //{
                //    DateTime current_bar_date = Times[bar_index][0].Date;
                //    DateTime date_to_process = Times[bar_index][0].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                //    bool bWithinTrainingPeriod = (date_to_process >= TrainingStartDate) && (date_to_process <= TrainingEndDate);
                //    bool bWithinTestingPeriod = (date_to_process >= TestingStartDate) && (date_to_process <= TestingEndDate);

                //    if ((bWithinTrainingPeriod) || (bWithinTestingPeriod))
                //    {
                //        //TimeSpan diff = TrainingEndDate - TrainingStartDate;
                //        //int training_days = (int)Math.Abs(Math.Round(diff.TotalDays));
                        
                //    }
                //}
               


                ////////

                uid = rnd.Next(100000, 999999).ToString();
                String unique_id = "_" + uid; // creates a number between 100k and 999k

                String debug_txt = "FINISHEDPROCESSING min_samples=" + min_samples_pct.ToString() + " id=" + unique_id.ToString();
                Debug.WriteLine(debug_txt);

                String control_file_path = output_folder + "control_file" + unique_id + ".txt";
                String consolidated_txt_file_path = output_folder + "consolidated_report" + unique_id + ".txt";
                String consolidated_csv_file_path = output_folder + "consolidated_report" + unique_id + ".csv";

                switch (indicator_to_use)
                {
                    case IndicatorEnum.indicator_MACD:
                        if (Sanitize == false)
                            description = "MACD";
                        else
                            description = "M";
                        indicator = description;
                        description = description + "(" + Param1.ToString() + "-" + Param2.ToString() + "-" + Param3.ToString() + ")";
                        break;
                    case IndicatorEnum.indicator_RSI:
                        if (Sanitize == false)
                            description = "RSI";
                        else
                            description = "R";
                        indicator = description;
                        description = description + "(" + Param1.ToString() + "-" + Param2.ToString() + ")";
                        break;
                    case IndicatorEnum.indicator_BOLLINGER:
                        if (Sanitize == false)
                            description = "BOLL";
                        else
                            description = "B";
                        indicator = description;
                        description = description + "(" + Param1.ToString() + "-" + Param2.ToString() + ")";
                        break;
                    case IndicatorEnum.indicator_STOCHASTIC:
                        if (Sanitize == false)
                            description = "STOCH";
                        else
                            description = "S";
                        indicator = description;
                        description = description + "(" + Param1.ToString() + "-" + Param2.ToString() + "-" + Param3.ToString() + ")";
                        break;
                    case IndicatorEnum.indicator_STOCHASTIC_RSI:
                        if (Sanitize == false)
                            description = "STOCH_RSI";
                        else
                            description = "SR";
                        indicator = description;
                        description = description + "(" + Param1.ToString() + ")";
                        break;
                    default:
                        description = "Unknown";
                        indicator = description;
                        break;
                }

                description = description + " - " + FutureValueDaysToLookAhead.ToString();

                if (Sanitize == false)
                    description = description + " days";

                description = description + " - " + unique_id.Substring(1);  //remove preceding "_"

                bool bFirstTime = true;
                int start_index = 1;
                int max_index = symbol_list.Count;
                if (ProcessAllTickers == false)
                {
                    start_index = 0;
                    max_index = 1;
                }
                for (int i = start_index; i < max_index; i++)  //Start at index=1 since we want to ignore the first primary/dummy instrument
                {
                    String symbol_name = symbol_list[i];
                    if (ProcessAllTickers == false)
                    {
                        symbol_name = master_symbol_name;
                    }

                    String company_name = company_name_list[i];
                    if (Sanitize == true)
                    {
                        String symbol_name_tmp = string.Concat(symbol_name.Reverse()).ToLower();
                        symbol_name = char.ToUpper(symbol_name_tmp[0]) + symbol_name_tmp.Substring(1);
                        company_name = "?";
                    }

                    String base_file_path_for_symbol = output_folder + symbol_name;  //i.e. c:\temp\knn\AAPL

                    String training_file_path = base_file_path_for_symbol + "_training" + unique_id + ".csv";
                    String testing_file_path = base_file_path_for_symbol + "_testing" + unique_id + ".csv";
                    WriteListToCSV(list_Indicator_FutureValueChange_Training_ALL[i], training_file_path);
                    WriteListToCSV(list_Indicator_FutureValueChange_Testing_ALL[i], testing_file_path);

                    String config_file_name = "";
                    config_file_name = GenerateConfigFile(base_file_path_for_symbol, symbol_name, company_name, training_file_path, testing_file_path, unique_id);

                    //mut.WaitOne();
                    if (bFirstTime)  //create new file
                    {
                        bFirstTime = false;
                        File.WriteAllText(control_file_path, config_file_name + "\r\n");
                    }
                    else // append to existing file
                    {
                        File.AppendAllText(control_file_path, config_file_name + "\r\n");
                    }
                    //mut.ReleaseMutex();
                }
                //Now we can call python for all symbols at once by passing in the control_file as cmd line arg
                mut.WaitOne();
                string mode = " BEST";
                if (FindBestSolutions == false)
                    mode = " SINGLE";
                string args = control_file_path + " " + consolidated_txt_file_path + " " + consolidated_csv_file_path + " c:\\temp\\knn\\master_report.csv" + " c:\\temp\\knn\\master_trig.csv" + " false" + mode;
                CallPythonScript(output_folder + "\\Data_Processing39.py", args);
                mut.ReleaseMutex();
            }
        }


        #region Properties
        [Display(ResourceType = typeof(Custom.Resource), Name = "TrainingStartDate", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime TrainingStartDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "TrainingEndDate", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime TrainingEndDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "TestingStartDate", GroupName = "NinjaScriptStrategyParameters", Order = 2)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime TestingStartDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "TestingEndDate", GroupName = "NinjaScriptStrategyParameters", Order = 3)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime TestingEndDate { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "DaysToLookBack", GroupName = "NinjaScriptStrategyParameters", Order = 4)]
        public int DaysToLookBack
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "FutureValueDaysToLookAhead", GroupName = "NinjaScriptStrategyParameters", Order = 5)]
        public int FutureValueDaysToLookAhead
        { get; set; }

        private IndicatorEnum my_enum;
        [RefreshProperties(RefreshProperties.All)]  //This will make UI refresh all property values (i.e. if one property affects another)
        [TypeConverter(typeof(IndicatorEnumConverter))] // Converts the enum to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Enums normally automatically get a combo box, but we need to apply this specific editor so default value is automatically selected
        [Display(Name = "Indicator", GroupName = "Indicator", Order = 0)]
        public IndicatorEnum indicator_to_use
        //{ get; set; }
        {
            get { return my_enum; }//return this.Param1; }
            set
            {
                my_enum = value;
                //// Call OnPropertyChanged whenever the property is updated
                //OnPropertyChanged(my_enum); // value.ToString());
                OnIndicatorChanged(my_enum); // value.ToString());
            }
        }



        [Range(int.MinValue, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Param1", GroupName = "Indicator", Order = 1)]
        public int Param1
        { get; set; }
        
        [Range(int.MinValue, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Param2", GroupName = "Indicator", Order = 2)]
        public int Param2
        { get; set; }

        [Range(int.MinValue, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Param3", GroupName = "Indicator", Order = 3)]
        public int Param3
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "KNN_num_neighbors", GroupName = "Custom", Order = 0)]
        public int KNN_num_neighbors
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "num_training_pts", GroupName = "Custom", Order = 1)]
        public int num_training_pts
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "num_groups", GroupName = "Custom", Order = 2)]
        public int num_groups
        { get; set; }

        [Range(0, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "stagger_factor", GroupName = "Custom", Order = 3)]
        public int stagger_factor
        { get; set; }

        [Range(0, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "window_size", GroupName = "Custom", Order = 4)]
        public int window_size
        { get; set; }

        [Range(0, 1.0f), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "avg_target", GroupName = "Custom", Order = 5)]
        public float avg_target
        { get; set; }

        [Range(0, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "good_target", GroupName = "Custom", Order = 6)]
        public float good_target
        { get; set; }

        [Range(0, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "bad_target", GroupName = "Custom", Order = 7)]
        public float bad_target
        { get; set; }

        [Range(0, 1.0), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "min_samples_pct", GroupName = "Custom", Order = 8)]
        public float min_samples_pct
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

        [TypeConverter(typeof(FriendlyBoolConverter))] // Converts the bool to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
        [Display(Name = "Sanitize", GroupName = "Custom", Order = 11)]
        public bool Sanitize
        { get; set; }

        [TypeConverter(typeof(FriendlyBoolConverter))] // Converts the bool to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
        [Display(Name = "All Symbols", GroupName = "Custom", Order = 12)]
        public bool ProcessAllTickers
        { get; set; }

        [TypeConverter(typeof(FriendlyBoolConverter))] // Converts the bool to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
        [Display(Name = "Find Best Solutions", GroupName = "Custom", Order = 13)]
        public bool FindBestSolutions
        { get; set; }


    }
    //{ get; set; }

    #endregion




}
