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
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class GBOLL : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Bollinger Percentage";
				Name										= "GBOLL";
				Calculate									= Calculate.OnEachTick;
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
				
				NumStdDev					= 2;
				Period						= 14;
				
				AddPlot(Brushes.Orange, "GBOLL_val");
				AddLine(Brushes.DarkCyan,	1.0,	NinjaTrader.Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.Gray,		0.0,	NinjaTrader.Custom.Resource.NinjaScriptIndicatorMiddle);
				AddLine(Brushes.DarkCyan,	-1.0,	NinjaTrader.Custom.Resource.NinjaScriptIndicatorUpper);
			
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			
			double upper_band = Bollinger(NumStdDev, Period).Upper[0];  //get the indicator val from n days ago
			double middle_band = Bollinger(NumStdDev, Period).Middle[0];  //get the indicator val from n days ago
			double lower_band = Bollinger(NumStdDev, Period).Lower[0];  //get the indicator val from n days ago
            double current_price = Input[0];
            double diff = current_price - middle_band;
            double band_range = upper_band - middle_band;
            double indicator_value = diff / band_range; //how far current price is from the middle band (-1.0 means we're at the lower band, +1 means we're at the upper band)

			Value[0] = indicator_value; //Input[0] * 2.0; //indicator_value;
		}
		
		#region Properties
		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NumStdDev", GroupName = "NinjaScriptParameters", Order = 0)]
		public double NumStdDev
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Period
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GBOLL_val
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
		private GBOLL[] cacheGBOLL;
		public GBOLL GBOLL(double numStdDev, int period)
		{
			return GBOLL(Input, numStdDev, period);
		}

		public GBOLL GBOLL(ISeries<double> input, double numStdDev, int period)
		{
			if (cacheGBOLL != null)
				for (int idx = 0; idx < cacheGBOLL.Length; idx++)
					if (cacheGBOLL[idx] != null && cacheGBOLL[idx].NumStdDev == numStdDev && cacheGBOLL[idx].Period == period && cacheGBOLL[idx].EqualsInput(input))
						return cacheGBOLL[idx];
			return CacheIndicator<GBOLL>(new GBOLL(){ NumStdDev = numStdDev, Period = period }, input, ref cacheGBOLL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GBOLL GBOLL(double numStdDev, int period)
		{
			return indicator.GBOLL(Input, numStdDev, period);
		}

		public Indicators.GBOLL GBOLL(ISeries<double> input , double numStdDev, int period)
		{
			return indicator.GBOLL(input, numStdDev, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GBOLL GBOLL(double numStdDev, int period)
		{
			return indicator.GBOLL(Input, numStdDev, period);
		}

		public Indicators.GBOLL GBOLL(ISeries<double> input , double numStdDev, int period)
		{
			return indicator.GBOLL(input, numStdDev, period);
		}
	}
}

#endregion
