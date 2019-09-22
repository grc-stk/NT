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
	public class GPCT : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Pct Change (1 Day)";
				Name										= "GPCT";
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
				
				DaysAgo = 1;
								
				AddPlot(Brushes.Orange, "GPCT_val");
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
            double indicator_value = 0.0;

            if (CurrentBar != 0)
            {
				double current_price = Input[0];
				double yesterday_price = Input[DaysAgo];
				indicator_value = ((current_price/yesterday_price) - 1.0) * 100.0;
			}
			
			Value[0] = indicator_value;
		}
		
		#region Properties
		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "DaysAgo", GroupName = "NinjaScriptParameters", Order = 0)]
		public int DaysAgo
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GPCT_val
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
		private GPCT[] cacheGPCT;
		public GPCT GPCT(int daysAgo)
		{
			return GPCT(Input, daysAgo);
		}

		public GPCT GPCT(ISeries<double> input, int daysAgo)
		{
			if (cacheGPCT != null)
				for (int idx = 0; idx < cacheGPCT.Length; idx++)
					if (cacheGPCT[idx] != null && cacheGPCT[idx].DaysAgo == daysAgo && cacheGPCT[idx].EqualsInput(input))
						return cacheGPCT[idx];
			return CacheIndicator<GPCT>(new GPCT(){ DaysAgo = daysAgo }, input, ref cacheGPCT);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GPCT GPCT(int daysAgo)
		{
			return indicator.GPCT(Input, daysAgo);
		}

		public Indicators.GPCT GPCT(ISeries<double> input , int daysAgo)
		{
			return indicator.GPCT(input, daysAgo);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GPCT GPCT(int daysAgo)
		{
			return indicator.GPCT(Input, daysAgo);
		}

		public Indicators.GPCT GPCT(ISeries<double> input , int daysAgo)
		{
			return indicator.GPCT(input, daysAgo);
		}
	}
}

#endregion
