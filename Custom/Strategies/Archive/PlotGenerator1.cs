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


    public class PlotGenerator1 : Strategy//, INotifyPropertyChanged
    {
        

        private static Mutex mut_python = new Mutex(false, "Python");
        
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                
                Description = "test";
                Name = "PlotGenerator1";
                Calculate = Calculate.OnEachTick;  //This will let us get the last Bar

                EndDate = DateTime.Today;
                SortedMasterFilePath = "c:\\temp\\knn\\";
                NumPlotsToGenerate = 20;

                
                // This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                //IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                mut_python.WaitOne();
                string args = SortedMasterFilePath + EndDate.ToString("ddMMMyyyy") + "\\master_sorted.csv" + " " + NumPlotsToGenerate.ToString();
                Debug.WriteLine("Python args" + args);
                CallPythonScript("c:\\temp\\knn" + "\\Data_Processing46.py", args);
                mut_python.ReleaseMutex();
                Debug.WriteLine("After Python mutex");
               
            }
            else if ((State == State.Transition) || (State == State.Terminated))  //finished processing historical data (and ready for real-time)
            {
                
            }
        }

        
        private void CallPythonScript(string script_file_path, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            //string Python_Path = "C:/Python27/python.exe";
            string Python_Path = "C:/ProgramData/Anaconda3/python.exe";
            start.FileName = "cmd.exe";
            start.Arguments = "/C " + Python_Path + string.Format(" {0} {1}", script_file_path, args) + " >>" + SortedMasterFilePath + EndDate.ToString("ddMMMyyyy") +"\\python_log.txt 2>&1";
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
        

        #region Properties

        [Display(ResourceType = typeof(Custom.Resource), Name = "End Date", GroupName = "Custom", Order = 1)]
        [Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
        public DateTime EndDate { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Base File Path", GroupName = "Custom", Order = 2)]
        public String SortedMasterFilePath
        { get; set; }

        
        [Range(0, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Num Plots to Generate", GroupName = "Custom", Order = 3)]
        public int NumPlotsToGenerate
        { get; set; }
        

    }
    //{ get; set; }

    #endregion




}
