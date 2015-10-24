//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Haversine.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.ViewModels
{
    using System;
    using Windows.Devices.Geolocation;

    public static class Haversine
    {
        public static double DistanceTo(this BasicGeoposition here, BasicGeoposition there)
        {
            //http://www.codecodex.com/wiki/Calculate_Distance_Between_Two_Points_on_a_Globe#C.23
            var dLat = (there.Latitude - here.Latitude).ToRadian();
            var dLon = (there.Longitude - here.Longitude).ToRadian();
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(here.Latitude.ToRadian()) * Math.Cos(there.Latitude.ToRadian()) * Math.Sin(dLon / 2) *
                    Math.Sin(dLon / 2);
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            var d = 3440 * c; //3440 = earth mean radius in nautical miles
            return d;
        }

        private static double ToRadian(this double val)
        {
            return (Math.PI / 180) * val;
        }
    }
}
