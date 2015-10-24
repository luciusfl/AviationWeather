//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DensityAltitude.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.Common
{
    using System;

    public static class Atmosphere
    {
        public static double DensityAlitude(metar.METAR metar)
        {
            // Calculate the vapor pressures (mb) given the ambient temperature (c) and dew point (c).
            var emb = VaporPressure(metar.dewpoint_c);

            // Calculate geo-potential altitude H (m) from geometric altitude (m) Z.
            var hm = GeoPotentialAltitude(metar.elevation_m);

            // Calculate the absolute pressure given the altimeter setting(mb) and geo-potential elevation(meters).
            var actpressmb = AbsolutePressure(metar.altim_in_hg * 33.86389, hm);

            // Calculate the air density (kg/m3) from absolute pressure (mb) vapor pressure (mb) and temp (c).
            var density = AirDensity(actpressmb, emb, metar.temp_c);

            // Calculate the geo-potential altitude (m) in ISA with that same density (kg/m3).
            var densaltm = IsaAltitude(density);

            // Calculate geometric altitude Z (m) from geo-potential altitude (m) H
            var densaltzm = CalcZ(densaltm);
            var densaltz = densaltzm / 0.304800;
            return densaltz;
        }

        /// <summary>
        /// Calculate the saturation vapor pressure given the temperature(Celsius) Polynomial.
        /// </summary>
        /// <param name="t">Temperature in Celsius</param>
        /// <returns></returns>
        public static double VaporPressure(double t)
        {
            double eso = 6.1078;
            double c0 = 0.99999683;
            double c1 = -0.90826951E-02;
            double c2 = 0.78736169E-04;
            double c3 = -0.61117958E-06;
            double c4 = 0.43884187E-08;
            double c5 = -0.29883885E-10;
            double c6 = 0.21874425E-12;
            double c7 = -0.17892321E-14;
            double c8 = 0.11112018E-16;
            double c9 = -0.30994571E-19;
            double pol = c0 +
                         t *
                         (c1 + t * (c2 + t * (c3 + t * (c4 + t * (c5 + t * (c6 + t * (c7 + t * (c8 + t * (c9)))))))));
            double es = eso / Math.Pow(pol, 8);
            return (es);
        }

        /// <summary>
        /// Calculate absolute air pressure given the barometric pressure(mb) and altitude(meters).
        /// </summary>
        /// <param name="pressure"></param>
        /// <param name="altitude"></param>
        /// <returns></returns>
        public static double AbsoluteAirPressure(double pressure, double altitude)
        {
            double k1 = 0.190284;
            double k2 = 8.4288 * Math.Pow(10, -5);
            double p1 = Math.Pow(pressure, k1);
            double p2 = altitude * k2;
            double p3 = 0.3 + Math.Pow((p1 - p2), (1 / k1));
            return (p3);
        }

        /// <summary>
        ///  Calculate the air density in kg/m3.
        /// </summary>
        /// <param name="abspressmb"></param>
        /// <param name="e"></param>
        /// <param name="tc"></param>
        /// <returns></returns>
        public static double AirDensity(double abspressmb, double e, double tc)
        {
            double Rv = 461.4964;
            double Rd = 287.0531;
            double tk = tc + 273.15;
            double pv = e * 100;
            double pd = (abspressmb - e) * 100;
            double d = (pv / (Rv * tk)) + (pd / (Rd * tk));
            return (d);
        }

        /// <summary>
        /// Calculate the ISA altitude (meters) for a given density (kg/m3).
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double IsaAltitude(double d)
        {
            double g = 9.80665;
            int Po = 101325;
            double To = 288.15;
            double L = 6.5;
            double R = 8.314320;
            double M = 28.9644;
            double D = d * 1000;
            double p2 = ((L * R) / (g * M - L * R)) * Math.Log((R * To * D) / (M * Po));
            double H = -(To / L) * (Math.Exp(p2) - 1);
            double h = H * 1000;
            return (h);
        }

        /// <summary>
        /// Calculate the H altitude (meters), given the Z altitude (meters).
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public static double GeoPotentialAltitude(double z)
        {
            double r = 6369E3;
            return ((r * z) / (r + z));
        }

        /// <summary>
        /// Calculate the actual pressure (mb)from the altimeter setting (mb) and geo-potential altitude (m).
        /// </summary>
        /// <param name="As"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static double AbsolutePressure(double altimeter, double altitude)        
        {
            double k1 = .190263;
            double k2 = 8.417286E-5;
            double p = Math.Pow((Math.Pow(altimeter, k1) - (k2 * altitude)), (1 / k1));
            return (p);
        }

        /// <summary>
        /// Calculate the Z altitude (meters), given the H altitude (meters).
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        private static double CalcZ(double h)
        {
            double r = 6369E3;
            return ((r * h) / (r - h));
        }
    }
}
