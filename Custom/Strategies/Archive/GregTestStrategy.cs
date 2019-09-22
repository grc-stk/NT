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
#endregion

//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class GregTestStrategy : Strategy
	{
		private double Buy_Price = 0.0;
		private double BarBought = 0;
		
		private GregIndicator1 greg1;

        private Order entryOrder = null;

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description			= "test";
				Name				= "GregTestStrategy";
				DaysToLookBack		= 1;
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
            double Percent_Change = (Current_Value/Prev_Value) - 1.0;
			
			if (Buy_Price != 0.0)
			{
				if ((Current_Value/Buy_Price - 1.0) > 0.02)  //sell if gain > 2%
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
		}


        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
        {
            // Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
            // This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
            if (order.Name == "Percent Drop" && orderState != OrderState.Filled)
                entryOrder = order;
        }





        #region Properties
        [Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "DaysToLookBack", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
		public int DaysToLookBack
		{ get; set; }
		
		[Gui.PropertyEditor("NinjaTrader.Gui.Tools.DateEditorKey")]
		public DateTime MyTime {get; set;}

		#endregion
	}
}
