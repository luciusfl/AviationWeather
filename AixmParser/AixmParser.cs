//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AixmParser.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace ParseAIXM
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Windows.Devices.Geolocation;
    using AirportInformation.ViewModels;

    public class AixmParser
    {
        private const int Resolution = 50;
        private readonly Dictionary<string, Airport> airports = new Dictionary<string, Airport>(20000);

        private readonly Dictionary<string, Airport> airportsByDesignator = new Dictionary<string, Airport>(
            20000,
            StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> artifacts = new HashSet<string>();
        private readonly Dictionary<String, AirportBase> faaMembers = new Dictionary<String, AirportBase>(10000);

        private readonly Dictionary<String, List<AirportBase>> faaMembersByAirportId =
            new Dictionary<String, List<AirportBase>>(10000);

        private readonly List<Airport>[] geoLatitudes = new List<Airport>[180 * Resolution];
        private readonly List<Airport>[] geoLongitudes = new List<Airport>[360 * Resolution];
        private readonly Regex idregex = new Regex(@"\d{7}", RegexOptions.Compiled | RegexOptions.Singleline);
        // Airports by internal airport id.

        private readonly Dictionary<string, string> metarStations = new Dictionary<string, string>();
        // Airport by three letter designator. I.e. RNT for Renton.

        // Only used during parsing. Can be deleted after parsing is finished.
        private readonly Dictionary<string, Runway> runways = new Dictionary<string, Runway>(20000);

        public int Serialize(string filename)
        {
            var airports = 0;
            using (var writer = new StreamWriter(File.OpenWrite(filename)))
            {
                foreach (var airport in this.airports.Values)
                {
                    if (!airport.IsHeliport)
                    {
                        airport.Serialize(writer);
                        airports++;
                    }
                }

                writer.Flush();
            }

            return airports;
        }

        public void InitializeFromAixmFile(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }

            TrimZeros(filename);
            this.LoadMetarStations();
            var readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            readerSettings.IgnoreComments = true;
            readerSettings.IgnoreProcessingInstructions = true;
            readerSettings.IgnoreWhitespace = true;
            readerSettings.CloseInput = true;
            readerSettings.CheckCharacters = false;
            var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using (var reader = XmlReader.Create(file, readerSettings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "aixm:AirportHeliport":
                                this.ProcessAirport(reader);
                                break;
                            case "aixm:Runway":
                                this.ProcessRunwayV1(reader);
                                break;
                            case "aixm:TouchDownLiftOff":
                                this.ProcessTouchDownLiftOff(reader);
                                break;
                        }
                    }
                }
            }

            // Runways hashtable not needed anymore, so free up space.
            this.runways.Clear();
        }

        /// <summary>
        /// Removes trailing zeros. This is required, since the xml files can have trailing zeros,
        /// which makes the XmlReader throw an exception, despite setting CheckCharacters to false.
        /// </summary>
        /// <param name="filename"></param>
        private static void TrimZeros(string filename)
        {
            const int finish = 100;
            using (var aixm = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                aixm.Position = aixm.Length - finish;
                var aixmReader = new StreamReader(aixm);
                var buffer = new char[finish];
                aixmReader.ReadBlock(buffer, 0, finish);
                int zeros = buffer.Count(c => c == 0);
                if (zeros > 0)
                {
                    aixm.SetLength(aixm.Length - zeros);
                }            
            }
        }

        public void InitializeFromAixmFile2(string filename)
        {
            this.LoadMetarStations();
            var readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            readerSettings.IgnoreComments = true;
            readerSettings.IgnoreProcessingInstructions = true;
            readerSettings.IgnoreWhitespace = true;
            readerSettings.CloseInput = true;
            var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using (var reader = XmlReader.Create(file, readerSettings))
            {
                this.PhaseOne(reader); // Consolidate all airport artifacts together.
                foreach (var artifact in this.artifacts)
                {
                    Console.WriteLine(artifact);
                }
            }
        }

        private void PhaseOne(XmlReader reader)
        {
            var faaMemberCount = 0;
            while (reader.ReadToFollowing("faa:Member"))
            {
                faaMemberCount++;
                if (faaMemberCount % 1000 == 0)
                {
                    Console.WriteLine(faaMemberCount);
                }

                this.MoveToFirstChild(reader);
                var inner = reader.ReadSubtree();
                inner.Read();
                this.ProcessMember(inner);
                inner.Close();
            }
        }

        private void ProcessMember(XmlReader reader)
        {
            AirportBase artifact = null;
            string airportId = null;
            artifact = AirportArtifactFactory.Create(reader.Name);
            this.artifacts.Add(reader.Name);
            artifact.Id = reader.GetAttribute("gml:id");
            this.faaMembers.Add(artifact.Id, artifact);
            airportId = this.idregex.Match(artifact.Id).ToString();
            Debug.Assert(!string.IsNullOrEmpty(airportId));
            List<AirportBase> children;
            if (this.faaMembersByAirportId.TryGetValue(airportId, out children))
            {
                children.Add(artifact);
            }
            else
            {
                children = new List<AirportBase> { artifact };
                this.faaMembersByAirportId.Add(airportId, children);
            }

            switch (reader.Name)
            {
                case "aixm:AirportHeliport":
                {
                    break;
                }

                case "aixm:OrganisationAuthority":
                {
                    break;
                }

                case "aixm:Unit":
                {
                    break;
                }

                case "aixm:AirTrafficControlService":
                {
                    break;
                }

                case "aixm:RadioCommunicationChannel":
                {
                    artifact = AirportArtifactFactory.Create(reader.Name);
                    this.ProcessRadioCommunicationChannel(reader.ReadSubtree(), (RadioCommunicationChannel)artifact);
                    break;
                }

                case "aixm:AirportSuppliesService":
                {
                    break;
                }

                case "aixm:Runway":
                {
                    artifact = AirportArtifactFactory.Create(reader.Name);
                    this.ProcessRunway(reader, (Runway)artifact);
                    break;
                }

                case "aixm:RunwayMarking":
                {
                    break;
                }

                case "aixm:TouchDownLiftOff":
                {
                    break;
                }

                case "aixm:RunwayDirection":
                {
                    break;
                }

                case "aixm:Glidepath":
                {
                    break;
                }

                case "aixm:ApproachLightingSystem":
                {
                    break;
                }
            }
        }

        private void ProcessRadioCommunicationChannel(
            XmlReader reader,
            RadioCommunicationChannel radioCommunicationChannel)
        {
            radioCommunicationChannel.Id = reader.GetAttribute("gml:id");
            this.MoveToNextElement(reader, "aixm:frequencyTransmission");
            radioCommunicationChannel.Frequency = float.Parse(reader.ReadElementString());
        }

        private void ProcessRunway(XmlReader reader, Runway runway)
        {
            runway.Id = reader.GetAttribute("gml:id");
            runway.Designator = this.MoveToNextElementAndRead(reader, "aixm:designator");
            runway.Length = int.Parse(this.MoveToNextElementAndRead(reader, "aixm:lengthStrip"));
            runway.Width = int.Parse(this.MoveToNextElementAndRead(reader, "aixm:widthStrip"));
            if (reader.ReadToDescendant("aixm:SurfaceCharacteristics"))
            {
                runway.Composition = this.MoveToNextElementAndRead(reader, "aixm:composition");
            }
        }

        private void LoadMetarStations()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (resource.EndsWith(".stations.txt", StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = assembly.GetManifestResourceStream(resource))
                    {
                        var reader = new StreamReader(stream);
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            var key = line.Substring(0, 4);
                            string existingStation;
                            if (this.metarStations.TryGetValue(key, out existingStation))
                            {
                                if ((existingStation[5] != 'T' || existingStation[5] != 'U') && line[5] != ' ')
                                {
                                    this.metarStations.Remove(key);
                                    this.metarStations.Add(key, line);
                                }
                            }
                            else
                            {
                                this.metarStations.Add(key, line);
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void ProcessAirport(XmlReader reader)
        {
            var id = reader.GetAttribute("gml:id");
            var airport = new Airport { Id = id };
            /*
            if (id == "AH_0018126")
            {
                Debugger.Break();
            }
             * */

            this.MoveToNextElement(reader, "aixm:AirportHeliportTimeSlice");
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "aixm:name":
                                reader.Read();
                                airport.Name = reader.Value;
                                break;
                            case "aixm:locationIndicatorICAO":
                                reader.Read();
                                airport.IcaoIdenticator = reader.Value;
                                break;
                            case "aixm:designator":
                                reader.Read();
                                airport.Designator = reader.Value;
                                break;
                            case "aixm:type":
                                reader.Read();
                                airport.Type = reader.Value;
                                break;
                            case "aixm:fieldElevation":
                                reader.Read();
                                airport.FieldElevation = float.Parse(reader.Value);
                                break;
                            case "aixm:magneticVariation":
                                reader.Read();
                                airport.MagneticVariation = float.Parse(reader.Value);
                                break;
                            case "aixm:servedCity":
                                this.MoveToNextElement(reader, "aixm:name");
                                reader.Read();
                                airport.City = reader.Value;
                                break;
                            case "aixm:ElevatedPoint":
                                this.MoveToNextElement(reader, "gml:pos");
                                reader.Read();
                                var pos = reader.Value.Split(' ');
                                airport.Location = new BasicGeoposition
                                {
                                    Longitude = float.Parse(pos[0]),
                                    Latitude = float.Parse(pos[1])
                                };
                                break;
                            case "aixm:transitionAltitude":
                                reader.Read();
                                airport.TransitionAltitude = int.Parse(reader.Value);
                                break;
                            case "apt:AirportHeliportExtension":
                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.EndElement &&
                                        reader.Name == "apt:AirportHeliportExtension")
                                    {
                                        break;
                                    }

                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        switch (reader.Name)
                                        {
                                            case "apt:administativeArea":
                                                reader.Read();
                                                airport.State = reader.Value;
                                                break;
                                            case "apt:ownershipType":
                                                reader.Read();
                                                airport.Ownership = reader.Value;
                                                break;
                                            case "apt:trafficControlTowerOnAirport":
                                                reader.Read();
                                                airport.ControlledAirport = reader.Value != "NO";
                                                break;
                                        }
                                    }
                                }

                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "aixm:AirportHeliport")
                        {
                            this.airports.Add(id, airport);
                            this.airportsByDesignator.Add(airport.Designator, airport);
                            if (airport.IcaoIdenticator != null)
                            {
                                string station;
                                if (this.metarStations.TryGetValue(airport.IcaoIdenticator, out station))
                                {
                                    airport.HasMetarStation = true;
                                    if (station[5] == 'T' || station[5] == 'U')
                                    {
                                        airport.HasTaf = true;
                                    }
                                }
                            }

                            this.Add(airport);
                            return;
                        }

                        break;
                }
            }
        }

        private int MapLatitude(double latitude)
        {
            return (int)((latitude + 90) * Resolution);
        }

        private int MapLongitude(double longitude)
        {
            return (int)((longitude + 180) * Resolution);
        }

        public void Add(Airport airport)
        {
            var latIndex = this.MapLatitude(airport.Location.Latitude);
            if (this.geoLatitudes[latIndex] == null)
            {
                this.geoLatitudes[latIndex] = new List<Airport> { airport };
            }
            else
            {
                this.geoLatitudes[latIndex].Add(airport);
            }

            var lonIndex = this.MapLongitude(airport.Location.Longitude);
            if (this.geoLongitudes[lonIndex] == null)
            {
                this.geoLongitudes[lonIndex] = new List<Airport> { airport };
            }
            else
            {
                this.geoLongitudes[lonIndex].Add(airport);
            }
        }

        private void ProcessRunwayV1(XmlReader reader)
        {
            var id = reader.GetAttribute("gml:id");
            var runway = new Runway { Id = id };
            this.MoveToNextElement(reader, "aixm:RunwayTimeSlice");
            if (runway.Id.StartsWith("RWY_RECIPROCAL_END") || runway.Id.StartsWith("RWY_BASE_END"))
            {
                this.ProcessRunwayEnd(reader, runway);
            }
            else
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "aixm:SurfaceCharacteristics":
                                    this.MoveToNextElement(reader, "aixm:composition");
                                    reader.Read();
                                    runway.Composition = reader.Value;
                                    break;
                                case "aixm:associatedAirportHeliport":
                                    runway.AirportId = this.GetIdFromHref(reader.GetAttribute("xlink:href"));
                                    break;
                                case "aixm:designator":
                                    reader.Read();
                                    runway.Designator = reader.Value;
                                    break;
                                case "aixm:lengthStrip":
                                    reader.Read();
                                    runway.Length = int.Parse(reader.Value);
                                    break;
                                case "aixm:widthStrip":
                                    reader.Read();
                                    runway.Width = int.Parse(reader.Value);
                                    break;
                            }

                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name == "aixm:Runway")
                            {
                                this.airports[runway.AirportId].Runways.Add(runway);
                                this.runways.Add(runway.Id, runway);
                                return;
                            }

                            break;
                    }
                }
            }
        }

        private void ProcessRunwayEnd(XmlReader reader, Runway runway)
        {
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "aixm:associatedAirportHeliport":
                                runway.AirportId = this.GetIdFromHref(reader.GetAttribute("xlink:href"));
                                break;
                            case "aixm:designator":
                                reader.Read();
                                runway.Designator = reader.Value;
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name == "aixm:Runway")
                        {
                            var airport = this.airports[runway.AirportId];
                            foreach (var rwy in airport.Runways)
                            {
                                if (rwy.Designator.StartsWith(runway.Designator) ||
                                    rwy.Designator.EndsWith(runway.Designator))
                                {
                                    // Correct runway identified.
                                    var direction = new RunwayDirection
                                    {
                                        Designator = runway.Designator,
                                        TrueBearing = -1
                                    };
                                    if (runway.Id.StartsWith("RWY_BASE_END"))
                                    {
                                        rwy.Base = direction;
                                    }
                                    else if (runway.Id.StartsWith("RWY_RECIPROCAL_END"))
                                    {
                                        rwy.Reciprocal = direction;
                                    }
                                    else
                                    {
                                        Debug.Assert(true, "Runway ID invalid.");
                                    }
                                }
                            }

                            this.runways.Add(runway.Id, runway);
                            return;
                        }

                        break;
                }
            }
        }

        private void ProcessTouchDownLiftOff(XmlReader reader)
        {
            var id = reader.GetAttribute("gml:id");
            string runwayEndDesignator = null;
            Airport airport = null;
            this.MoveToNextElement(reader, "aixm:TouchDownLiftOffTimeSlice");
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "aixm:designator":
                                reader.Read();
                                runwayEndDesignator = reader.Value;
                                break;
                            case "aixm:associatedAirportHeliport":
                                var airportId = this.GetIdFromHref(reader.GetAttribute("xlink:href"));
                                airport = this.airports[airportId];
                                break;
                            case "aixm:annotation":
                                this.ProcessTouchDownLiftOffAnnotation(reader, airport, runwayEndDesignator);
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name == "aixm:TouchDownLiftOff")
                        {
                            return;
                        }

                        break;
                }
            }
        }

        private void ProcessTouchDownLiftOffAnnotation(XmlReader reader, Airport airport, string runwayEndDesignator)
        {
            reader.Read();
            var id = reader.GetAttribute("gml:id");
            if (id.Contains("RIGHTHANDTRAFFICPATTERN"))
            {
                this.MoveToNextElement(reader, "aixm:note");
                reader.Read();
                foreach (var rwy in airport.Runways)
                {
                    if (rwy.Base.Designator == runwayEndDesignator)
                    {
                        rwy.Base.TrafficDirection = reader.Value == "NO" ? 'L' : 'R';
                    }
                    else if (rwy.Reciprocal != null && rwy.Reciprocal.Designator == runwayEndDesignator)
                    {
                        rwy.Reciprocal.TrafficDirection = reader.Value == "NO" ? 'L' : 'R';
                    }
                }
            }
            else if (id.Contains("TRUE_BEARING"))
            {
                this.MoveToNextElement(reader, "aixm:note");
                reader.Read();
                foreach (var rwy in airport.Runways)
                {
                    if (rwy.Base.Designator == runwayEndDesignator)
                    {
                        rwy.Base.TrueBearing = int.Parse(reader.Value);
                    }
                    else if (rwy.Reciprocal != null && rwy.Reciprocal.Designator == runwayEndDesignator)
                    {
                        rwy.Reciprocal.TrueBearing = int.Parse(reader.Value);
                    }
                }
            }
        }

        private string GetIdFromHref(string href)
        {
            var startid = href.IndexOf("id='") + 4;
            var endid = href.IndexOf("'%5D", startid);
            return href.Substring(startid, endid - startid);
        }

        private void MoveToNextElement(XmlReader reader, string element)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == element)
                {
                    return;
                }
            } while (reader.Read());
        }

        private string MoveToNextElementAndRead(XmlReader reader, string element)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == element)
                {
                    return reader.ReadElementString();
                }
            } while (reader.Read());
            return null;
        }

        private void MoveToFirstChild(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    return;
                }
            }
        }
    }
}
