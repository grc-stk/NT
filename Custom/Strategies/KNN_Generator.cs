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


    public class KNN_Generator17 : Strategy//, INotifyPropertyChanged
    {
              		
        private static Random rnd;
        static KNN_Generator17()
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

        public List<List<List<Indicator_FutureValueChange_Pair>>> master_list_training = new List<List<List<Indicator_FutureValueChange_Pair>>>();
        public List<List<List<Indicator_FutureValueChange_Pair>>> master_list_testing = new List<List<List<Indicator_FutureValueChange_Pair>>>();

        public List<IndicatorEnum> indicator_list = new List<IndicatorEnum>() { IndicatorEnum.indicator_MACD, IndicatorEnum.indicator_RSI, IndicatorEnum.indicator_BOLLINGER, IndicatorEnum.indicator_STOCHASTIC, IndicatorEnum.indicator_STOCHASTIC_RSI };
    
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
                Name = "KNN_Generator17";
                Calculate = Calculate.OnEachTick;  //This will let us get the last Bar
                DaysToLookBack = 1;
                FutureValueDaysToLookAhead = 5;

                
                TestingEndDate = DateTime.Today;

                //Custom params
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
                

                Sanitize = false;
                ProcessAllTickers = true;
                FindBestSolutions = false;
                GeneratePlotsForBestSolutions = false;
                OnlyGeneratePlots = false;

                NumPlotsToGenerate = 20;

                master_symbol_name = "";

                bDataLoaded = false;

                MACD_param_string = "12;26;9";
                RSI_param_string = "14;3;-1";
                BOLL_param_string = "2;14;-1";
                STOCH_param_string = "7;14;3";
                STOCH_RSI_param_string = "14;-1;-1";


                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                //IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                //System.Windows.Forms.MessageBox.Show("Configure");

                bDataLoaded = false;

                if (!OnlyGeneratePlots)
                {
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

                SetStopLoss(CalculationMode.Percent, 0.05);

                // Sets a 20 tick trailing stop for an open position
                //SetTrailStop(CalculationMode.Ticks, 20);
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

                if (OnlyGeneratePlots)
                {
                    mut_python.WaitOne();
                    string args = output_folder_with_date + "master_sorted.csv" + " " + NumPlotsToGenerate.ToString();
                    Debug.WriteLine("Python args" + args);
                    CallPythonScript(output_folder + "\\Data_Processing50.py", args);
                    mut_python.ReleaseMutex();
                    Debug.WriteLine("After Python mutex");
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

                if (Sanitize == false)
                    newLine = string.Format("{0},{1},{2},{3},{4}", date, price, indicator_value, future_value_change, SP500_price);

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

            List<List<String>> param_list = new List<List<String>>();

            param_list.Add(new List<String>() { "uid", uid, "str" });
            param_list.Add(new List<String>() { "KNN_num_neighbors", KNN_num_neighbors.ToString(), "int" });
            param_list.Add(new List<String>() { "num_training_pts", num_training_pts.ToString(), "int" });
            param_list.Add(new List<String>() { "num_groups", num_groups.ToString(), "int" });
            param_list.Add(new List<String>() { "stagger_factor", stagger_factor.ToString(), "int" });
            param_list.Add(new List<String>() { "window_size", window_size.ToString(), "int" });
            param_list.Add(new List<String>() { "avg_target", avg_target.ToString(), "float" });
            param_list.Add(new List<String>() { "good_target", good_target.ToString(), "float" });
            param_list.Add(new List<String>() { "bad_target", bad_target.ToString(), "float" });
            param_list.Add(new List<String>() { "min_samples_pct", min_samples_pct.ToString(), "float" });
            param_list.Add(new List<String>() { "thresh1", thresh1.ToString(), "float" });
            param_list.Add(new List<String>() { "thresh2", thresh2.ToString(), "float" });
            param_list.Add(new List<String>() { "future_lookahead", FutureValueDaysToLookAhead.ToString(), "int" });
            param_list.Add(new List<String>() { "s_name", symbol, "str" });
            param_list.Add(new List<String>() { "co_name", company, "str" });
            param_list.Add(new List<String>() { "description", description, "str" });
            param_list.Add(new List<String>() { "indicator", indicator, "str" });
            param_list.Add(new List<String>() { "param1", Param1.ToString(), "int" });
            param_list.Add(new List<String>() { "param2", Param2.ToString(), "int" });
            param_list.Add(new List<String>() { "param3", Param3.ToString(), "int" });
            param_list.Add(new List<String>() { "training_file", training_file_path, "str" });
            param_list.Add(new List<String>() { "training_start", TrainingStartDate.ToShortDateString(), "str" });
            param_list.Add(new List<String>() { "training_end", TrainingEndDate.ToShortDateString(), "str" });
            param_list.Add(new List<String>() { "testing_file", testing_file_path, "str" });
            param_list.Add(new List<String>() { "testing_start", TestingStartDate.ToShortDateString(), "str" });
            param_list.Add(new List<String>() { "testing_end", TestingEndDate.ToShortDateString(), "str" });
            param_list.Add(new List<String>() { "output_csv_file", output_file_base + "data" + unique_id + ".csv", "str" });
            param_list.Add(new List<String>() { "output_txt_file", output_file_base + "summary" + unique_id + ".txt", "str" });
            param_list.Add(new List<String>() { "output_pdf_file", output_file_base + "plot" + unique_id + ".pdf", "str" });

            var cfg = new StringBuilder();

            for (int i = 0; i < param_list.Count; i++)
            {
                cfg.AppendLine(param_list[i][0] + "=" + param_list[i][1] + "#" + param_list[i][2]);
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

        public void BuildIndicatorAndDescriptionStrings(IndicatorEnum this_ind) //, int Param1, int Param2, int Param3)
        {


            switch (this_ind)
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

            description = description + " - " + uid;
            
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
                case IndicatorEnum.indicator_MACD:
                    param_string = MACD_param_string;
                    break;
                case IndicatorEnum.indicator_RSI:
                    param_string = RSI_param_string;
                    break;
                case IndicatorEnum.indicator_BOLLINGER:
                    param_string = BOLL_param_string;
                    break;
                case IndicatorEnum.indicator_STOCHASTIC:
                    param_string = STOCH_param_string;
                    break;
                case IndicatorEnum.indicator_STOCHASTIC_RSI:
                    param_string = STOCH_RSI_param_string;
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

        protected override void OnBarUpdate()
        {
            if (OnlyGeneratePlots)
                return;

            int BarIndex = BarsInProgress;

            bool bFinishedProcessingAllData = false;

            String debug_txt1 = "Date=" + Times[BarIndex][0].Date.ToShortDateString() + "BarIndex=" + BarIndex.ToString() + "CurrentBar=" + CurrentBar.ToString() + "Count=" +Count.ToString();
            Debug.WriteLine(debug_txt1);

            DateTime current_bar_date = Times[BarIndex][0].Date;

            Boolean bLastSymbol = BarIndex == (symbol_list.Count - 1);

            if (current_bar_date > TestingEndDate)
            {
                return;
            }
            else if ( bLastSymbol &&
                (((CurrentBar+1) == Count) || (current_bar_date == TestingEndDate)) )  //we are on the last bar to process
            {
                Debug.WriteLine("bFinished trigger");
                bFinishedProcessingAllData = true;
            }

            //NOTE: Index 1 is always S&P 500
            if ( ((ProcessAllTickers == true) && (BarIndex >= 1) && (BarIndex < (symbol_list.Count))) ||  //Start at index=1 since we want to ignore the primary/dummy instrument
                    ((ProcessAllTickers == false) && (BarIndex == 0)) )
            {

                if (CurrentBar >= (FutureValueDaysToLookAhead))  //just make sure we avoid out of bounds error in case we don't yet have enough data
                {
                    DateTime date_to_process = Times[BarIndex][FutureValueDaysToLookAhead].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                    bool bWithinTrainingPeriod = (date_to_process >= TrainingStartDate) && (date_to_process <= TrainingEndDate);
                    bool bWithinTestingPeriod = (date_to_process >= TestingStartDate) && (date_to_process <= TestingEndDate);

                    if ((bWithinTrainingPeriod) || (bWithinTestingPeriod))
                    {
                        for (int i = 0; i < indicator_list.Count; i++)
                        {

                            PopulateParamatersForIndicator(indicator_list[i]);
                            Indicator_FutureValueChange_Pair indicator_pair = GetIndicatorValue(indicator_list[i], BarIndex, FutureValueDaysToLookAhead, date_to_process);

                            if (bWithinTrainingPeriod)
                            {
                                master_list_training[i][BarIndex].Add(indicator_pair);
                            }
                            else if (bWithinTestingPeriod)
                            {
                                master_list_testing[i][BarIndex].Add(indicator_pair);
                            }
                        }
                    }
                 }
            }
            
            if (bFinishedProcessingAllData)
            {
                String debug_txt2 = "finished processing all data"; 
                Debug.WriteLine(debug_txt2);
                for (int bar_index=1; bar_index<symbol_list.Count; bar_index++)
                {
                    for (int i = 0; i < indicator_list.Count; i++)
                    {
                        int num_bars_for_symbol = Times[bar_index].Count;
                        if (CurrentBar < num_bars_for_symbol)  //Check for missing data
                        {
                            for (int j = (FutureValueDaysToLookAhead - 1); j >= 0; j--)
                            {
                                DateTime date_to_process = Times[bar_index][j].Date;
                                debug_txt2 = "date_to_process=" + date_to_process.ToShortDateString();
                                Debug.WriteLine(debug_txt2);

                                PopulateParamatersForIndicator(indicator_list[i]);
                                Indicator_FutureValueChange_Pair indicator_pair = GetIndicatorValue(indicator_list[i], bar_index, j, date_to_process);

                                //Add to the "Testing" lists since these should be the ones capturing most recent values
                                master_list_testing[i][bar_index].Add(indicator_pair);
                            }
                        }
                    }
                }

                uid = rnd.Next(100000, 999999).ToString();
                String unique_id = "_" + uid; // creates a number between 100k and 999k
                String control_file_path = output_folder_with_date + "control_file" + unique_id + ".txt";
                String consolidated_txt_file_path = output_folder_with_date + "consolidated_report" + unique_id + ".txt";
                String consolidated_csv_file_path = output_folder_with_date + "consolidated_report" + unique_id + ".csv";

                bool bFirstTime = true;
                int start_index = 1;
                int max_index = symbol_list.Count;
                if (ProcessAllTickers == false)
                {
                    start_index = 0;
                    max_index = 1;
                }
                for (int j = 0; j < indicator_list.Count; j++)
                {
                    uid = rnd.Next(100000, 999999).ToString();
                    unique_id = "_" + uid; // creates a number between 100k and 999k

                    String debug_txt = "FINISHEDPROCESSING min_samples=" + min_samples_pct.ToString() + " id=" + unique_id.ToString();
                    Debug.WriteLine(debug_txt);

                    PopulateParamatersForIndicator(indicator_list[j]);
                    BuildIndicatorAndDescriptionStrings(indicator_list[j]);
                    
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

                        String base_file_path_for_symbol = output_folder_with_date + symbol_name;  //i.e. c:\temp\knn\AAPL

                        String training_file_path = base_file_path_for_symbol + "_training" + unique_id + ".csv";
                        String testing_file_path = base_file_path_for_symbol + "_testing" + unique_id + ".csv";
                        WriteListToCSV(master_list_training[j][i], training_file_path);
                        WriteListToCSV(master_list_testing[j][i], testing_file_path);

                        String config_file_name = "";
                        config_file_name = GenerateConfigFile(base_file_path_for_symbol, symbol_name, company_name, training_file_path, testing_file_path, unique_id);

                        if (bFirstTime)  //create new file
                        {
                            bFirstTime = false;
                            File.WriteAllText(control_file_path, config_file_name + "\r\n");
                        }
                        else // append to existing file
                        {
                            File.AppendAllText(control_file_path, config_file_name + "\r\n");
                        }
                    }
                }
                Debug.WriteLine("Before Python mutex");
                //Now we can call python for all symbols at once by passing in the control_file as cmd line arg
                mut_python.WaitOne();
                string mode = " BEST";
                if (FindBestSolutions == false)
                    mode = " RECENT";
                string args = control_file_path + " " + consolidated_txt_file_path + " " + consolidated_csv_file_path + " " + output_folder_with_date + "master_report.csv" + " " + output_folder_with_date + "master_trig.csv" + " false" + " false" + mode + " " + output_folder_with_date + "master_sorted.csv" + " " + GeneratePlotsForBestSolutions.ToString() + " " + NumPlotsToGenerate.ToString();
                Debug.WriteLine("Python args" + args);
                CallPythonScript(output_folder + "\\Data_Processing46.py", args);
                mut_python.ReleaseMutex();
                Debug.WriteLine("After Python mutex");
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

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "DaysToLookBack", GroupName = "NinjaScriptStrategyParameters", Order = 4)]
        public int DaysToLookBack
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "FutureValueDaysToLookAhead", GroupName = "NinjaScriptStrategyParameters", Order = 5)]
        public int FutureValueDaysToLookAhead
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "MACD Params", GroupName = "Indicator", Order = 4)]
        public String MACD_param_string
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "RSI Params", GroupName = "Indicator", Order = 5)]
        public String RSI_param_string
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "BOLLINGER Params", GroupName = "Indicator", Order = 6)]
        public String BOLL_param_string
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "STOCHASTIC Params", GroupName = "Indicator", Order = 7)]
        public String STOCH_param_string
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "STOCH_RSI Params", GroupName = "Indicator", Order = 8)]
        public String STOCH_RSI_param_string
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

        [TypeConverter(typeof(FriendlyBoolConverter))] // Converts the bool to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
        [Display(Name = "Generate Plots for Best Solutions", GroupName = "Custom", Order = 14)]
        public bool GeneratePlotsForBestSolutions
        { get; set; }

        [Range(0, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Num Plots to Generate", GroupName = "Custom", Order = 15)]
        public int NumPlotsToGenerate
        { get; set; }

        [TypeConverter(typeof(FriendlyBoolConverter))] // Converts the bool to string values
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")] // Create the combo box on the property grid
        [Display(Name = "Only Generate Plots", GroupName = "Custom", Order = 16)]
        public bool OnlyGeneratePlots
        { get; set; }


    }
    //{ get; set; }

    #endregion




}
