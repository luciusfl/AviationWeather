//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Xml;
    using Windows.Devices.Geolocation;
    using Newtonsoft.Json.Linq;
    using ParseAIXM;
    using ViewModels;

    /// <summary>
    /// This class provides several helpers to help with development of the AIXM file parsing, testing it, and also 
    /// generating the final binary file database, that is deployed with the application.
    /// The AIXM files must be downloaded from the FAA web site at 
    /// 
    /// More information about the AIXM format can be found at
    /// http://www.aixm.aero/public/subsite_homepage/homepage.html and
    /// http://www.faa.gov/about/office_org/headquarters_offices/ato/service_units/mission_support/aixm/
    /// 
    /// The actual AIXM data files can be download at https://nfdc.faa.gov/xwiki/bin/view/NFDC/56+Day+NASR+Subscription.
    /// They are updated every 56 dates, which coincides nicely with a phone apps development upgrade live cycle.
    /// 
    /// Use the APT_AIXM.xml.short.xml for debugging and testing, since the actual xml file is over 600 MB large, and 
    /// therefore inconvenient to manage during development.
    /// 
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            //GetLocationForAviationForcastDiscussions();
            //JsonNotamParser();
            //ParseModelOutputStatistic();      
            //CreateWindsAloftStationDatabase();
            //ParseWindsAloft();
            //Serialization();
            CreateAixmBinaryDatabase();
        }

        private static void CreateAixmBinaryDatabase()
        {
            var database = new AirportDatabase();
            var parser = new AixmParser();
            var rootAixmFolder = @"C:\AIXM\Oct2015\AIXM_5.1\";
            parser.InitializeFromAixmFile(rootAixmFolder + @"XML-Subscriber-Files\APT_AIXM.xml");
            //parser.InitializeFromAixmFile2(@"..\..\APT_AIXM.xml.short.xml");
            Stopwatch timer = Stopwatch.StartNew();
            int airportsSerialized = parser.Serialize(rootAixmFolder + @"\aixm.db");
            timer.Stop();
            Console.WriteLine("Serializer {0} airports took {1}ms", airportsSerialized, timer.ElapsedMilliseconds);
            timer.Restart();
            database.Initialize(rootAixmFolder + @"\aixm.db");
            timer.Stop();
            Console.WriteLine("Deserializer took {0}ms", timer.ElapsedMilliseconds);
            VerifyDataIntegrity(database.AirportTable);
            Airport san = database.LookupByDesignator("SAN");
            Airport sea = database.LookupByDesignator("SEA");
            Airport bfi = database.LookupByDesignator("BFI");
            Airport rnt = database.LookupByDesignator("RNT");
            Airport mia = database.LookupByDesignator("MIA");
            Airport here = rnt;
            const int radius = 50;
            Console.WriteLine("==>Airports nearby {0} in {1}nm radius.", here, radius);
            foreach (var airport in database.Nearby(here.Location, radius, 1001))
            {
                Console.WriteLine("{0}, {1:F1}nm", airport, here.Location.DistanceTo(airport.Location));
            }
        }

        private static void GetLocationForAviationForcastDiscussions()
        {
            var input = File.OpenRead(@"C:\Users\luciusf\Documents\AFD.TXT");
            var output = File.OpenWrite(@"C:\Users\luciusf\Documents\AFD.output.TXT");
            var writer = new StreamWriter(output);
            writer.AutoFlush = true;
            var reader = new StreamReader(input);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                string station = line.Substring(15, 4);
                string uri = string.Format("http://aviationweather.gov/adds/dataserver_current/httpparam?dataSource=metars&requestType=retrieve&format=xml&stationString={0}&hoursBeforeNow=3", station);
                var request = WebRequest.CreateHttp(uri);
                try
                {
                    using (var response = request.GetResponse())
                    {
                        using (XmlReader xmlReader = XmlReader.Create(response.GetResponseStream()))
                        {
                            xmlReader.ReadToFollowing("latitude");
                            string lat = xmlReader.ReadElementString();
                            xmlReader.ReadToFollowing("longitude");
                            string lon = xmlReader.ReadElementString();
                            //            {"KABQ", new BasicGeoposition {Latitude = 35.05, Longitude = -106.62}},
                            string item = string.Format(
                                @"{{""{0}"", new BasicGeoposition {{Latitude = {1}, Longitude = {2}}}}},",
                                station,
                                lat,
                                lon);
                            //Debug.WriteLine(item);
                            //writer.WriteLine(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("{0}, {1}", station, ex.Message);
                    Console.WriteLine(station);
                }

                Thread.Sleep(100);
            }

            writer.Flush();
        }

        private static void Serialization()
        {
            var database = new AirportDatabase();
            var parser = new AixmParser();
            parser.InitializeFromAixmFile(@"..\..\APT_AIXM.xml.short.xml");
            //parser.InitializeFromAixmFile(@"E:\AIXM_5.1\XML-Subscriber-Files\APT_AIXM.xml");
            parser.Serialize("test.bin");
            database.Initialize("test.bin");
            Stopwatch timer = Stopwatch.StartNew();
            var memWrite = new MemoryStream();
            var writer = new StreamWriter(memWrite);
            int airportsSerialized = 0;
            foreach (var apt in database.AirportTable.Values)
            {
                apt.Serialize(writer);
                airportsSerialized++;
            }

            writer.Flush();
            timer.Stop();
            Console.WriteLine("Serialization of {0} airports took {1}ms.", airportsSerialized, timer.ElapsedMilliseconds);
            timer.Start();
            memWrite.Position = 0;
            var airports = new List<Airport>(20000);
            int airportDeserialized = 0;

            foreach (var a in AirportBase.Deserialize(memWrite))
            {
                airports.Add(a);
                airportDeserialized++;
            }

            timer.Stop();
            Console.WriteLine("Deserialization of {0} airports took {1}ms.", airportDeserialized, timer.ElapsedMilliseconds);
        }

        private static void ParseModelOutputStatistic()
        {
            using (var file = File.OpenRead(@"..\..\GFS MOS FORECASTS.htm"))
            {
                ParseModelOutputStatistic(file);
            }
        }

        /*
         DT /DEC  13/DEC  14                /DEC  15                /DEC  16 
         HR   18 21 00 03 06 09 12 15 18 21 00 03 06 09 12 15 18 21 00 06 12 
         N/X                    36          49          34          48    38 
         TMP  45 46 45 42 40 38 37 37 41 47 47 42 39 37 36 37 41 46 46 41 40 
         DPT  42 41 40 40 38 37 35 34 34 34 34 34 32 32 31 32 33 33 34 35 35 
         CLD  OV BK BK CL FW CL FW CL CL CL CL CL CL CL BK BK OV OV BK OV OV 
         WDR  14 14 14 03 05 02 36 05 36 34 01 04 06 01 01 05 03 05 10 09 03 
         WSP  01 02 04 02 01 02 03 02 04 08 06 04 06 04 03 03 04 05 04 02 03 
         P06         0     0     3     2     0     5    19    24    25 31 28 
         P12                     8           2          19          29    35 
         Q06         0     0     0     0     0     0     0     0     0  0  0 
         Q12                     0           0           0           0     0 
         T06      0/ 3  0/ 0  0/ 0  0/ 1  0/ 0  0/ 0  0/ 1  0/ 2  0/ 0  0/ 0 
         T12            0/ 3        0/ 3        0/ 0        0/ 3     0/ 0    
         POZ   0  0  0  0  0  1  0  1  1  0  0  0  0  0  0  0  0  0  0  0  0 
         POS   2  9  9 17 16 18 23 23 13 13 13 21 23 24 24 23 15 11 13 11 16 
         TYP   R  R  R  R  R  R  R  R  R  R  R  R  R  R  R  R  R  R  R  R  R 
         SNW                     0                       0                 0 
         CIG   5  6  8  8  8  8  8  8  8  8  8  8  8  8  8  8  8  8  8  7  7 
         VIS   7  7  7  7  7  7  7  7  7  7  7  7  7  7  7  7  7  7  7  7  7 
         OBV   N  N  N  N  N  N  N  N  N  N  N  N  N  N  N  N  N  N  N  N  N 

         * * */
        private static List<ModelOutputStatistic> ParseModelOutputStatistic(Stream htmlStream)
        {
            var mosList = new List<ModelOutputStatistic>(16);
            for (int i = 0; i < mosList.Capacity; ++i)
            {
                mosList.Add(new ModelOutputStatistic());
            }

            var reader = new StreamReader(htmlStream);
            bool inTable = false;
            string month = string.Empty;
            int day = 0;
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.StartsWith(@"<PRE>"))
                {
                    inTable = true;
                }
                else if (line.StartsWith(@"</PRE>"))
                {
                    inTable = false;
                    break;
                }

                if (inTable)
                {
                    string symbol = line.Substring(1, 3);
                    switch (symbol)
                    {
                        case "DT ":
                            month = line.Substring(5, 3);
                            day = int.Parse(line.Substring(10, 2));
                            break;
                        case "HR ":
                            int startHour = int.Parse(line.Substring(6, 2));
                            var u = DateTime.UtcNow;
                            var forecastStart = new DateTime(u.Year, u.Month, day);
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                var utcForecast = forecastStart.AddHours(startHour + i * 3);
                                mosList[i].ForecastTime = utcForecast.ToLocalTime();
                            }

                            break;
                        case "TMP":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                mosList[i].Temperature = int.Parse(line.Substring(6 + i * 3, 2));
                            }
                            break;
                        case "DPT":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                mosList[i].DewPoint = int.Parse(line.Substring(6 + i * 3, 2));
                            }
                            break;
                        case "CLD":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                mosList[i].CloudCoverage = line.Substring(6 + i * 3, 2);
                            }
                            break;
                        case "WDR":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                mosList[i].WindDirection = 10 * int.Parse(line.Substring(6 + i * 3, 2));
                            }
                            break;
                        case "WSP":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                mosList[i].WindSpeed = int.Parse(line.Substring(7 + i * 3, 2));
                            }
                            break;
                        case "CIG":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                mosList[i].Ceiling = int.Parse(line.Substring(7 + i * 3, 2));
                            }
                            break;
                        case "VIS":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                mosList[i].Visibility = int.Parse(line.Substring(7 + i * 3, 2));
                            }
                            break;
                        case "OBV":
                            for (int i = 0; i < mosList.Count; ++i)
                            {
                                var sym = line.Substring(6 + i * 3, 2);
                                if (sym == "N")
                                {
                                    sym = string.Empty;
                                }

                                mosList[i].ObstructionOfVision = sym;
                            }
                            break;
                    }
                }
            }

            return mosList;
        }

        private static void ShortenAixmFile()
        {
            var reader = new StreamReader(@"E:\AIXM_5.1\XML-Subscriber-Files\APT_AIXM.xml");
            var writer = new StreamWriter(@"E:\AIXM_5.1\XML-Subscriber-Files\APT_AIXM_SHORT2.xml");
            const int lines = 10000;
            for (int i = 0; i < lines; ++i)
            {
                writer.WriteLine(reader.ReadLine());
            }

            writer.Flush();
            Environment.Exit(0);
        }

        private static void DumpNode(string token)
        {
            var reader = new StreamReader(@"E:\AIXM_5.1\XML-Subscriber-Files\APT_AIXM.xml");
            int maxLines = 100;
            bool found = false;
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.Contains(token))
                {
                    found = true;
                }

                if (found)
                {
                    --maxLines;
                    Console.WriteLine(line);
                }

                if (maxLines == 0)
                {
                    break;
                }
            }
        }

        private static void VerifyDataIntegrity(Dictionary<string, Airport> airports)
        {
            var Helipads = new[]
            {
                "HP", "HI", "H1", "H2", "H3", "H4", "H5", "H6", "H7", "H8", "H9", "H10", "B1", "H-A", "H-B", "H-C", "H-D",
                "H-E", "H-F", "HB", "HF"
            };

            foreach (var key in airports.Keys)
            {
                Airport airport = airports[key];
                Debug.Assert(airport.Id.Length > 5);
                Debug.Assert(airport.Runways.Any());
                foreach (var runway in airport.Runways)
                {
                    if (!airport.IsHeliport)
                    {
                        if (!Helipads.Contains(runway.Designator))
                        {
                            if (runway.Designator.Length == 3 && runway.Designator[2] == 'X')
                            {
                                // Console.WriteLine("Invalid runway {0} {1}", airport.Designator, runway);
                            }
                            else
                            {
                                Debug.Assert(runway.Base.TrafficDirection == 'L' || runway.Base.TrafficDirection == 'R');
                                Debug.Assert(
                                    runway.Reciprocal.TrafficDirection == 'L' ||
                                    runway.Reciprocal.TrafficDirection == 'R');
                                Debug.Assert(runway.Base.TrafficDirection != '\0');
                                Debug.Assert(runway.Reciprocal.TrafficDirection != '\0');
                                Debug.Assert(airport.City.Length > 0);
                                Debug.Assert(airport.FieldElevation >= -250);
                                Debug.Assert(airport.MagneticVariation >= -90 && airport.MagneticVariation <= 90);
                                //Debug.Assert(airport.TransitionAltitude >= 500);
                                Debug.Assert(airport.IcaoIdenticator != null || airport.Designator != null);
                                Debug.Assert(airport.Location.Latitude > -90 && airport.Location.Latitude < 90);
                                Debug.Assert(airport.Location.Longitude > -180 && airport.Location.Longitude < 180);
                                Debug.Assert(airport.Designator.Length > 2);
                            }
                        }
                    }
                }
            }
        }

        private static void JsonNotamParser()
        {
            string response =
                @"{'notamList':[{'facilityDesignator':'RNT','notamNumber':'11/005','featureName':'Aerodrome','issueDate':'11/12/2014 2033','startDate':'11/12/2014 2033','endDate':'02/27/2015 2359EST','source':'DN','sourceType':'D','icaoMessage':'\u003cp align\u003d\'left\'\u003e 11/005 NOTAMR\u003cbr\u003e \r\n\u003cb\u003eQ) \u003c/b\u003eZSE/QMRHW/IV/NBO/A/000/999/4729N12212W005\u003cbr\u003e \r\n\u003cb\u003eA) \u003c/b\u003eKRNT\u003cbr\u003e \r\n\u003cb\u003eB) \u003c/b\u003e1411122033\u003cbr\u003e \r\n\u003cb\u003eC) \u003c/b\u003e1502272359EST\u003cbr\u003e \r\n\r\n\u003cb\u003eE) \u003c/b\u003e RWY 16/34 WIP CONST ADJ S END\u003cbr\u003e\u003c/p\u003e','traditionalMessage':'!RNT 11/005 RNT RWY 16/34 WIP CONST ADJ S END 1411122033-1502272359EST','plainLanguageMessage':'\u003ctable border\u003d\'0\'\u003e\r\n\u003ctbody\u003e\u003ctr\u003e\u003ctd\u003e\u003cb\u003eIssuing Airport:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e(RNT) Renton Muni \u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eNOTAM Number:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e 11/005\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eEffective Time Frame\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eBeginning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eWednesday, November 12, 2014 2033 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eEnding:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFriday, February 27, 2015 2359EST (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eAffected Areas\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eRunway:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e16/34\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Warning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e Work In Progress - Construction Adjacent to runway South Of End \u003c/td\u003e\u003c/tr\u003e\r\n\r\n \u003ctr\u003e\u003ctd\u003e\u003cb\u003e\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\t\u003ctr\u003e\u003c/tr\u003e\r\n\u003c/tbody\u003e\u003c/table\u003e','phoneNumberOfCreator':'2064230087','faxNumberOfCreator':'','radioFrequencyOfCreator':'','traditionalMessageFrom4thWord':'RWY 16/34 WIP CONST ADJ S END 1411122033-1502272359EST','icaoId':'KRNT','accountId':'3069','airportName':'Renton Muni','procedure':false,'userID':0,'transactionID':39039684,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'keyword':'RWY','snowtam':false,'geometry':'POINT(-1642626.16790685 507740.060276838)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'12/002','featureName':'Aerodrome','issueDate':'12/01/2014 1715','startDate':'12/01/2014 1715','endDate':'07/31/2015 2359','source':'DN','sourceType':'D','icaoMessage':'\u003cp align\u003d\'left\'\u003e 12/002 NOTAMN\u003cbr\u003e \r\n\u003cb\u003eQ) \u003c/b\u003eZSE/QMMXX/IV/NBO/A/000/999/4729N12212W005\u003cbr\u003e \r\n\u003cb\u003eA) \u003c/b\u003eKRNT\u003cbr\u003e \r\n\u003cb\u003eB) \u003c/b\u003e1412011715\u003cbr\u003e \r\n\u003cb\u003eC) \u003c/b\u003e1507312359\u003cbr\u003e \r\n\r\n\u003cb\u003eE) \u003c/b\u003eRWY 16/34 SURFACE MARKINGS FADED \u003cbr\u003e\u003c/p\u003e','traditionalMessage':'!RNT 12/002 RNT RWY 16/34 SURFACE MARKINGS FADED 1412011715-1507312359','plainLanguageMessage':'\u003ctable border\u003d\'0\'\u003e\r\n\u003ctbody\u003e\u003ctr\u003e\u003ctd\u003e\u003cb\u003eIssuing Airport:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e(RNT) Renton Muni \u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eNOTAM Number:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e 12/002\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eEffective Time Frame\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eBeginning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eMonday, December 1, 2014 1715 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eEnding:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFriday, July 31, 2015 2359 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eAffected Areas\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eRunway:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e16/34\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Marking Location:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eSurface Markings\u003c/td\u003e\u003c/tr\u003e\r\n\r\n\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Marking Type:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFaded \u003c/td\u003e\u003c/tr\u003e\r\n\r\n \u003ctr\u003e\u003ctd\u003e\u003cb\u003e\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\t\u003ctr\u003e\u003c/tr\u003e\r\n\u003c/tbody\u003e\u003c/table\u003e','phoneNumberOfCreator':'2064230087','faxNumberOfCreator':'','radioFrequencyOfCreator':'','traditionalMessageFrom4thWord':'RWY 16/34 SURFACE MARKINGS FADED 1412011715-1507312359','icaoId':'KRNT','accountId':'3069','airportName':'Renton Muni','procedure':false,'userID':0,'transactionID':39184469,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'keyword':'RWY','snowtam':false,'geometry':'POINT(-1642630.95873854 507736.902699476)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'10/012','featureName':'Aerodrome','issueDate':'10/13/2014 2117','startDate':'10/13/2014 2117','endDate':'05/29/2015 2359','source':'DN','sourceType':'D','icaoMessage':'\u003cp align\u003d\'left\'\u003e 10/012 NOTAMN\u003cbr\u003e \r\n\u003cb\u003eQ) \u003c/b\u003eZSE/QMMXX/IV/NBO/A/000/999/4729N12212W005\u003cbr\u003e \r\n\u003cb\u003eA) \u003c/b\u003eKRNT\u003cbr\u003e \r\n\u003cb\u003eB) \u003c/b\u003e1410132117\u003cbr\u003e \r\n\u003cb\u003eC) \u003c/b\u003e1505292359\u003cbr\u003e \r\n\r\n\u003cb\u003eE) \u003c/b\u003eTWY A SURFACE MARKINGS FADED\u003cbr\u003e\u003c/p\u003e','traditionalMessage':'!RNT 10/012 RNT TWY A SFC MARKINGS FADED 1410132117-1505292359','plainLanguageMessage':'\u003ctable border\u003d\'0\'\u003e\r\n\u003ctbody\u003e\u003ctr\u003e\u003ctd\u003e\u003cb\u003eIssuing Airport:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e(RNT) Renton Muni \u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eNOTAM Number:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e 10/012\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eEffective Time Frame\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eBeginning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eMonday, October 13, 2014 2117 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eEnding:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFriday, May 29, 2015 2359 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eAffected Areas\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eTaxiway:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eA\u003c/td\u003e\u003c/tr\u003e\r\n\r\n\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Marking Type:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eSurface Markings \u003c/td\u003e\u003c/tr\u003e\r\n\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Status:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFaded \u003c/td\u003e\u003c/tr\u003e\r\n\r\n \u003ctr\u003e\u003ctd\u003e\u003cb\u003e\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\t\u003ctr\u003e\u003c/tr\u003e\r\n\u003c/tbody\u003e\u003c/table\u003e','phoneNumberOfCreator':'2064230087','faxNumberOfCreator':'','radioFrequencyOfCreator':'','traditionalMessageFrom4thWord':'TWY A SFC MARKINGS FADED 1410132117-1505292359','icaoId':'KRNT','accountId':'3069','airportName':'Renton Muni','procedure':false,'userID':0,'transactionID':38836136,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'keyword':'TWY','snowtam':false,'geometry':'POINT(-1642626.16790685 507740.060276838)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'10/017','featureName':'Aerodrome','issueDate':'10/21/2014 1915','startDate':'10/21/2014 1915','endDate':'01/23/2015 2359','source':'DN','sourceType':'D','icaoMessage':'\u003cp align\u003d\'left\'\u003e 10/017 NOTAMR\u003cbr\u003e \r\n\u003cb\u003eQ) \u003c/b\u003eZSE/QMMXX/IV/NBO/A/000/999/4729N12212W005\u003cbr\u003e \r\n\u003cb\u003eA) \u003c/b\u003eKRNT\u003cbr\u003e \r\n\u003cb\u003eB) \u003c/b\u003e1410211915\u003cbr\u003e \r\n\u003cb\u003eC) \u003c/b\u003e1501232359\u003cbr\u003e \r\n\r\n\u003cb\u003eE) \u003c/b\u003eRWY 16/34 SURFACE MARKINGS N SIDE NONSTD \u003cbr\u003e\u003c/p\u003e','traditionalMessage':'!RNT 10/017 RNT RWY 16/34 SURFACE MARKINGS N SIDE NOT STD 1410211915-1501232359','plainLanguageMessage':'\u003ctable border\u003d\'0\'\u003e\r\n\u003ctbody\u003e\u003ctr\u003e\u003ctd\u003e\u003cb\u003eIssuing Airport:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e(RNT) Renton Muni \u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eNOTAM Number:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e 10/017\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eEffective Time Frame\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eBeginning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eTuesday, October 21, 2014 1915 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eEnding:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFriday, January 23, 2015 2359 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eAffected Areas\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eRunway:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e16/34\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Marking Location:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eSurface Markings\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Direction:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eNorth \u003c/td\u003e\u003c/tr\u003e\r\n\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Marking Type:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eNon-Standard \u003c/td\u003e\u003c/tr\u003e\r\n\r\n \u003ctr\u003e\u003ctd\u003e\u003cb\u003e\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\t\u003ctr\u003e\u003c/tr\u003e\r\n\u003c/tbody\u003e\u003c/table\u003e','phoneNumberOfCreator':'2064230087','faxNumberOfCreator':'','radioFrequencyOfCreator':'','traditionalMessageFrom4thWord':'RWY 16/34 SURFACE MARKINGS N SIDE NOT STD 1410211915-1501232359','icaoId':'KRNT','accountId':'3069','airportName':'Renton Muni','procedure':false,'userID':0,'transactionID':38887675,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'keyword':'RWY','snowtam':false,'geometry':'POINT(-1642626.16790685 507740.060276838)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'12/001','featureName':'Aerodrome','issueDate':'12/01/2014 1712','startDate':'12/01/2014 1712','endDate':'12/19/2014 2359','source':'DN','sourceType':'D','icaoMessage':'\u003cp align\u003d\'left\'\u003e 12/001 NOTAMR\u003cbr\u003e \r\n\u003cb\u003eQ) \u003c/b\u003eZSE/QMRHW/IV/NBO/A/000/999/4729N12212W005\u003cbr\u003e \r\n\u003cb\u003eA) \u003c/b\u003eKRNT\u003cbr\u003e \r\n\u003cb\u003eB) \u003c/b\u003e1412011712\u003cbr\u003e \r\n\u003cb\u003eC) \u003c/b\u003e1412192359\u003cbr\u003e \r\n\r\n\u003cb\u003eE) \u003c/b\u003e RWY 16/34 WIP CONST ADJ NE EDGE\u003cbr\u003e\u003c/p\u003e','traditionalMessage':'!RNT 12/001 RNT RWY 16/34 WIP CONST ADJ NE EDGE 1412011712-1412192359','plainLanguageMessage':'\u003ctable border\u003d\'0\'\u003e\r\n\u003ctbody\u003e\u003ctr\u003e\u003ctd\u003e\u003cb\u003eIssuing Airport:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e(RNT) Renton Muni \u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eNOTAM Number:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e 12/001\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eEffective Time Frame\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eBeginning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eMonday, December 1, 2014 1712 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eEnding:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFriday, December 19, 2014 2359 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eAffected Areas\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eRunway:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e16/34\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Warning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e Work In Progress - Construction Adjacent to runway NorthEast Of Edge \u003c/td\u003e\u003c/tr\u003e\r\n\r\n \u003ctr\u003e\u003ctd\u003e\u003cb\u003e\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\t\u003ctr\u003e\u003c/tr\u003e\r\n\u003c/tbody\u003e\u003c/table\u003e','phoneNumberOfCreator':'2064230087','faxNumberOfCreator':'','radioFrequencyOfCreator':'','traditionalMessageFrom4thWord':'RWY 16/34 WIP CONST ADJ NE EDGE 1412011712-1412192359','icaoId':'KRNT','accountId':'3069','airportName':'Renton Muni','procedure':false,'userID':0,'transactionID':39184443,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'keyword':'RWY','snowtam':false,'geometry':'POINT(-1642630.95873854 507736.902699476)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'10/010','featureName':'Obstruction','issueDate':'10/07/2014 1752','startDate':'10/08/2014 1400','endDate':'12/19/2014 2359','source':'DN','sourceType':'D','icaoMessage':'\u003cp align\u003d\'left\'\u003e 10/010 NOTAMR\u003cbr\u003e\r\n\u003cb\u003eQ) \u003c/b\u003eZSE/QOBXX/IV/M/AE/000/124/4729N12212W005\u003cbr\u003e\r\n\u003cb\u003eA) \u003c/b\u003eKRNT\u003cbr\u003e\r\n\u003cb\u003eB) \u003c/b\u003e1410081400\u003cbr\u003e\r\n\u003cb\u003eC) \u003c/b\u003e1412192359\u003cbr\u003e \r\n \u003cb\u003eD)\u003c/b\u003e WED THU FRI SAT MON TUE 1400-2359 \u003cbr\u003e\r\n\u003cb\u003eE) \u003c/b\u003eOBST CRANE 37.79 (27.43) 0.25 SE( 472901N1221237W) APCH END RWY 34 FLAGGED AND NOT LGTD \u003cbr\u003e\u003c/p\u003e','traditionalMessage':'!RNT 10/010 RNT OBST CRANE (ASN UNKNOWN) 472901N1221237W (0.25NM SE APCH END RWY 34) \n 124FT (90FT AGL) FLAGGED AND NOT LGTD WED THU FRI SAT MON TUE 1400-2359 1410081400-1412192359','plainLanguageMessage':'\u003ctable border\u003d\'0\'\u003e\r\n\u003ctbody\u003e\u003ctr\u003e\u003ctd\u003e\u003cb\u003eClosest Airport:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eRNT (Renton Muni )( 0.25 NM SouthEast )\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eNOTAM Number:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e 10/010\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eEffective Time Frame\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eBeginning:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eWednesday, October 8, 2014 1400 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eEnding:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFriday, December 19, 2014 2359 (UTC)\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd colSpan\u003d\'2\'\u003e\u003cb\u003eAffected Areas\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eType:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eCrane\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eDirection from Airport:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e0.25 SouthEast \u003c/td\u003e\u003c/tr\u003e\r\n\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e AMSL:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e124 feet\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e AGL:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e90 feet\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003eCoordinates:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e 472901N/1221237W\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e APCH END :\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eRWY 34\u003c/td\u003e\u003c/tr\u003e\r\n\u003ctr\u003e\u003ctd\u003e\u003cb\u003e Light Status:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eFlagged and unlighted\u003c/td\u003e\u003c/tr\u003e\r\n \u003ctr\u003e\u003ctd\u003e\u003cb\u003e\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\r\n\t\u003ctr\u003e\u003ctd\u003e\u003cb\u003eSchedule:\u003c/b\u003e\u003c/td\u003e\u003ctd\u003eWednesday Thursday Friday Saturday Monday Tuesday 1400-2359 \u003c/td\u003e\u003c/tr\u003e\r\n\u003c/tbody\u003e\u003c/table\u003e','phoneNumberOfCreator':'2064230087','faxNumberOfCreator':'','radioFrequencyOfCreator':'','traditionalMessageFrom4thWord':'OBST CRANE (ASN UNKNOWN) 472901N1221237W (0.25NM SE APCH END RWY 34) \n 124FT (90FT AGL) FLAGGED AND NOT LGTD WED THU FRI SAT MON TUE 1400-2359 1410081400-1412192359','icaoId':'KRNT','accountId':'3069','airportName':'Renton Muni','procedure':false,'userID':0,'transactionID':38803118,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'keyword':'OBST','snowtam':false,'geometry':'POINT(-1642626.16790685 507740.060276838)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'LTA-RNT-1','featureName':'LTA','issueDate':'02/20/2014 1920','startDate':'02/21/2014 1200','endDate':'02/01/2016 0000','source':'LM','sourceType':'LTA','icaoMessage':' ','traditionalMessage':'CONSTRUCTION PROJECT ON NORTH CEDAR RIVER BRIDGE REPLACEMENT','plainLanguageMessage':'','traditionalMessageFrom4thWord':'CONSTRUCTION PROJECT ON NORTH CEDAR RIVER BRIDGE REPLACEMENT','icaoId':'KRNT','accountId':'0','airportName':'RENTON MUNI','procedure':false,'userID':0,'transactionID':35894025,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'comment':'https://notams.aim.faa.gov/lta/main/viewlta?lookupid\u003d642866992428095187','keyword':'LTA','snowtam':false,'geometry':'POINT(-1642626.16790685 507740.060276838)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'LTA-S46-1','featureName':'LTA','issueDate':'02/21/2014 1626','startDate':'02/21/2014 1700','endDate':'02/20/2016 1700','source':'LM','sourceType':'LTA','icaoMessage':' ','traditionalMessage':'VFR PRACTICE APPROACHES','plainLanguageMessage':'','traditionalMessageFrom4thWord':'VFR PRACTICE APPROACHES','icaoId':'KRNT','accountId':'0','airportName':'RENTON MUNI','procedure':false,'userID':0,'transactionID':35902593,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'comment':'https://notams.aim.faa.gov/lta/main/viewlta?lookupid\u003d659387065867179313','keyword':'LTA','snowtam':false,'geometry':'POINT(-1642626.16790685 507740.060276838)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0},{'facilityDesignator':'RNT','notamNumber':'LTA-RNT-3','featureName':'LTA','issueDate':'05/21/2014 1511','startDate':'05/23/2014 1200','endDate':'05/23/2016 1200','source':'LM','sourceType':'LTA','icaoMessage':' ','traditionalMessage':'CONSTRUCTION PROJECT FOR RUNWAY 34 BLAST WALL REPLACEMENT','plainLanguageMessage':'','traditionalMessageFrom4thWord':'CONSTRUCTION PROJECT FOR RUNWAY 34 BLAST WALL REPLACEMENT','icaoId':'KRNT','accountId':'0','airportName':'RENTON MUNI','procedure':false,'userID':0,'transactionID':37114422,'cancelledOrExpired':false,'digitalTppLink':false,'status':'Active','contractionsExpandedForPlainLanguage':false,'comment':'https://notams.aim.faa.gov/lta/main/viewlta?lookupid\u003d724779805437859657','keyword':'LTA','snowtam':false,'geometry':'POINT(-1642626.16790685 507740.060276838)','digitallyTransformed':false,'messageDisplayed':'concise','moreThan300Chars':false,'showingFullText':false,'requestID':0}],'startRecordCount':1,'endRecordCount':9,'totalNotamCount':9,'filteredResultCount':0,'criteriaCaption':' Location search on location(s) RNT','searchDateTime':'2014-12-17 17:11:25','error':'','requestID':0}";
            dynamic notamList = JObject.Parse(response);
            int notamCount = (int)notamList["totalNotamCount"];
            var list = new List<Notam>(notamCount);
            foreach (var notam in notamList["notamList"])
            {
                var n = new Notam { Designator = ((object)notam["facilityDesignator"]).ToString() };
                list.Add(n);
            }
        }

        private static void ParseWindsAloft()
        {
            using (var file = File.OpenRead(@"..\..\WindsAloft.txt"))
            {
                ParseModelOutputStatistic(file);
            }
        }

        private static void CreateWindsAloftStationDatabase()
        {
            var input = File.OpenRead(@"..\..\WindsAloftHawaii.txt");
            var output = File.OpenWrite(input.Name + ".output.cs");
            var writer = new StreamWriter(output);
            writer.AutoFlush = true;
            var reader = new StreamReader(input);
            for (int i = 0; i < 5; ++i)
            {
                reader.ReadLine(); //Advance to the 6th line
            }

            const string Header = @"var windsAloftStationMap = new Dictionary<string, BasicGeoposition>{";
            const string StationTemplate = @"    {{""{0}"", new BasicGeoposition {{ Latitude = {1:F2}, Longitude = {2:F2} }}}},";
            const string Footer = @"};";

            var database = new AirportDatabase();
            database.Initialize(@"..\..\..\AirportInformation\Assets\aixm.db");
            writer.WriteLine(Header);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    var station = line.Substring(1, 3);
                    var airport = database.LookupByDesignator(station);
                    if (airport != null)
                    {
                        writer.WriteLine(StationTemplate, station, airport.Location.Latitude, airport.Location.Longitude);
                    }
                    else
                    {
                        writer.WriteLine(StationTemplate, station, 0, 0);
                    }
                }
            }

            writer.WriteLine(Footer);
            writer.Flush();
        }
    }
}
