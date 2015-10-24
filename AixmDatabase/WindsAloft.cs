//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="WindsAloft.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AixmDatabase
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Windows.Devices.Geolocation;

    [DebuggerDisplay("Station = {Station}, From = {ValidFrom}, To = {ValidTo}")]
    public class WindsAloft
    {
        public WindsAloft()
        {
            this.Winds = new List<WindAloft>();
        }

        public string Station { get; set; }

        public BasicGeoposition GridPoint { get; set; }

        public string GridPointBearing  { get; set; }

        public BasicGeoposition Location { get; set; }

        public DateTime QueryTime { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public List<WindAloft> Winds { get; set; }
    }

    [DebuggerDisplay("Alt = {Altitude}ft, Dir = {Direction}°, Speed = {Speed}kt, Temp={Temperature}°")]
    public class WindAloft
    {
        public float Altitude { get; set; }

        public float Direction { get; set; }

        public float Speed { get; set; }

        public float Temperature { get; set; }

        public float DewPoint { get; set; }

        public float DewPointSpread
        {
            get { return this.Temperature - this.DewPoint; }
            private set { ; }
        }

        public bool Inversion { get; set; }

        public bool WindShear { get; set; }
    }
}
