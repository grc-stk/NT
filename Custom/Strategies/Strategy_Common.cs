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
#endregion



//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{

    public class NT
    {
        public bool InRange(double value, double min, double max)
        {
            if ((value > min) && (value < max))
                return true;
            else
                return false;
        }
    }

    public class TradeScannerResult
    {
        public DateTime Date { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public string StrategyMatches { get; set; }
        public float Open { get; set; }
        public float Close { get; set; }
        public float Low { get; set; }
        public float High { get; set; }
        public float RelPosition { get; set; }   //How close to high (100% = close @ high, 0 = close @ low)
        public float PercentChange_Today{ get; set; }
        public float SP500_PercentChange_Today { get; set; }
        public float Compare_SP500_Today { get; set; }  //Compare symbol's % change today with S&P500
        public float PercentChange_1Day { get; set; }   //Percent change for this symbol over past N days
        public float PercentChange_2Day { get; set; }
        public float PercentChange_5Day { get; set; }
        public float PercentChange_10Day { get; set; }
        public float PercentChange_15Day { get; set; }
        public float PercentChange_20Day { get; set; }
        public float PercentChange_1DayAgo { get; set; }
        public float PercentChange_2DayAgo { get; set; }
        public float PercentChange_3DayAgo { get; set; }
        public float PercentChange_4DayAgo { get; set; }
        public float PercentChange_5DayAgo { get; set; }
        public float SP_PercentChange_1Day { get; set; }
        public float SP_PercentChange_2Day { get; set; }
        public float SP_PercentChange_5Day { get; set; }
        public float SP_PercentChange_10Day { get; set; }
        public float SP_PercentChange_15Day { get; set; }
        public float SP_PercentChange_20Day { get; set; }
        public float SP_PercentChange_1DayAgo { get; set; }
        public float SP_PercentChange_2DayAgo { get; set; }
        public float SP_PercentChange_3DayAgo { get; set; }
        public float SP_PercentChange_4DayAgo { get; set; }
        public float SP_PercentChange_5DayAgo { get; set; }
        public float Compare_SP_1Day { get; set; }
        public float Compare_SP_2Day { get; set; }
        public float Compare_SP_5Day { get; set; }
        public float Compare_SP_10Day { get; set; }
        public float Compare_SP_15Day { get; set; }
        public float Compare_SP_20Day { get; set; }
        public string Indicator_Name { get; set; }
        public float Indicator_Today { get; set; }      
        public float Indicator_Change_1Day { get; set; }    //See how indicator has changed over past N days
        public float Indicator_Change_2Day { get; set; }
        public float Indicator_Change_5Day { get; set; }
        public float Indicator_Change_10Day { get; set; }
        public float Indicator_Change_15Day { get; set; }
        public float Indicator_Change_20Day { get; set; }
        public float Green_2Day { get; set; } //% of Green days in the past N days
        public float Green_3Day { get; set; }
        public float Green_4Day { get; set; }
        public float Green_5Day { get; set; }
        public float Green_10Day { get; set; }
        public float Green_15Day { get; set; }
        public float Green_20Day { get; set; }
        public float Future_1Day { get; set; }
        public float Future_2Day { get; set; }
        public float Future_3Day { get; set; }
        public float Future_4Day { get; set; }
        public float Future_5Day { get; set; }
        public int Consec_Green { get; set; }
        public int Consec_Red { get; set; }
        public float boll_0 { get; set; }
        public float boll_1 { get; set; }
        public float boll_2 { get; set; }
        public float boll_3 { get; set; }
        public float boll_4 { get; set; }
        public float boll_5 { get; set; }
        public float close_0 { get; set; }
        public float close_1 { get; set; }
        public float close_2 { get; set; }
        public float close_3 { get; set; }
        public float close_4 { get; set; }
        public float close_5 { get; set; }
        public float open_0 { get; set; }
        public float open_1 { get; set; }
        public float open_2 { get; set; }
        public float open_3 { get; set; }
        public float open_4 { get; set; }
        public float open_5 { get; set; }
        public float low_0 { get; set; }
        public float low_1 { get; set; }
        public float low_2 { get; set; }
        public float low_3 { get; set; }
        public float low_4 { get; set; }
        public float low_5 { get; set; }
        public float high_0 { get; set; }
        public float high_1 { get; set; }
        public float high_2 { get; set; }
        public float high_3 { get; set; }
        public float high_4 { get; set; }
        public float high_5 { get; set; }
        public float RSI { get; set; }
        public float MACD { get; set; }
        public float STOCH_RSI { get; set; }
        public float STOCHASTIC { get; set; }
        public string Strategy_Name { get; set; }
    }

    public class TradeScannerResultOld
    {
        public DateTime Date { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public string StrategyMatches { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double Low { get; set; }
        public double High { get; set; }
        public double RelPosition { get; set; }   //How close to high (100% = close @ high, 0 = close @ low)
        public double PercentChange_Today { get; set; }
        public double SP500_PercentChange_Today { get; set; }
        public double Compare_SP500_Today { get; set; }  //Compare symbol's % change today with S&P500
        public double PercentChange_1Day { get; set; }   //Percent change for this symbol over past N days
        public double PercentChange_2Day { get; set; }
        public double PercentChange_5Day { get; set; }
        public double PercentChange_10Day { get; set; }
        public double PercentChange_15Day { get; set; }
        public double PercentChange_20Day { get; set; }
        public double PercentChange_1DayAgo { get; set; }
        public double PercentChange_2DayAgo { get; set; }
        public double PercentChange_3DayAgo { get; set; }
        public double PercentChange_4DayAgo { get; set; }
        public double PercentChange_5DayAgo { get; set; }
        public double SP_PercentChange_1Day { get; set; }
        public double SP_PercentChange_2Day { get; set; }
        public double SP_PercentChange_5Day { get; set; }
        public double SP_PercentChange_10Day { get; set; }
        public double SP_PercentChange_15Day { get; set; }
        public double SP_PercentChange_20Day { get; set; }
        public double SP_PercentChange_1DayAgo { get; set; }
        public double SP_PercentChange_2DayAgo { get; set; }
        public double SP_PercentChange_3DayAgo { get; set; }
        public double SP_PercentChange_4DayAgo { get; set; }
        public double SP_PercentChange_5DayAgo { get; set; }
        public double Compare_SP_1Day { get; set; }
        public double Compare_SP_2Day { get; set; }
        public double Compare_SP_5Day { get; set; }
        public double Compare_SP_10Day { get; set; }
        public double Compare_SP_15Day { get; set; }
        public double Compare_SP_20Day { get; set; }
        public string Indicator_Name { get; set; }
        public double Indicator_Today { get; set; }
        public double Indicator_Change_1Day { get; set; }    //See how indicator has changed over past N days
        public double Indicator_Change_2Day { get; set; }
        public double Indicator_Change_5Day { get; set; }
        public double Indicator_Change_10Day { get; set; }
        public double Indicator_Change_15Day { get; set; }
        public double Indicator_Change_20Day { get; set; }
        public double Green_2Day { get; set; } //% of Green days in the past N days
        public double Green_3Day { get; set; }
        public double Green_4Day { get; set; }
        public double Green_5Day { get; set; }
        public double Green_10Day { get; set; }
        public double Green_15Day { get; set; }
        public double Green_20Day { get; set; }
        public double Future_1Day { get; set; }
        public double Future_2Day { get; set; }
        public double Future_3Day { get; set; }
        public double Future_4Day { get; set; }
        public double Future_5Day { get; set; }
        public int Consec_Green { get; set; }
        public int Consec_Red { get; set; }
        public double boll_0 { get; set; }
        public double boll_1 { get; set; }
        public double boll_2 { get; set; }
        public double boll_3 { get; set; }
        public double boll_4 { get; set; }
        public double boll_5 { get; set; }
        public double close_0 { get; set; }
        public double close_1 { get; set; }
        public double close_2 { get; set; }
        public double close_3 { get; set; }
        public double close_4 { get; set; }
        public double close_5 { get; set; }
        public double open_0 { get; set; }
        public double open_1 { get; set; }
        public double open_2 { get; set; }
        public double open_3 { get; set; }
        public double open_4 { get; set; }
        public double open_5 { get; set; }
        public double low_0 { get; set; }
        public double low_1 { get; set; }
        public double low_2 { get; set; }
        public double low_3 { get; set; }
        public double low_4 { get; set; }
        public double low_5 { get; set; }
        public double high_0 { get; set; }
        public double high_1 { get; set; }
        public double high_2 { get; set; }
        public double high_3 { get; set; }
        public double high_4 { get; set; }
        public double high_5 { get; set; }
        public double RSI { get; set; }
        public double MACD { get; set; }
        public double STOCH_RSI { get; set; }
        public double STOCHASTIC { get; set; }
        public string Strategy_Name { get; set; }
    }
    public class Date_Price_Pair
    {
        public DateTime Date { get; set; }
        public double Price { get; set; }        
    }

    public class Indicator_FutureValueChange_Pair
    {
        public DateTime Date { get; set; }
        public float Price { get; set; }
        public float Indicator { get; set; }
        public float FutureValueChange { get; set; }
        public float SP500_Price { get; set; }
    }

    public class TradeTrigger
    {
        public string Symbol { get; set; }
        public IndicatorEnum Indicator { get; set; }
        public double StartRange { get; set; }
        public double EndRange { get; set; }
        public int DaysToHold { get; set; }
        public int Param1 { get; set; }
        public int Param2 { get; set; }
        public int Param3 { get; set; }
        public string Identifier { get; set; }
    }

    public class TradeTriggerByDate
    {
        public string Symbol { get; set; }
        public DateTime TradeDate { get; set; }
        public int DaysToHold { get; set; }
        public string Status { get; set; }
    }

    public enum IndicatorEnum
    {
        indicator_MACD,
        indicator_RSI,
        indicator_BOLLINGER,
        indicator_STOCHASTIC,
        indicator_STOCHASTIC_RSI,
        indicator_GREG
    }

    // Since this is only being applied to a specific property rather than the whole class,
    // we don't need to inherit from IndicatorBaseConverter and we can just use a generic TypeConverter
    public class IndicatorEnumConverter : TypeConverter
    {
        // Set the values to appear in the combo box
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values = new List<string>() { "MACD", "RSI", "BOLLINGER", "STOCHASTIC", "STOCH_RSI", "GREG"};

            return new StandardValuesCollection(values);
        }

        // map the value from "Friendly" string to MyEnum type
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringVal = value.ToString();
            switch (stringVal)
            {
                case "MACD":
                    return IndicatorEnum.indicator_MACD;
                case "RSI":
                    return IndicatorEnum.indicator_RSI;
                case "BOLLINGER":
                    return IndicatorEnum.indicator_BOLLINGER;
                case "STOCHASTIC":
                    return IndicatorEnum.indicator_STOCHASTIC;
                case "STOCH_RSI":
                    return IndicatorEnum.indicator_STOCHASTIC_RSI;
                case "GREG":
                    return IndicatorEnum.indicator_GREG;
            }
            return IndicatorEnum.indicator_MACD;  //default
        }

        // map the MyEnum type to "Friendly" string
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            IndicatorEnum enumVal = (IndicatorEnum)Enum.Parse(typeof(IndicatorEnum), value.ToString());
            switch (enumVal)
            {
                case IndicatorEnum.indicator_MACD:
                    return "MACD";
                case IndicatorEnum.indicator_RSI:
                    return "RSI";
                case IndicatorEnum.indicator_BOLLINGER:
                    return "BOLLINGER";
                case IndicatorEnum.indicator_STOCHASTIC:
                    return "STOCHASTIC";
                case IndicatorEnum.indicator_STOCHASTIC_RSI:
                    return "STOCH_RSI";
                case IndicatorEnum.indicator_GREG:
                    return "GREG";
            }
            return string.Empty;
        }

        // required interface members needed to compile
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        { return true; }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        { return true; }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        { return true; }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        { return true; }
    }

    public class FriendlyBoolConverter : TypeConverter
    {
        // Set the values to appear in the combo box
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values = new List<string>() { "Yes", "No" };

            return new StandardValuesCollection(values);
        }

        // map the value from "Friendly" string to bool type
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value.ToString() == "Yes" ? true : false;
        }

        // map the bool type to "Friendly" string
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return (bool)value ? "Yes" : "No";
        }

        // required interface members needed to compile
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        { return true; }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        { return true; }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        { return true; }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        { return true; }
    }

}
