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
    
    public class GregTestStrategy2 : Strategy
    {
        public class Indicator_FutureValueChange_Pair
	    {
	        public DateTime Date { get; set; }
	        public double Indicator { get; set; }
	        public double FutureValueChange { get; set; }
	    }
	
		private double Buy_Price = 0.0;
        private double BarBought = 0;

        private GregIndicator1 greg1;

        private Order entryOrder = null;

        private List<Indicator_FutureValueChange_Pair> list_Indicator_FutureValueChange_Training = new List<Indicator_FutureValueChange_Pair>();
        private List<Indicator_FutureValueChange_Pair> list_Indicator_FutureValueChange_Testing = new List<Indicator_FutureValueChange_Pair>();

        private int count = 0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "test";
                Name = "GregTestStrategy2";
                DaysToLookBack = 1;
                FutureValueDaysToLookAhead = 5;

                TrainingStartDate = new DateTime(2017, 07, 01);
                TrainingEndDate = new DateTime(2017, 07, 28);
                TestingStartDate = new DateTime(2017, 08, 01);
                TestingEndDate = new DateTime(2017, 12, 31);

                //KNN params
                MinIndicatorValue = -2.0f;
                MaxIndicatorValue = 2.0f;
                NumNeighbors = 3;


                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                // Add an MSFT 1 day Bars object to the strategy
                AddDataSeries("MSFT", Data.BarsPeriodType.Day, 1);

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
                String training_file_path = "C:\\temp\\ninjatrader_training.csv";
                String testing_file_path = "C:\\temp\\ninjatrader_testing.csv";
                WriteListToCSV(list_Indicator_FutureValueChange_Training, training_file_path);
                WriteListToCSV(list_Indicator_FutureValueChange_Testing, testing_file_path);

                String training_args = training_file_path + " " + MinIndicatorValue.ToString() +
                                                            " " + MaxIndicatorValue.ToString() +
                                                            " " + NumNeighbors.ToString();

                CallPythonScript("C:\\GregPython\\Examples\\Greg_HW\\Greg_NinjaTrader_1.py", training_args);
            }
        }

        private void CallPythonScript(string script_file_path, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "C:/Python27/python.exe";
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
        protected override void OnBarUpdate()
        {
            double Current_Value = Closes[0][0];  //get the most recent Close data for the input symbol (index 0)
            double Prev_Value = Closes[0][0];

            double greg_indicator_value = GregIndicator1(BarsArray[0], 2.0)[0];

            if (CurrentBar >= DaysToLookBack)  //make sure we have enough data loaded to look back n days
            {
                Prev_Value = Closes[0][DaysToLookBack];
            }

            //Value[0] = CurrentBar == 0 ? 0 : Input[0] - Input[Math.Min(CurrentBar, Period)];

            //double Current_Value = Close[0];  
            //double Prev_Value = Close[0];

            //double greg_indicator_value = greg1[0];

            //if (CurrentBar >= DaysToLookBack)  //make sure we have enough data loaded to look back n days
            //{
            //	Prev_Value = Close[DaysToLookBack];
            //}




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

            if (BarsInProgress == 0)  //if we're looking at data for primary instrument
            {

                if (CurrentBar >= (FutureValueDaysToLookAhead))  //just make sure we avoid out of bounds error in case we don't yet have enough data
                {
                    //DateTime current_date = Times[0][0].Date;
                    DateTime date_to_process = Times[0][FutureValueDaysToLookAhead].Date;  //need to wait FutureValueDaysToLookAhead days before we can make calcs

                    bool bWithinTrainingPeriod = (date_to_process >= TrainingStartDate) && (date_to_process <= TrainingEndDate);
                    bool bWithinTestingPeriod = (date_to_process >= TestingStartDate) && (date_to_process <= TestingEndDate);


                    //if ((minimum_date >= TrainingStartDate) && (minimum_date <= TrainingEndDate) || //if we are within training period
                    //    (minimum_date >= TestingStartDate) && (minimum_date <= TestingEndDate))    //or within the testing period
                    if ((bWithinTrainingPeriod) || (bWithinTestingPeriod))
                    {
                        //TimeSpan diff = TrainingEndDate - TrainingStartDate;
                        //int training_days = (int)Math.Abs(Math.Round(diff.TotalDays));
                        
                        double indicator_value = MACD(BarsArray[0], 12, 26, 9)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago
                        //double indicator_value = GregIndicator1(BarsArray[0], 2.0)[FutureValueDaysToLookAhead];  //get the indicator val from n days ago

                        double future_price_change = 0.0;
                        double future_price = Closes[0][0];
                        double start_price = Closes[0][FutureValueDaysToLookAhead];
                        future_price_change = ((future_price / start_price) - 1.0) * 100.0;

                        Indicator_FutureValueChange_Pair indicator_pair = new Indicator_FutureValueChange_Pair();
                        indicator_pair.Date = date_to_process;
                        indicator_pair.Indicator = indicator_value;
                        indicator_pair.FutureValueChange = future_price_change;

                        if (bWithinTrainingPeriod)
                        {
                            list_Indicator_FutureValueChange_Training.Add(indicator_pair);
                            int count_training = list_Indicator_FutureValueChange_Training.Count;
                        }
                        else if (bWithinTestingPeriod)
                        {
                            list_Indicator_FutureValueChange_Testing.Add(indicator_pair);
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

        [Range(-float.MaxValue, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "MinIndicatorValue", GroupName = "KNN", Order = 6)]
        public float MinIndicatorValue
        { get; set; }

        [Range(-float.MaxValue, float.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "MaxIndicatorValue", GroupName = "KNN", Order = 7)]
        public float MaxIndicatorValue
        { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "NumNeighbors", GroupName = "KNN", Order = 8)]
        public int NumNeighbors
        { get; set; }

        #endregion
    }
}
