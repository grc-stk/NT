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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;

// Add this to your declarations to use StreamWriter
using System.IO;

#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class GregIndicator1 : Indicator
	{
		private string path;
		private StreamWriter sw; // a variable for the StreamWriter that will be used
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Greg's test indicator";
				Name										= "GregIndicator1";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				My_input_parameter					= 2;
				AddPlot(Brushes.Orange, "My_indicator_value");
				
				path 										= NinjaTrader.Core.Globals.UserDataDir + "MyTestFile.txt"; // Define the Path to our test file
			}
			else if (State == State.Configure)
			{
			}
			// Necessary to call in order to clean up resources used by the StreamWriter object
			else if(State == State.Terminated)
			{
				if (sw != null)
				{
					sw.Close();
					sw.Dispose();
					sw = null;
				}
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			Value[0] = Input[0] * My_input_parameter;
			
			sw = File.AppendText(path);  // Open the path for writing
			sw.WriteLine(Time[0] + " " + Open[0] + " " + High[0] + " " + Low[0] + " " + Close[0] + " " + Value[0]); // Append a new line to the file
			sw.Close(); // Close the file to allow future calls to access the file again.
			
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="My_input_parameter", Description="test parameter", Order=1, GroupName="Parameters")]
		public double My_input_parameter
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> My_indicator_value
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GregIndicator1[] cacheGregIndicator1;
		public GregIndicator1 GregIndicator1(double my_input_parameter)
		{
			return GregIndicator1(Input, my_input_parameter);
		}

		public GregIndicator1 GregIndicator1(ISeries<double> input, double my_input_parameter)
		{
			if (cacheGregIndicator1 != null)
				for (int idx = 0; idx < cacheGregIndicator1.Length; idx++)
					if (cacheGregIndicator1[idx] != null && cacheGregIndicator1[idx].My_input_parameter == my_input_parameter && cacheGregIndicator1[idx].EqualsInput(input))
						return cacheGregIndicator1[idx];
			return CacheIndicator<GregIndicator1>(new GregIndicator1(){ My_input_parameter = my_input_parameter }, input, ref cacheGregIndicator1);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GregIndicator1 GregIndicator1(double my_input_parameter)
		{
			return indicator.GregIndicator1(Input, my_input_parameter);
		}

		public Indicators.GregIndicator1 GregIndicator1(ISeries<double> input , double my_input_parameter)
		{
			return indicator.GregIndicator1(input, my_input_parameter);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GregIndicator1 GregIndicator1(double my_input_parameter)
		{
			return indicator.GregIndicator1(Input, my_input_parameter);
		}

		public Indicators.GregIndicator1 GregIndicator1(ISeries<double> input , double my_input_parameter)
		{
			return indicator.GregIndicator1(input, my_input_parameter);
		}
	}
}

#endregion
