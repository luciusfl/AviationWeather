//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace ConvertMetarStationTextFile
{
    using System.IO;
    using System.Linq;

    /// <summary>
    ///     Converts http://www.aviationweather.gov/static/adds/metars/stations.txt to a small text file with only the ICAO stations.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            ConvertMetarStationFile(@"E:\visual studio\Projects\PilotWeather\ConvertMetarStationTextFile\MetarTafStations.txt");
            //ConvertGfsMosStationFile(@"E:\visual studio\Projects\PilotWeather\ConvertMetarStationTextFile\NEW GFS MOS Stations and WMO Headers.htm");
        }

        private static void ConvertMetarStationFile(string textFilename)
        {
            var inputFilename = textFilename;
            var outputFilename = inputFilename + ".converted.txt";
            using (var input = File.OpenRead(inputFilename))
            {
                var reader = new StreamReader(input);
                using (var output = File.OpenWrite(outputFilename))
                {
                    var writer = new StreamWriter(output);
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line.Length == 83)
                        {
                            var station = line.Substring(20, 4);
                            if (station.Length == 4 && EveryCharacterBetweenAandZ(station))
                            {
                                string vFlag = line.Substring(68, 1); //T=TAF, U=Taf+Airmet/Sigmet endpoint, A=ARTCC
                                writer.WriteLine(station + " " + vFlag);
                            }
                        }
                    }

                    writer.Flush();
                }
            }
        }

        /// <summary>
        /// Get file from http://www.nws.noaa.gov/mdl/synop/stadrggfs2009.php
        /// The count of stations should be around 1639.
        /// </summary>
        /// <param name="textFilename"></param>
        private static void ConvertGfsMosStationFile(string textFilename)
        {
            var inputFilename = textFilename;
            var outputFilename = inputFilename + ".converted.txt";
            using (var input = File.OpenRead(inputFilename))
            {
                var reader = new StreamReader(input);
                using (var output = File.OpenWrite(outputFilename))
                {
                    var writer = new StreamWriter(output);
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line.Length == 49)
                        {
                            var station = line.Substring(0, 4);
                            if (station.Length == 4 && EveryCharacterBetweenAandZ(station))
                            {
                                writer.WriteLine(station);
                            }
                        }
                    }

                    writer.Flush();
                }
            }
        }

        private static bool EveryCharacterBetweenAandZ(string text)
        {
            return text.All(c => c >= 'A' && c <= 'Z');
        }
    }
}
