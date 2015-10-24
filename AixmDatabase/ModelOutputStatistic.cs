//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ModelOutputStatistic.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Diagnostics;

    /// <summary>
    ///     See http://www.nws.noaa.gov/mdl/gfslamp/docs/LAMP_description.shtml
    /// </summary>
    [DebuggerDisplay(
        "Time = {ForecastTime}, T = {Temperature}, Dew = {DewPoint}, Cloud = {CloudCoverage}, WindDir = {Direction} @{WindSpeed}"
        )]
    public class ModelOutputStatistic
    {
        public DateTime ForecastTime { get; set; }
        public int Temperature { get; set; }
        public int DewPoint { get; set; }
        public string CloudCoverage { get; set; }
        public int WindDirection { get; set; }
        public int WindSpeed { get; set; }
        public int Gust { get; set; }
        public int Visibility { get; set; }
        public int PrecipationProbability { get; set; }
        public string Convection { get; set; }

        public String VisibilityDescription
        {
            get
            {
                switch (this.Visibility)
                {
                    case 1:
                        return "<1/2mi";
                    case 2:
                        return "1/2-1mi";
                    case 3:
                        return "1-2mi";
                    case 4:
                        return "2-3mi";
                    case 5:
                        return "3-5mi";
                    case 6:
                        return "6mi";
                    case 7:
                        return ">6mi";
                    default:
                        return string.Empty;
                }
            }
            private set { ; }
        }

        public int Ceiling { get; set; }

        public string Precipitation { get; set; }

        public String CeilingDescription
        {
            get
            {
                switch (this.Ceiling)
                {
                    case 1:
                        return "<200";
                    case 2:
                        return "200-400";
                    case 3:
                        return "500-900";
                    case 4:
                        return "1000-1900";
                    case 5:
                        return "2000-3000";
                    case 6:
                        return "3100-6500";
                    case 7:
                        return "6600-12000";
                    case 8:
                        return "unlimited";
                    default:
                        return string.Empty;
                }
            }
            private set { ; }
        }

        public string ObstructionOfVision { get; set; }

        public String ObstructionOfVisionDescription
        {
            get
            {
                switch (this.ObstructionOfVision)
                {
                    case "N":
                        return string.Empty;
                    case "HZ":
                        return "Haze";
                    case "BR":
                        return "Mist";
                    case "FG":
                        return "Fog";
                    case "BL":
                        return "Dust";
                    default:
                        return string.Empty;
                }
            }
            private set { ; }
        }

        public String FriendlyDescription
        {
            get { return this.ToString(); }
            private set { ; }
        }

        public override string ToString()
        {
            return string.Format(
                "{0:MM/dd HH} {1:000}° @{2,2}kt {3,-2} {4,2}/{5,2}°F {6,11} {7,5} {8,3} {9,5}",
                this.ForecastTime,
                this.WindDirection,
                this.WindSpeed,
                this.Gust == 0 ? "    " : this.Gust.ToString(),
                this.Temperature,
                this.DewPoint,
                this.CeilingDescription,
                this.CloudCoverage,
                this.VisibilityDescription,
                this.ObstructionOfVisionDescription);
        }
    }
}
