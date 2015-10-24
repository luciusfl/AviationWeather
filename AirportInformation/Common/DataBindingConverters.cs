//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DataBindingConverters.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Windows.Devices.Geolocation;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Media;
    using metar;
    using taf;
    using ViewModels;

    /// <summary>
    ///     Value converter that translates true to false and vice versa.
    /// </summary>
    public sealed class IncrementalLoadingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var v = (bool)(value);

            return v ? IncrementalLoadingTrigger.Edge : IncrementalLoadingTrigger.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool && (bool)value);
        }
    }

    public sealed class TimezoneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime utc;
            if (DateTime.TryParse((string)value, out utc))
            {
                var local = utc.ToLocalTime();
                if (parameter == null)
                {
                    return local;
                }
                return string.Format(parameter as string, local);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            DateTime local;
            if (DateTime.TryParse((string)value, out local))
            {
                return local.ToUniversalTime();
            }
            return value;
        }
    }

    public sealed class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.Format(parameter as string, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class DistanceFromMyLocationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var airportLocation = (BasicGeoposition)value;
            var myLocation = new BasicGeoposition
            {
                Latitude = App.ViewModel.MyLocation.Latitude,
                Longitude = App.ViewModel.MyLocation.Longitude
            };
            var distance = airportLocation.DistanceTo(myLocation);
            if (parameter == null)
            {
                return string.Format("{0:F1}", distance);
            }
            return string.Format(parameter as string, distance);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class FlightRuleColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var metar = (METAR)value;
            if (metar == null)
            {
                return Colors.DimGray;
            }

            if (metar.flight_category.Length > 0)
            {
                switch (metar.flight_category)
                {
                    case "VFR":
                        return Colors.LightGreen;
                    case "MVFR":
                        return Colors.Blue;
                    case "IFR":
                        return Colors.Red;
                    case "LIFR":
                        return Colors.Magenta;
                    default:
                        return Colors.DimGray;
                }
            }
            if (metar.visibility_statute_mi < 1 ||
                (metar.sky_condition != null &&
                 metar.sky_condition.Any(
                     s => s.cloud_base_ft_agl < 500 && (s.sky_cover == "BKN" || s.sky_cover == "OVC"))))
            {
                return Colors.DarkMagenta;
            }
            if (metar.visibility_statute_mi < 3 ||
                (metar.sky_condition != null &&
                 metar.sky_condition.Any(
                     s => s.cloud_base_ft_agl < 1000 && (s.sky_cover == "BKN" || s.sky_cover == "OVC"))))
            {
                return Colors.DarkRed;
            }
            if (metar.visibility_statute_mi <= 5 ||
                (metar.sky_condition != null &&
                 metar.sky_condition.Any(
                     s =>
                         s.cloud_base_ft_agl <= 3000 &&
                         (s.sky_cover == "BKN" || s.sky_cover == "OVC" || s.sky_cover == "OVX"))))
            {
                return Colors.DarkBlue;
            }
            if (metar.visibility_statute_mi > 5 ||
                (metar.sky_condition != null && metar.sky_condition.Any(s => s.cloud_base_ft_agl > 1000)))
            {
                return Colors.DarkGreen;
            }
            return Colors.DimGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class TerminalForecastColorConverter : IValueConverter
    {
        private static Color previousForcastColor = Colors.DarkGray;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // See http://www.aviationweather.gov/adds/metars/description/page_no/4
            var forecast = ((forecast)value);
            if (forecast == null)
            {
                previousForcastColor = Colors.DarkGray;
                return new SolidColorBrush(previousForcastColor);
            }

            if (!forecast.visibility_statute_miSpecified &&
                (forecast.sky_condition != null && forecast.sky_condition.Any(s => s.cloud_base_ft_agl > 3000)))
            {
                previousForcastColor = Colors.DarkGreen;
                return new SolidColorBrush(previousForcastColor);
            }

            if (forecast.visibility_statute_miSpecified && forecast.visibility_statute_mi < 1 ||
                (forecast.sky_condition != null &&
                 forecast.sky_condition.Any(s => s.cloud_base_ft_agl < 500 && this.IsCeiling(s.sky_cover))))
            {
                previousForcastColor = Colors.DarkMagenta;
                return new SolidColorBrush(previousForcastColor);
            }

            if (forecast.visibility_statute_miSpecified && forecast.visibility_statute_mi < 3 ||
                (forecast.sky_condition != null &&
                 forecast.sky_condition.Any(s => s.cloud_base_ft_agl < 1000 && this.IsCeiling(s.sky_cover))))
            {
                previousForcastColor = Colors.DarkRed;
                return new SolidColorBrush(previousForcastColor);
            }

            if (forecast.visibility_statute_miSpecified && forecast.visibility_statute_mi <= 5 ||
                (forecast.sky_condition != null &&
                 forecast.sky_condition.Any(s => s.cloud_base_ft_agl <= 3000 && this.IsCeiling(s.sky_cover))))
            {
                previousForcastColor = Colors.DarkBlue;
                return new SolidColorBrush(previousForcastColor);
            }

            if (forecast.visibility_statute_miSpecified && forecast.visibility_statute_mi > 5 ||
                (forecast.sky_condition != null && forecast.sky_condition.Any(s => s.cloud_base_ft_agl > 3000)))
            {
                previousForcastColor = Colors.DarkGreen;
                return new SolidColorBrush(previousForcastColor);
            }

            return new SolidColorBrush(previousForcastColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }

        private bool IsCeiling(string cover)
        {
            return (cover == "BKN" || cover == "OVC" || cover == "OVX");
        }
    }

    public sealed class ModelOutputStatisticToFlightruleColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // See http://www.nws.noaa.gov/mdl/synop/mavcard.php
            var mos = ((ModelOutputStatistic)value);
            if (mos == null || mos.ForecastTime == DateTime.MinValue)
            {
                return new SolidColorBrush(Colors.DimGray);
            }

            if (mos.Ceiling <= 2 || mos.Visibility <= 2)
            {
                // < 200-400ft or <1/2-<1miles 
                return new SolidColorBrush(Colors.DarkMagenta);
            }
            if (mos.Ceiling <= 3 || mos.Visibility <= 4)
            {
                // 500-900ft or 2-<3miles
                return new SolidColorBrush(Colors.DarkRed);
            }
            if (mos.Ceiling <= 5 || mos.Visibility <= 5)
            {
                // 2000-3000ft or <=3-5miles
                return new SolidColorBrush(Colors.DarkBlue);
            }
            return new SolidColorBrush(Colors.DarkGreen);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new NotImplementedException();
        }
    }

    public sealed class FlightRuleBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var metar = (METAR)value;
            if (metar == null || metar.flight_category == null)
            {
                return new SolidColorBrush(Colors.DimGray);
            }

            if (metar.flight_category.Length > 0)
            {
                switch (metar.flight_category)
                {
                    case "VFR":
                        return new SolidColorBrush(Colors.DarkGreen);
                    case "MVFR":
                        return new SolidColorBrush(Colors.DarkBlue);
                    case "IFR":
                        return new SolidColorBrush(Colors.DarkRed);
                    case "LIFR":
                        return new SolidColorBrush(Colors.DarkMagenta);
                    default:
                        return new SolidColorBrush(Colors.DimGray);
                }
            }
            if (metar.visibility_statute_mi < 1 ||
                (metar.sky_condition != null &&
                 metar.sky_condition.Any(
                     s => s.cloud_base_ft_agl < 500 && (s.sky_cover == "BKN" || s.sky_cover == "OVC"))))
            {
                return new SolidColorBrush(Colors.DarkMagenta);
            }
            if (metar.visibility_statute_mi < 3 ||
                (metar.sky_condition != null &&
                 metar.sky_condition.Any(
                     s => s.cloud_base_ft_agl < 1000 && (s.sky_cover == "BKN" || s.sky_cover == "OVC"))))
            {
                return new SolidColorBrush(Colors.DarkRed);
            }
            if (metar.visibility_statute_mi <= 5 ||
                (metar.sky_condition != null &&
                 metar.sky_condition.Any(
                     s =>
                         s.cloud_base_ft_agl <= 3000 &&
                         (s.sky_cover == "BKN" || s.sky_cover == "OVC" || s.sky_cover == "OVX"))))
            {
                return new SolidColorBrush(Colors.DarkBlue);
            }
            if (metar.visibility_statute_mi > 5 ||
                (metar.sky_condition != null && metar.sky_condition.Any(s => s.cloud_base_ft_agl > 1000)))
            {
                return new SolidColorBrush(Colors.DarkGreen);
            }
            return new SolidColorBrush(Colors.DimGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var paramValue = (string)parameter;
            if (value == null || (bool)value)
            {
                return paramValue == "Collapsed" ? Visibility.Collapsed : Visibility.Visible;
            }

            return paramValue == "Collapsed" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var paramValue = (string)parameter;
            if (value == null || (Visibility)value == Visibility.Visible)
            {
                return paramValue != "Collapsed";
            }

            return paramValue == "Collapsed";
        }
    }

    public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var paramValue = (string)parameter;
            if (!string.IsNullOrWhiteSpace((string)value))
            {
                return paramValue == "Collapsed" ? Visibility.Collapsed : Visibility.Visible;
            }

            return paramValue == "Collapsed" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var paramValue = (string)parameter;
            if (value == null || (Visibility)value == Visibility.Visible)
            {
                return paramValue != "Collapsed";
            }

            return paramValue == "Collapsed";
        }
    }

    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class IntegerToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var paramValue = (string)parameter;
            if (value == null || ((int)value) > 0)
            {
                return paramValue == "Collapsed" ? Visibility.Collapsed : Visibility.Visible;
            }

            return paramValue == "Collapsed" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var paramValue = (string)parameter;
            if (value == null || (Visibility)value == Visibility.Visible)
            {
                return paramValue != "Collapsed";
            }

            return paramValue == "Collapsed";
        }
    }

    public sealed class DateTimeToTimeSinceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime past;
            if (DateTime.TryParse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out past))
            {
                var timeSpan = DateTime.UtcNow - past.ToUniversalTime();
                var sb = new StringBuilder();
                if (timeSpan.Days > 0)
                {
                    sb.AppendFormat("{0} days ", timeSpan.Days);
                }

                if (timeSpan.Hours > 0)
                {
                    sb.AppendFormat("{0} hrs ", timeSpan.Hours);
                }

                if (timeSpan.Minutes > 0)
                {
                    sb.AppendFormat("{0} min", timeSpan.Minutes);
                }

                sb.Append(" ago");

                return sb.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MetarCodeConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> translator = new Dictionary<string, string>
        {
            { "+", "Heavy" },
            { "-", "Light" },
            { "VC", "In Vicinity" },
            { "BC", "Patches" },
            { "BL", "Blowing" },
            { "DR", "Low Drifting" },
            { "FZ", "Freezing" },
            { "MI", "Shallow" },
            { "PR", "Partial" },
            { "SH", "Shower(s)" },
            { "TS", "Thunderstorm" },
            { "DZ", "Drizzle" },
            { "GR", "Hail" },
            { "GS", "Small Hail" },
            { "IC", "Ice Crystals" },
            { "PL", "Ice Pellets" },
            { "RA", "Rain" },
            { "SG", "Snow Grains" },
            { "SN", "Snow" },
            { "UP", "Unknown Precipitation" },
            { "BR", "Mist" },
            { "DU", "Widespread Dust" },
            { "FG", "Fog" },
            { "FU", "Smoke" },
            { "HZ", "Haze" },
            { "PY", "Spray" },
            { "SA", "Sand" },
            { "VA", "Volcanic Ash" },
            { "DS", "Duststorm" },
            { "FC", "Tornado" },
            { "PO", "Well-developed dust/sand whirls" },
            { "SQ", "Squalls" },
            { "SS", "Sandstorm" }
        };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            /*
             * Improvement:
             * 
             * Right now we sequentially decode, which results in bad grammar.
             * For example, in the case of "-VCSHRA" we get "Light In Vicinity Shower(s) Rain", instead of Light Rain Showers in vicinity".
             * It's better than no translation, since context should be obvious to user. To be sure no misinterpretation is possible
             * the raw wx_string is displayed in the UI as well at the bottom.
            */
            var wx = (string)value;
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(wx) || wx.Length < 2)
            {
                return wx;
            }

            var translation = string.Empty;
            for (var i = 0; i < wx.Length; i++)
            {
                var c = wx[i];
                if (c == ' ')
                {
                    continue;
                }

                if (c == '+')
                {
                    sb.Append("Heavy ");
                }
                else if (c == '-')
                {
                    sb.Append("Light ");
                }
                else
                {
                    var code = string.Empty;
                    if (wx.Length >= i + 2)
                    {
                        code = wx.Substring(i, 2);
                    }

                    i++;
                    if (translator.TryGetValue(code, out translation))
                    {
                        sb.Append(translation + " ");
                    }
                }
            }

            return sb.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class TranslatorConverter : IValueConverter
    {
        private static readonly char[] recordsplitter = { '|' };
        private static readonly char[] fieldsplitter = { ';' };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var map = (string)parameter;
            if (string.IsNullOrEmpty(map))
            {
                return value;
            }

            var input = (string)value;

            var records = map.Split(recordsplitter, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rec in records)
            {
                var tuple = rec.Split(fieldsplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tuple[0] == input)
                {
                    if (tuple.Length == 2)
                    {
                        return tuple[1];
                    }
                    return string.Empty;
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class HideIfKeywordsNullorEmptyConverter : IValueConverter
    {
        private static readonly char[] splitter = { '|' };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var input = (string)value;
            if (string.IsNullOrEmpty(input))
            {
                return Visibility.Collapsed;
            }
            var tokens = (string)parameter;
            var idx = tokens.IndexOf(input);
            if (idx == -1)
            {
                return Visibility.Visible;
            }
            if (tokens.Length == input.Length)
            {
                return Visibility.Collapsed;
            }
            var toks = tokens.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tok in toks)
            {
                if (tok == input)
                {
                    return Visibility.Collapsed;
                }
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var paramValue = (string)parameter;
            if (value == null || (Visibility)value == Visibility.Visible)
            {
                return paramValue != "Collapsed";
            }

            return paramValue == "Collapsed";
        }
    }

    public sealed class HideInvalidLampDataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var input = (int)value;
            if (input == 99 || input == 990 || input == 999)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class LampGustConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var input = (int)value;
            if (input == 0 || input == 99 || input == 990 || input == 999)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class TafWindDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var f = (forecast)value;
            if (f.wind_speed_ktSpecified && f.wind_dir_degreesSpecified && f.wind_speed_kt > 0 &&
                f.wind_dir_degrees == 0)
            {
                return "Wind Variable";
            }
            return "Wind " + f.wind_dir_degrees + "°";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MetarWindDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var f = (METAR)value;
            if (f.wind_speed_ktSpecified && f.wind_dir_degreesSpecified && f.wind_speed_kt > 0 &&
                f.wind_dir_degrees == 0)
            {
                return "Wind Variable";
            }

            return "Wind " + f.wind_dir_degrees + "°";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class BasicGeopositionToGeopointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new Geopoint((BasicGeoposition)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class AltitudeToStandardTemperatureConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int altitude = (int)value;
            float result = 15f - (1.98f * altitude) / 1000f;
            if (parameter == null)
            {
                return result;
            }
            else
            {
                return string.Format((string)parameter, result);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            float temperature = (float)value;
            float result = (temperature * 1000f - 15f) / 1.98f;
            if (parameter == null)
            {
                return result;
            }
            else
            {
                return string.Format((string)parameter, result);
            }
        }
    }

    public sealed class InversionColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool inversion = (bool)value;
            if (inversion)
            {
                return new SolidColorBrush(Colors.Red);
            }
            else
            {
                return new SolidColorBrush(Colors.White);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class WindSpeedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            float speed = (float)value;
            if (speed >= 50)
            {
                return new SolidColorBrush(Colors.Red);
            }
            else
            {
                return new SolidColorBrush(Colors.White);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public sealed class FreezingPointColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            float temperature = (float)value;
            if (temperature == 0)
            {
                return new SolidColorBrush(Colors.Blue);
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class DuePointSpreadToCloudConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            float dewPointSpread = (float)value;
            if (dewPointSpread < 1)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// See WMO-No. 306 Table 1800. Intensity or character of the weather element (type of weather).
    /// Applicable for icing and turbulence.
    /// </summary>
    public sealed class WeatherElementIntensityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var intensity = (string)value;
            switch (intensity)
            {
                case "0":
                    return "Not specified";
                case "1":
                    return string.Format("Light {0} in clouds", parameter);
                case "2":
                    return string.Format("Moderate {0} in clouds", parameter);
                case "3":
                    return string.Format("Severe {0} in clouds", parameter);
                case "4":
                    return string.Format("Light {0} in precipitation", parameter);
                case "5":
                    return string.Format("Moderate {0} in precipitation", parameter);
                case "6":
                    return string.Format("Severe {0} in precipitation", parameter);
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
