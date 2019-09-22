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
#endregion



//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
    

    public class KNN_Generator2 : Strategy
    {
        public class Indicator_FutureValueChange_Pair
	    {
	        public DateTime Date { get; set; }
	        public double Indicator { get; set; }
	        public double FutureValueChange { get; set; }
	    }

        
        string description = "MACD(12,26,9)";


        String[] symbol_list = new String[] { "SLB", "MSFT", "AAPL", "GOOG" };
		
		int symbol_index = 3;
	
		private double Buy_Price = 0.0;
        private double BarBought = 0;

        private GregIndicator1 greg1;

        private Order entryOrder = null;

		private List<List<Indicator_FutureValueChange_Pair>> list_Indicator_FutureValueChange_Training_ALL = new List<List<Indicator_FutureValueChange_Pair>>();
        private List<List<Indicator_FutureValueChange_Pair>> list_Indicator_FutureValueChange_Testing_ALL = new List<List<Indicator_FutureValueChange_Pair>>();

        private List<Indicator_FutureValueChange_Pair> list_Indicator_FutureValueChange_Training = new List<Indicator_FutureValueChange_Pair>();
        private List<Indicator_FutureValueChange_Pair> list_Indicator_FutureValueChange_Testing = new List<Indicator_FutureValueChange_Pair>();

        private int count = 0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "test";
                Name = "KNN_Generator2";
                DaysToLookBack = 1;
                FutureValueDaysToLookAhead = 5;

                TrainingStartDate = new DateTime(2017, 01, 01);
                TrainingEndDate = new DateTime(2017, 06, 30);
                TestingStartDate = new DateTime(2017, 07, 01);
                TestingEndDate = new DateTime(2017, 12, 31);

                //Custom params
                KNN_num_neighbors = 3;
                num_training_pts = 200;
                samples_per_group = 10;
                window_size = 1;
                avg_target = 0.5f;
                good_target = 0.1f;
                bad_target = 0.25f;
                min_samples = 10;
                thresh1 = 1.0f;
                thresh2 = -1.0f;
                output_folder = "C:\\temp\\knn\\";


                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                
				
				for (int i=0; i<symbol_list.Length; i++)
				{	
					// Add an MSFT 1 day Bars object to the strategy
	                AddDataSeries(symbol_list[i], Data.BarsPeriodType.Day, 1);

                    //private List<Indicator_FutureValueChange_Pair> list_Indicator_FutureValueChange_Training = new List<Indicator_FutureValueChange_Pair>();

                    list_Indicator_FutureValueChange_Training_ALL.Add(new List<Indicator_FutureValueChange_Pair>());
                    list_Indicator_FutureValueChange_Testing_ALL.Add(new List<Indicator_FutureValueChange_Pair>());

                }

                SetStopLoss(CalculationMode.Percent, 0.05);

                // Sets a 20 tick trailing stop for an open position
                //SetTrailStop(CalculationMode.Ticks, 20);
            }
            else if (State == State.DataLoaded)
            {
                double input_param = 2.0;
                greg1 = GregIndicator1(input_param);

                AddChartIndicator(greg1);
            }
            else if (State == State.Transition)  //finished processing historical data (and ready for real-time)
            {
                String control_file_path = output_folder + "control_file.txt";

                for (int i = 0; i < symbol_list.Length; i++)
                {
                    
                    String base_file_path_for_symbol = output_folder + symbol_list[i];  //i.e. c:\temp\knn\AAPL
                    String training_file_path = base_file_path_for_symbol + "_training.csv";
                    String testing_file_path = base_file_path_for_symbol + "_testing.csv";
                    WriteListToCSV(list_Indicator_FutureValueChange_Training_ALL[i], training_file_path);
                    WriteListToCSV(list_Indicator_FutureValueChange_Testing_ALL[i], testing_file_path);

                    String config_file_name = "";
                    config_file_name = GenerateConfigFile(base_file_path_for_symbol, symbol_list[i], training_file_path, testing_file_path);

                    if (i == 0)  //create new file
                    {
                        File.WriteAllText(control_file_path, config_file_name +"\r\n");
                    }
                    else // append to existing file
                    {
                        File.AppendAllText(control_file_path, config_file_name +"\r\n");
                    }

                    
                }

                //Now we can call python for all symbols at once by passing in the control_file as cmd line arg
                CallPythonScript(output_folder + "\\Data_Processing18.py", control_file_path);
            }
        }

        private void CallPythonScript(string script_file_path, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            //start.FileName = "C:/Python27/python.exe";
            start.FileName = "C:/ProgramData/Anaconda3/python.exe";
        
            start.Arguments = string.Format("{0} {1}", script_file_path, args);
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
                double indicator_value = list_to_write[i].Indicator;
                double future_value_change = list_to_write[i].FutureValueChange;

                var newLine = string.Format("{0},{1},{2}", date, indicator_value, future_value_change);
                csv.AppendLine(newLine);
            }

            File.WriteAllText(filePath, csv.ToString());

        }

        protected String GenerateConfigFile(String filePath, String symbol, String training_file_path, String testing_file_path)
        {
			
			String output_file_base = filePath + "_results_";
            String config_file_name = filePath + "_config.txt";

            List<String> labels = new List<String>
                                    {  "KNN_num_neighbors",
                                        "num_training_pts",
                                        "samples_per_group",
                                        "window_size",
                                        "avg_target",
                                        "good_target",
                                        "bad_target",
                                        "min_samples",
                                        "thresh1",
                                        "thresh2",
                                        "s_name",
                                        "description",
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
                                    {   KNN_num_neighbors.ToString(),
                                        num_training_pts.ToString(),
                                        samples_per_group.ToString(),
                                        window_size.ToString(),
                                        avg_target.ToString(),
                                        good_target.ToString(),
                                        bad_target.ToString(),
                                        min_samples.ToString(),
                                        thresh1.ToString(),
                                        thresh2.ToString(),
                                        symbol,
                                        description,
                                        training_file_path,
                                        TrainingStartDate.ToShortDateString(),
                                        TrainingEndDate.ToShortDateString(),
                                        testing_file_path,
                                        TestingStartDate.ToShortDateString(),
                                        TestingEndDate.ToShortDateString(),
                                        output_file_base + "data.csv",
                                        output_file_base + "summary.txt",
                                        output_file_base + "plot.pdf",
                                        };

            List<String> types = new List<String>
                                    {   "int",
                                        "int",
                                        "int",
                                        "int",
                                        "float",
                                        "float",
                                        "float",
                                        "int",
                                        "float",
                                        "float",
                                        "str",
                                        "str",
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
            
            File.WriteAllText(config_file_name , cfg.ToString());

            return (config_file_name);

        }
        protected override void OnBarUpdate()
        {
            double Current_Value = Closes[0][0];  //get the most recent Close data for the input symbol (index 0)
            double Prev_Value = Closes[0][0];

            double greg_indicator_value = GregIndicator1(BarsArray[0], 2.0)[0];

            if (CurrentBar >= DaysToLookBack)  //make sure we have enough data loaded to look back n days
            {
                Prev_Value = Closes[0][DaysToLookBack];
            }

            

            //			if (Current_Value >
            //			double Prev_Value = Close[CurrentBar - DaysToLookBack];
            double Percent_Change = (Current_Value / Prev_Value) - 1.0;

            if (Buy_Price != 0.0)
            {
                if ((Current_Value / Buy_Price - 1.0) > 0.02)  //sell if gain > 2%
                {
                    Buy_Price = 0.0;
                    //ExitLong();
                    //ExitLong("Percent Drop", "Percent Drop");
                    ExitLong(0, 100, "Exit Long from Percent Drop", "Percent Drop");
                    //ExitLong("Selling - Went + 2%");
                    //EnterShort("Selling - Went + 2%");
                }
                //else if ((CurrentBar - BarBought) > 20)  //sell after 20 days
                //{
                //                Buy_Price = 0.0;
                //                ExitLong();
                //                //ExitLong("Selling - Waited 20 days");
                //                //EnterShort("Selling - Waited 20 days");
                //            }
            }

            if (Percent_Change < -0.02)
            {
                //if (entryOrder == null)
                //{
                EnterLong(0, 100, "Percent Drop");
                //}

                //EnterLong();
                //EnterLong("Buying - Dropped 2%");
                //EnterLong(0, 100, "Percent Drop");
                //SetTrailStop(CalculationMode.Percent, 0.02);
                //SetStopLoss(CalculationMode.Percent, 0.05);
                Buy_Price = Current_Value;
                BarBought = CurrentBar;
                //SetTrailStop(CalculationMode.Percent, 0.01);
            }
			
			int BarIndex = BarsInProgress;

            if ((BarIndex >=0) && (BarIndex <=3)) //symbol_index)  //if we're looking at data for primary instrument
            {

                if (CurrentBar >= (FutureValueDaysToLookAhead))  //just make sure we avoid out of bounds error in case we don't yet have enough data
                {
                    //DateTime current_date = Times[0][0].Date;
                    DateTime date_to_process = Times[BarIndex][FutureValueDaysToLookAhead].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                    bool bWithinTrainingPeriod = (date_to_process >= TrainingStartDate) && (date_to_process <= TrainingEndDate);
                    bool bWithinTestingPeriod = (date_to_process >= TestingStartDate) && (date_to_process <= TestingEndDate);


                    //if ((minimum_date >= TrainingStartDate) && (minimum_date <= TrainingEndDate) || //if we are within training period
                    //    (minimum_date >= TestingStartDate) && (minimum_date <= TestingEndDate))    //or within the testing period
                    if ((bWithinTrainingPeriod) || (bWithinTestingPeriod))
                    {
                        //TimeSpan diff = TrainingEndDate - TrainingStartDate;
                        //int training_days = (int)Math.Abs(Math.Round(diff.TotalDays));
                        
                        double indicator_value = MACD(BarsArray[BarIndex], 12, 26, 9)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                        //double indicator_value = GregIndicator1(BarsArray[0], 2.0)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago

                        double future_price_change = 0.0;
                        double future_price = Closes[BarIndex][0];
                        double start_price = Closes[BarIndex][FutureValueDaysToLookAhead];
                        future_price_change = ((future_price / start_price) - 1.0) * 100.0;

                        Indicator_FutureValueChange_Pair indicator_pair = new Indicator_FutureValueChange_Pair();
                        indicator_pair.Date = date_to_process;
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

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "KNN_num_neighbors", GroupName = "Custom", Order = 0)]
        public int KNN_num_neighbors
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "num_training_pts", GroupName = "Custom", Order = 1)]
        public int num_training_pts
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "samples_per_group", GroupName = "Custom", Order = 2)]
        public int samples_per_group
        { get; set; }

        [Range(0, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "window_size", GroupName = "Custom", Order = 3)]
        public int window_size
        { get; set; }

        [Range(0, 1.0f), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "avg_target", GroupName = "Custom", Order = 4)]
        public float avg_target
        { get; set; }

        [Range(0, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "good_target", GroupName = "Custom", Order = 5)]
        public float good_target
        { get; set; }

        [Range(0, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "bad_target", GroupName = "Custom", Order = 6)]
        public float bad_target
        { get; set; }

        [Range(0, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "min_samples", GroupName = "Custom", Order = 7)]
        public int min_samples
        { get; set; }

        [Range(-1.0f, 1.0f), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "thresh1", GroupName = "Custom", Order = 8)]
        public float thresh1
        { get; set; }

        [Range(-1.0f, 1.0f), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "thresh2", GroupName = "Custom", Order = 9)]
        public float thresh2
        { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "output_folder", GroupName = "Custom", Order = 10)]
        public String output_folder
        { get; set; }

        #endregion
    }
}
