//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Airport.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;
    using Windows.Devices.Geolocation;
    using metar;
    using Newtonsoft.Json.Linq;
    using taf;
    using response = taf.response;
    using AixmDatabase;
    using Common;
    using System.Net.Http;

    [DebuggerDisplay(
        "Id = {Id}, Designator = {Designator}, ArtifactType = {ArtifactType}, ArtifactType={ArtifactType}, State={State}, City={City}"
        )]
    public sealed class Airport : AirportBase, INotifyPropertyChanged
    {
        private WindsAloft windsAloft = new WindsAloft();

        private static readonly CacheControlHeaderValue cacheControl = new CacheControlHeaderValue { NoCache = true };

        private string areaForecast = null;

        private static readonly List<Tuple<string, BasicGeoposition>> weatherForecastAreaMap =
            new List<Tuple<string, BasicGeoposition>>
            {
                // Lookup coordinates from METAR stations. If not a metar stations get get cities
                // from http://aviationweather.gov/fcstdisc/data and use http://www.latlong.net/ to lookup up lat lon of city.

                new Tuple<string, BasicGeoposition>(
                    "KABQ",
                    new BasicGeoposition { Latitude = 35.05, Longitude = -106.62 }),
                new Tuple<string, BasicGeoposition>(
                    "KABR",
                    new BasicGeoposition { Latitude = 45.45, Longitude = -98.42 }),
                new Tuple<string, BasicGeoposition>(
                    "KAKQ",
                    new BasicGeoposition { Latitude = 36.98, Longitude = -77.0 }),
                new Tuple<string, BasicGeoposition>(
                    "KAMA",
                    new BasicGeoposition { Latitude = 35.22, Longitude = -101.72 }),
                new Tuple<string, BasicGeoposition>(
                    "KBGM",
                    new BasicGeoposition { Latitude = 42.22, Longitude = -75.98 }),
                new Tuple<string, BasicGeoposition>(
                    "KBIS",
                    new BasicGeoposition { Latitude = 46.77, Longitude = -100.75 }),
                new Tuple<string, BasicGeoposition>(
                    "KBOI",
                    new BasicGeoposition { Latitude = 43.57, Longitude = -116.23 }),
                new Tuple<string, BasicGeoposition>(
                    "KBRO",
                    new BasicGeoposition { Latitude = 25.92, Longitude = -97.42 }),
                new Tuple<string, BasicGeoposition>(
                    "KBTV",
                    new BasicGeoposition { Latitude = 44.47, Longitude = -73.15 }),
                new Tuple<string, BasicGeoposition>(
                    "KBUF",
                    new BasicGeoposition { Latitude = 42.93, Longitude = -78.73 }),
                new Tuple<string, BasicGeoposition>(
                    "KBYZ", // KBIL is a bug in aviationweather.gov
                    new BasicGeoposition { Latitude = 45.8, Longitude = -108.55 }),
                new Tuple<string, BasicGeoposition>(
                    "KCAE",
                    new BasicGeoposition { Latitude = 33.93, Longitude = -81.12 }),
                new Tuple<string, BasicGeoposition>(
                    "KCAR",
                    new BasicGeoposition { Latitude = 46.87, Longitude = -68.02 }),
                new Tuple<string, BasicGeoposition>(
                    "KCHS",
                    new BasicGeoposition { Latitude = 32.9, Longitude = -80.03 }),
                new Tuple<string, BasicGeoposition>(
                    "KCLE",
                    new BasicGeoposition { Latitude = 41.42, Longitude = -81.85 }),
                new Tuple<string, BasicGeoposition>(
                    "KCRP",
                    new BasicGeoposition { Latitude = 27.77, Longitude = -97.5 }),
                new Tuple<string, BasicGeoposition>(
                    "KCYS",
                    new BasicGeoposition { Latitude = 41.15, Longitude = -104.8 }),
                new Tuple<string, BasicGeoposition>(
                    "KDDC",
                    new BasicGeoposition { Latitude = 37.77, Longitude = -99.97 }),
                new Tuple<string, BasicGeoposition>(
                    "KDLH",
                    new BasicGeoposition { Latitude = 46.85, Longitude = -92.2 }),
                new Tuple<string, BasicGeoposition>(
                    "KDTW",
                    new BasicGeoposition { Latitude = 42.23, Longitude = -83.33 }),
                new Tuple<string, BasicGeoposition>(
                    "KDVN",
                    new BasicGeoposition { Latitude = 41.62, Longitude = -90.58 }),
                new Tuple<string, BasicGeoposition>(
                    "KFFC",
                    new BasicGeoposition { Latitude = 33.35, Longitude = -84.57 }),
                new Tuple<string, BasicGeoposition>(
                    "KFSD",
                    new BasicGeoposition { Latitude = 43.58, Longitude = -96.75 }),
                new Tuple<string, BasicGeoposition>(
                    "KGGW",
                    new BasicGeoposition { Latitude = 48.22, Longitude = -106.62 }),
                new Tuple<string, BasicGeoposition>(
                    "KGJT",
                    new BasicGeoposition { Latitude = 39.12, Longitude = -108.52 }),
                new Tuple<string, BasicGeoposition>(
                    "KGLD",
                    new BasicGeoposition { Latitude = 39.37, Longitude = -101.7 }),
                new Tuple<string, BasicGeoposition>(
                    "KGRB",
                    new BasicGeoposition { Latitude = 44.48, Longitude = -88.13 }),
                new Tuple<string, BasicGeoposition>(
                    "KGRR",
                    new BasicGeoposition { Latitude = 42.88, Longitude = -85.52 }),
                new Tuple<string, BasicGeoposition>(
                    "KGSP",
                    new BasicGeoposition { Latitude = 34.9, Longitude = -82.22 }),
                new Tuple<string, BasicGeoposition>(
                    "KICT",
                    new BasicGeoposition { Latitude = 37.65, Longitude = -97.43 }),
                new Tuple<string, BasicGeoposition>(
                    "KILM",
                    new BasicGeoposition { Latitude = 34.27, Longitude = -77.9 }),
                new Tuple<string, BasicGeoposition>(
                    "KILN",
                    new BasicGeoposition { Latitude = 39.43, Longitude = -83.8 }),
                new Tuple<string, BasicGeoposition>(
                    "KIND",
                    new BasicGeoposition { Latitude = 39.72, Longitude = -86.3 }),
                new Tuple<string, BasicGeoposition>(
                    "KJAN",
                    new BasicGeoposition { Latitude = 32.32, Longitude = -90.08 }),
                new Tuple<string, BasicGeoposition>(
                    "KJAX",
                    new BasicGeoposition { Latitude = 30.5, Longitude = -81.68 }),
                new Tuple<string, BasicGeoposition>(
                    "KJKL",
                    new BasicGeoposition { Latitude = 37.6, Longitude = -83.32 }),
                new Tuple<string, BasicGeoposition>(
                    "KLBF",
                    new BasicGeoposition { Latitude = 41.12, Longitude = -100.67 }),
                new Tuple<string, BasicGeoposition>(
                    "KLCH",
                    new BasicGeoposition { Latitude = 30.13, Longitude = -93.22 }),
                new Tuple<string, BasicGeoposition>("KLOT", new BasicGeoposition { Latitude = 41.6, Longitude = -88.1 }),
                new Tuple<string, BasicGeoposition>(
                    "KMAF",
                    new BasicGeoposition { Latitude = 31.95, Longitude = -102.2 }),
                new Tuple<string, BasicGeoposition>(
                    "KMFR",
                    new BasicGeoposition { Latitude = 42.38, Longitude = -122.87 }),
                new Tuple<string, BasicGeoposition>(
                    "KMLB",
                    new BasicGeoposition { Latitude = 28.1, Longitude = -80.65 }),
                new Tuple<string, BasicGeoposition>(
                    "KMOB",
                    new BasicGeoposition { Latitude = 30.68, Longitude = -88.25 }),
                new Tuple<string, BasicGeoposition>(
                    "KMSO",
                    new BasicGeoposition { Latitude = 46.92, Longitude = -114.1 }),
                new Tuple<string, BasicGeoposition>(
                    "KOUN",
                    new BasicGeoposition { Latitude = 35.25, Longitude = -97.47 }),
                new Tuple<string, BasicGeoposition>(
                    "KPAH",
                    new BasicGeoposition { Latitude = 37.07, Longitude = -88.77 }),
                new Tuple<string, BasicGeoposition>(
                    "KPDT",
                    new BasicGeoposition { Latitude = 45.7, Longitude = -118.83 }),
                new Tuple<string, BasicGeoposition>(
                    "KPIH",
                    new BasicGeoposition { Latitude = 42.92, Longitude = -112.57 }),
                new Tuple<string, BasicGeoposition>(
                    "KPUB",
                    new BasicGeoposition { Latitude = 38.28, Longitude = -104.5 }),
                new Tuple<string, BasicGeoposition>(
                    "KRIW",
                    new BasicGeoposition { Latitude = 43.07, Longitude = -108.47 }),
                new Tuple<string, BasicGeoposition>(
                    "KSGF",
                    new BasicGeoposition { Latitude = 37.23, Longitude = -93.38 }),
                new Tuple<string, BasicGeoposition>(
                    "KSHV",
                    new BasicGeoposition { Latitude = 32.45, Longitude = -93.83 }),
                new Tuple<string, BasicGeoposition>(
                    "KSJT",
                    new BasicGeoposition { Latitude = 31.37, Longitude = -100.5 }),
                new Tuple<string, BasicGeoposition>(
                    "KSLC",
                    new BasicGeoposition { Latitude = 40.77, Longitude = -111.97 }),
                new Tuple<string, BasicGeoposition>(
                    "KTOP",
                    new BasicGeoposition { Latitude = 39.07, Longitude = -95.63 }),
                new Tuple<string, BasicGeoposition>(
                    "TJSJ",
                    new BasicGeoposition { Latitude = 18.43, Longitude = -66.02 }),
                new Tuple<string, BasicGeoposition>(
                    "KBOU",
                    new BasicGeoposition { Latitude = 40.01, Longitude = -105.27 }),
                new Tuple<string, BasicGeoposition>(
                    "KBOX",
                    new BasicGeoposition { Latitude = 42.27, Longitude = -71.11 }),
                new Tuple<string, BasicGeoposition>(
                    "KCTP",
                    new BasicGeoposition { Latitude = 40.79, Longitude = -77.86 }),
                new Tuple<string, BasicGeoposition>(
                    "KDMX",
                    new BasicGeoposition { Latitude = 41.60, Longitude = -93.61 }),
                new Tuple<string, BasicGeoposition>(
                    "KEAX",
                    new BasicGeoposition { Latitude = 39.1, Longitude = -94.58 }),
                new Tuple<string, BasicGeoposition>(
                    "KEKA",
                    new BasicGeoposition { Latitude = 40.80, Longitude = -124.16 }),
                new Tuple<string, BasicGeoposition>(
                    "KEPZ",
                    new BasicGeoposition { Latitude = 31.78, Longitude = -106.44 }),
                new Tuple<string, BasicGeoposition>(
                    "KEWX",
                    new BasicGeoposition { Latitude = 29.42, Longitude = -98.49 }),
                new Tuple<string, BasicGeoposition>(
                    "KFGF",
                    new BasicGeoposition { Latitude = 47.93, Longitude = -97.03 }),
                new Tuple<string, BasicGeoposition>(
                    "KFGZ",
                    new BasicGeoposition { Latitude = 35.2, Longitude = -111.65 }),
                new Tuple<string, BasicGeoposition>(
                    "KFWD",
                    new BasicGeoposition { Latitude = 32.76, Longitude = -97.33 }),
                new Tuple<string, BasicGeoposition>(
                    "KGID",
                    new BasicGeoposition { Latitude = 40.59, Longitude = -98.39 }),
                new Tuple<string, BasicGeoposition>(
                    "KGYX",
                    new BasicGeoposition { Latitude = 43.89, Longitude = -70.33 }),
                new Tuple<string, BasicGeoposition>(
                    "KHGX",
                    new BasicGeoposition { Latitude = 29.76, Longitude = -95.37 }),
                new Tuple<string, BasicGeoposition>(
                    "KHNX",
                    new BasicGeoposition { Latitude = 34.1, Longitude = -117.58 }),
                new Tuple<string, BasicGeoposition>(
                    "KHUN",
                    new BasicGeoposition { Latitude = 34.73, Longitude = -86.57 }),
                new Tuple<string, BasicGeoposition>(
                    "KILX",
                    new BasicGeoposition { Latitude = 40.15, Longitude = -89.36 }),
                new Tuple<string, BasicGeoposition>(
                    "KIWX",
                    new BasicGeoposition { Latitude = 41.33, Longitude = -85.7 }),
                new Tuple<string, BasicGeoposition>(
                    "KKEY",
                    new BasicGeoposition { Latitude = 24.56, Longitude = -81.78 }),
                new Tuple<string, BasicGeoposition>(
                    "KLKN",
                    new BasicGeoposition { Latitude = 40.83, Longitude = -115.76 }),
                new Tuple<string, BasicGeoposition>(
                    "KLMK",
                    new BasicGeoposition { Latitude = 38.25, Longitude = -85.76 }),
                new Tuple<string, BasicGeoposition>(
                    "KLOX",
                    new BasicGeoposition { Latitude = 34.15, Longitude = -119.21 }),
                new Tuple<string, BasicGeoposition>(
                    "KLSX",
                    new BasicGeoposition { Latitude = 38.63, Longitude = -90.2 }),
                new Tuple<string, BasicGeoposition>(
                    "KLUB",
                    new BasicGeoposition { Latitude = 33.58, Longitude = -101.86 }),
                new Tuple<string, BasicGeoposition>(
                    "KLWX",
                    new BasicGeoposition { Latitude = 38.91, Longitude = -76.93 }),
                new Tuple<string, BasicGeoposition>(
                    "KLZK",
                    new BasicGeoposition { Latitude = 34.75, Longitude = -92.29 }),
                new Tuple<string, BasicGeoposition>(
                    "KMEG",
                    new BasicGeoposition { Latitude = 35.15, Longitude = -90.05 }),
                new Tuple<string, BasicGeoposition>(
                    "KMFL",
                    new BasicGeoposition { Latitude = 26.1, Longitude = -80.13 }),
                new Tuple<string, BasicGeoposition>(
                    "KMHX",
                    new BasicGeoposition { Latitude = 34.72, Longitude = -76.73 }),
                new Tuple<string, BasicGeoposition>(
                    "KMKX",
                    new BasicGeoposition { Latitude = 43.04, Longitude = -87.91 }),
                new Tuple<string, BasicGeoposition>(
                    "KMPX",
                    new BasicGeoposition { Latitude = 44.98, Longitude = -93.27 }),
                new Tuple<string, BasicGeoposition>(
                    "KMQT",
                    new BasicGeoposition { Latitude = 46.58, Longitude = -87.395 }),
                new Tuple<string, BasicGeoposition>(
                    "KMRX",
                    new BasicGeoposition { Latitude = 36.02, Longitude = -83.6 }),
                new Tuple<string, BasicGeoposition>(
                    "KMTR",
                    new BasicGeoposition { Latitude = 37.774, Longitude = -122.42 }),
                new Tuple<string, BasicGeoposition>(
                    "KOAX",
                    new BasicGeoposition { Latitude = 41.25, Longitude = -96.0 }),
                new Tuple<string, BasicGeoposition>(
                    "KOHX",
                    new BasicGeoposition { Latitude = 36.16, Longitude = -86.781 }),
                new Tuple<string, BasicGeoposition>(
                    "KOKX",
                    new BasicGeoposition { Latitude = 40.712, Longitude = -74.0 }),
                new Tuple<string, BasicGeoposition>(
                    "KOTX",
                    new BasicGeoposition { Latitude = 47.66, Longitude = -117.43 }),
                new Tuple<string, BasicGeoposition>("KPBZ", new BasicGeoposition { Latitude = 40.44, Longitude = -80 }),
                new Tuple<string, BasicGeoposition>(
                    "KPHI",
                    new BasicGeoposition { Latitude = 39.95, Longitude = -75.17 }),
                new Tuple<string, BasicGeoposition>(
                    "KPQR",
                    new BasicGeoposition { Latitude = 45.52, Longitude = -122.676 }),
                new Tuple<string, BasicGeoposition>(
                    "KPSR",
                    new BasicGeoposition { Latitude = 33.45, Longitude = -112.07 }),
                new Tuple<string, BasicGeoposition>(
                    "KRAH",
                    new BasicGeoposition { Latitude = 35.78, Longitude = -78.64 }),
                new Tuple<string, BasicGeoposition>(
                    "KREV",
                    new BasicGeoposition { Latitude = 39.53, Longitude = 119.81 }),
                new Tuple<string, BasicGeoposition>(
                    "KRLX",
                    new BasicGeoposition { Latitude = 38.35, Longitude = -81.63 }),
                new Tuple<string, BasicGeoposition>(
                    "KRNK",
                    new BasicGeoposition { Latitude = 37.27, Longitude = -79.94 }),
                new Tuple<string, BasicGeoposition>(
                    "KSEW",
                    new BasicGeoposition { Latitude = 47.60, Longitude = -122.33 }),
                new Tuple<string, BasicGeoposition>(
                    "KSGX",
                    new BasicGeoposition { Latitude = 32.716, Longitude = -117.16 }),
                new Tuple<string, BasicGeoposition>(
                    "KTAE",
                    new BasicGeoposition { Latitude = 30.438, Longitude = -84.28 }),
                new Tuple<string, BasicGeoposition>(
                    "KTBW",
                    new BasicGeoposition { Latitude = 27.7, Longitude = -82.58 }),
                new Tuple<string, BasicGeoposition>(
                    "KTFX",
                    new BasicGeoposition { Latitude = 47.5, Longitude = 111.28 }),
                new Tuple<string, BasicGeoposition>("KTSA", new BasicGeoposition { Latitude = 36.153, Longitude = -96 }),
                new Tuple<string, BasicGeoposition>(
                    "KTWC",
                    new BasicGeoposition { Latitude = 32.22, Longitude = -110.92 }),
                new Tuple<string, BasicGeoposition>(
                    "KUNR",
                    new BasicGeoposition { Latitude = 44.08, Longitude = -103.23 }),
                new Tuple<string, BasicGeoposition>(
                    "KVEF",
                    new BasicGeoposition { Latitude = 36.17, Longitude = -115.14 }),
                new Tuple<string, BasicGeoposition>(
                    "PHFO",
                    new BasicGeoposition { Latitude = 21.31, Longitude = -157.86 })
            };

        private static readonly string[] Helipads =
        {
            "HP", "HI", "H1", "H2", "H3", "H4", "H5", "H6", "H7", "H8", "H9",
            "H10", "B1", "H-A", "H-B", "H-C", "H-D", "H-E", "H-F", "HB", "HF"
        };

        private readonly Regex dateRegex = new Regex(
            @"\d{1,2}/\d{1,2}/20\d{2}  \d{4}",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private string areaForecastDiscussion;
        private METAR metar;
        private ObservableCollection<ModelOutputStatistic> mos;
        private ObservableCollection<Notam> notams;
        private Collection<Runway> runways = new Collection<Runway>();
        private TAF[] tafs;
        public bool HasMetarStation { get; set; }
        public string IcaoIdenticator { get; set; }
        public string Designator { get; set; }
        public string Name { get; set; }
        public BasicGeoposition Location { get; set; }
        public string State { get; set; }
        public bool ControlledAirport { get; set; }
        public float MagneticVariation { get; set; }
        public float FieldElevation { get; set; }
        public string Ownership { get; set; }
        public string City { get; set; }
        public int TransitionAltitude { get; set; }

        public Collection<Runway> Runways
        {
            get { return this.runways; }
        }

        public int LongestRunwayLength
        {
            get
            {
                var longest = 0;
                foreach (var runway in this.runways)
                {
                    if (runway.Length > longest)
                    {
                        longest = runway.Length;
                    }
                }

                return longest;
            }
        }

        public string Type { get; set; }
        public bool HasTaf { get; set; }

        public ObservableCollection<ModelOutputStatistic> Mos
        {
            get
            {
                if (this.mos == null && !string.IsNullOrEmpty(this.IcaoIdenticator))
                {
                    this.mos = new ObservableCollection<ModelOutputStatistic>();
                    this.GetLocalizedAviationModelOutputStatisticsAsync(this.IcaoIdenticator);
                }

                return this.mos;
            }
        }

        public WindsAloft WindsAloft
        {
            get { return this.windsAloft; }

            set
            {
                if (this.windsAloft != value)
                {
                    this.windsAloft = value;
                    this.NotifyPropertyChanged("WindsAloft");
                }
            }
        }

        public string AreaForecast
        {
            get
            {
                return this.areaForecast;
            }

            set
            {
                if (this.areaForecast != value)
                {
                    this.areaForecast = value;
                    this.NotifyPropertyChanged("AreaForecast");
                }
            }
        }

        public ObservableCollection<Notam> Notams
        {
            get
            {
                if (this.notams == null)
                {
                    this.notams = new ObservableCollection<Notam>();
                    this.GetNotamsAsync();
                }

                return this.notams;
            }
        }

        public bool IsHeliport
        {
            get { return Helipads.Contains(this.Type); }
        }

        public string AreaForecastDiscussion
        {
            get
            {
                if (this.areaForecastDiscussion == null)
                {
                    this.GetForecastDiscussionAsync();
                }

                return this.areaForecastDiscussion;
            }
            set
            {
                if (this.areaForecastDiscussion != value)
                {
                    this.areaForecastDiscussion = value;
                    this.NotifyPropertyChanged("AreaForecastDiscussion");
                }
            }
        }

        public METAR Metar
        {
            get
            {
                return this.metar;
            }

            set
            {
                if (this.metar != value)
                {
                    this.metar = value;
                    this.NotifyPropertyChanged("Metar");
                    this.NotifyPropertyChanged("MagneticWindDirection");
                }
            }
        }

        public int MagneticWindDirection
        {
            get
            {
                if (this.Metar != null)
                {
                    if (this.Metar.wind_speed_ktSpecified && this.Metar.wind_dir_degreesSpecified && this.Metar.wind_speed_kt > 0 &&
                        this.Metar.wind_dir_degrees == 0)
                    {
                        return 0; // Wind variable.
                    }

                    int dir = this.Metar.wind_dir_degrees - (int)this.MagneticVariation;
                    if (dir >= 360)
                    {
                        return dir - 360;
                    }
                    else if (dir < 0)
                    {
                        return dir + 360;
                    }
                    else
                    {
                        return dir;
                    }
                }
                else
                {
                    return 777;
                }
            }
        }

        public TAF[] Taf
        {
            get
            {
                if (this.tafs == null && this.HasTaf)
                {
                    this.LoadTafsAsync();
                }

                return this.tafs;
            }

            set
            {
                if (this.tafs != value)
                {
                    this.tafs = value;
                    this.NotifyPropertyChanged("Taf");
                }
            }
        }

        public Uri FacilityDirectoryUrl
        {
            get { return new Uri(@"http://vfrmap.com/fe?req=get_afd&amp;q=" + this.Designator); }
            private set { ; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override void Serialize(StreamWriter writer)
        {
            writer.Write("A");
            base.Serialize(writer);
            writer.Write(this.IcaoIdenticator);
            writer.Write('|');
            writer.Write(this.Designator);
            writer.Write('|');
            writer.Write(this.Name);
            writer.Write('|');
            writer.Write(this.Location.Latitude);
            writer.Write('|');
            writer.Write(this.Location.Longitude);
            writer.Write('|');
            writer.Write(this.State);
            writer.Write('|');
            writer.Write(this.ControlledAirport ? 1 : 0);
            writer.Write('|');
            writer.Write(this.MagneticVariation);
            writer.Write('|');
            writer.Write(this.FieldElevation);
            writer.Write('|');
            writer.Write(this.Ownership);
            writer.Write('|');
            writer.Write(this.City);
            writer.Write('|');
            writer.Write(this.TransitionAltitude);
            writer.Write('|');
            writer.Write(this.Type);
            writer.Write('|');
            writer.Write(this.HasTaf ? 1 : 0); // Bool writes as "True" and "False", which is inefficient.
            writer.Write('|');
            writer.Write(this.HasMetarStation ? 1 : 0);
            writer.Write('|');
            writer.Write(this.runways.Count);
            writer.Write('|');
            foreach (var runway in this.runways)
            {
                runway.Serialize(writer);
            }
        }

        public override void Deserialize(StreamReader reader)
        {
            base.Deserialize(reader); // Call base serializer.
            this.IcaoIdenticator = this.DeserializeReadNext(reader);
            this.Designator = this.DeserializeReadNext(reader);
            this.Name = this.DeserializeReadNext(reader);
            this.Location = new BasicGeoposition
            {
                Latitude = double.Parse(this.DeserializeReadNext(reader)),
                Longitude = double.Parse(this.DeserializeReadNext(reader))
            };
            this.State = this.DeserializeReadNext(reader);
            this.ControlledAirport = 1 == byte.Parse(this.DeserializeReadNext(reader));
            this.MagneticVariation = float.Parse(this.DeserializeReadNext(reader));
            this.FieldElevation = float.Parse(this.DeserializeReadNext(reader));
            this.Ownership = this.DeserializeReadNext(reader);
            this.City = this.DeserializeReadNext(reader);
            this.TransitionAltitude = int.Parse(this.DeserializeReadNext(reader));
            this.Type = this.DeserializeReadNext(reader);
            this.HasTaf = 1 == byte.Parse(this.DeserializeReadNext(reader));
            this.HasMetarStation = 1 == byte.Parse(this.DeserializeReadNext(reader));
            var runwayCount = int.Parse(this.DeserializeReadNext(reader));
            this.runways = new Collection<Runway>();
            for (var i = 0; i < runwayCount; ++i)
            {
                var rwy = new Runway();
                rwy.Deserialize(reader);
                this.runways.Add(rwy);
            }
        }

        private async void LoadTafsAsync()
        {
            Debug.Assert(!string.IsNullOrEmpty(this.IcaoIdenticator));
            var uri =
                new Uri(
                    "http://aviationweather.gov/adds/dataserver_current/httpparam?dataSource=tafs&requestType=retrieve&mostRecent=true&format=xml&hoursBeforeNow=3&stationString=" +
                    this.IcaoIdenticator);
            var xmlResponse = await HttpGetAsync(uri);
            if (xmlResponse != null)
            {
                using (var reader = XmlReader.Create(xmlResponse))
                {
                    var ser = new XmlSerializer(typeof(response));
                    var response = (response)ser.Deserialize(reader);
                    if (response.data != null)
                    {
                        this.Taf = response.data.TAF;
                    }
                    else
                    {
                        this.Taf = new TAF[0];
                    }
                }
            }
        }

        public static async Task<IEnumerable<TAF>> GetTafsAsync(string icaoIdenticator)
        {
            Debug.Assert(!string.IsNullOrEmpty(icaoIdenticator));

            // Request the most recent TAF from three hours ago, based on issue time. Only one TAF is returned.
            // See http://aviationweather.gov/dataserver/example?datatype=taf
            var uri =
                new Uri(
                    "http://aviationweather.gov/adds/dataserver_current/httpparam?dataSource=tafs&requestType=retrieve&mostRecent=true&format=xml&hoursBeforeNow=3&stationString=" +
                    icaoIdenticator);
            var xmlResponse = await HttpGetAsync(uri);
            if (xmlResponse != null)
            {
                using (var reader = XmlReader.Create(xmlResponse))
                {
                    var ser = new XmlSerializer(typeof(response));
                    var response = (response)ser.Deserialize(reader);
                    if (response.data != null)
                    {
                        return response.data.TAF;
                    }
                }
            }

            return new TAF[0];
        }

        public static async Task<METAR[]> GetMetarsAsync(string spaceSeparaedListOfIcaoStations)
        {
            Debug.Assert(!string.IsNullOrEmpty(spaceSeparaedListOfIcaoStations));

            // Of all METARS, obtain the single most recent METAR from the past three hours. Only one METAR per station is returned.

            var uri =
                new Uri(
                    "http://aviationweather.gov/adds/dataserver_current/httpparam?dataSource=metars&requestType=retrieve&mostRecentForEachStation=constraint&format=xml&hoursBeforeNow=3&stationString=" +
                    spaceSeparaedListOfIcaoStations);
            using (var xmlResponse = await HttpGetAsync(uri))
            {
                if (xmlResponse != null)
                {
                    var reader = XmlReader.Create(xmlResponse);
                    try
                    {
                        var ser = new XmlSerializer(typeof(metar.response));
                        var response = (metar.response)ser.Deserialize(reader);
                        if (response.data != null && response.data.METAR != null)
                        {
                            return response.data.METAR;
                        }
                    }
                    catch(Exception)
                    {
                        return new METAR[0];
                    }
                }
            }

            return new METAR[0];
        }

        private async void GetMetarAsync()
        {
            if (this.HasMetarStation)
            {
                var uri =
                    new Uri(
                        "http://aviationweather.gov/adds/dataserver_current/httpparam?dataSource=metars&requestType=retrieve&mostRecentForEachStation=constraint&format=xml&hoursBeforeNow=2&stationString=" +
                        this.IcaoIdenticator);
                using (var xmlResponse = await HttpGetAsync(uri))
                {
                    if (xmlResponse != null)
                    {
                        var reader = XmlReader.Create(xmlResponse);
                        var ser = new XmlSerializer(typeof(metar.response));
                        var response = (metar.response)ser.Deserialize(reader);
                        if (response.data != null && response.data.METAR != null && response.data.METAR.Length > 0)
                        {
                            this.Metar = response.data.METAR[0];
                        }
                    }
                }
            }
        }

        public async void GetForecastDiscussionAsync()
        {
            double closestDistance = 9999;
            var closestWeatherUnit = new Tuple<string, BasicGeoposition>(
                string.Empty,
                new BasicGeoposition { Latitude = 0, Longitude = 0 });
            foreach (var loc in weatherForecastAreaMap)
            {
                var distance = loc.Item2.DistanceTo(this.Location);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWeatherUnit = loc;
                }
            }

            var uri = new Uri("http://aviationweather.gov/fcstdisc/data?cwa=" + closestWeatherUnit.Item1);
            var response = await HttpGetAsync(uri);
            if (response != null)
            {
                using (var reader = new StreamReader(response))
                {
                    var html = reader.ReadToEnd();
                    const string StartToken = "<!-- raw data starts -->";
                    const string EndToken = "<!-- raw data ends -->";
                    var start = html.IndexOf(StartToken);
                    if (start != -1)
                    {
                        var end = html.IndexOf(EndToken, start + StartToken.Length, StringComparison.Ordinal);
                        if (end != -1)
                        {
                            var fa = html.Substring(start + 1 + StartToken.Length, end - start - StartToken.Length - 1);
                            fa = fa.Replace("\n    ", " ");
                            this.AreaForecastDiscussion = fa;
                        }
                    }
                }
            }
        }

        public async void GetNotamsAsync()
        {
            var uri =
                new Uri(
                    "https://notams.aim.faa.gov/notamSearch/search?" +
                    "searchType=0&designatorForAccountable=&latDegrees=&latMinutes=0&latSeconds=0&longDegrees=&longMinutes=0&longSeconds=0&radius=10&sortColumns=5+false&sortDirection=true&designatorForNotamNumberSearch=&notamNumber=&radiusSearchOnDesignator=false&radiusSearchDesignator=&latitudeDirection=N&longitudeDirection=W&freeFormText=&flightPathText=&flightPathDivertAirfields=&flightPathBuffer=4&flightPathIncludeNavaids=true&flightPathIncludeArtcc=false&flightPathIncludeTfr=true&flightPathIncludeRegulatory=false&flightPathResultsType=All+NOTAMs&archiveDate=2014-12-17&archiveDesignator=&offset=0&notamsOnly=false&filters=&designatorsForLocation=" +
                    this.Designator);
            Debug.WriteLine(uri);
            var response = string.Empty;
            var task = await HttpPostAsync(uri);
            if (task == null)
            {
                return;
            }

            using (var r = new StreamReader(task))
            {
                response = r.ReadToEnd();
            }

            if (string.IsNullOrEmpty(response))
            {
                return;
            }

            dynamic notamList = JObject.Parse(response);
            var notamCount = (int)notamList["totalNotamCount"];
            var idregex = new Regex(@"\d{10}-\d{10}", RegexOptions.Singleline);
            var dateRegex = new Regex(@"\d{2}/\d{2}/\d{4} \d{2}\d{2}", RegexOptions.Singleline);
            var dateFormat = "MM/dd/yy h:mmt";
            foreach (var notam in notamList["notamList"])
            {
                var n = new Notam();
                try
                {
                    string date;
                    var utc = DateTime.MinValue;
                    n.Designator = ((object)notam["facilityDesignator"]).ToString();
                    date = ((object)notam["startDate"]).ToString();
                    var match = dateRegex.Match(date);
                    if (match.Success)
                    {
                        utc = DateTime.ParseExact(
                            date.Substring(0, match.Length),
                            "MM/dd/yyyy HHmm",
                            CultureInfo.InvariantCulture);
                        n.Start = utc.ToLocalTime().ToString(dateFormat);
                    }
                    else
                    {
                        n.Start = date;
                    }

                    date = ((object)notam["endDate"]).ToString();
                    match = dateRegex.Match(date);
                    if (match.Success)
                    {
                        utc = DateTime.ParseExact(
                            date.Substring(0, match.Length),
                            "MM/dd/yyyy HHmm",
                            CultureInfo.InvariantCulture);
                        n.End = utc.ToLocalTime().ToString(dateFormat);
                    }
                    else
                    {
                        n.End = date;
                    }

                    n.Airport = ((object)notam["airportName"]).ToString();
                    var msg = ((object)notam["traditionalMessage"]).ToString();
                    if (string.IsNullOrWhiteSpace(msg))
                    {
                        msg = ((object)notam["traditionalMessageFrom4thWord"]).ToString();
                    }

                    match = idregex.Match(msg);
                    if (match.Success)
                    {
                        msg = msg.Substring(0, match.Index);
                    }

                    n.Message = msg;
                    n.Active = (((object)notam["status"]).ToString()) == "Active" ? true : false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                if (n.Active)
                {
                    this.notams.Add(n);
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0} ({1}) {2}, {3} {4}ft Var={5} Ownership={6}",
                this.Designator,
                this.Type,
                this.City,
                this.State,
                this.FieldElevation,
                this.MagneticVariation,
                this.Ownership);
        }

        public double Distance(Airport to)
        {
            return this.Location.DistanceTo(to.Location);
        }

        private static async Task<Stream> HttpGetAsync(Uri url)
        {
            var timer = Stopwatch.StartNew();
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            httpClient.DefaultRequestHeaders.CacheControl = cacheControl;
            int retries = 0;
            const int MaxRetries = 2;
            while (true)
            {
                try
                {
                    var response = await httpClient.GetStreamAsync(url);
                    timer.Stop();
                    Debug.WriteLine("{0}ms for {1}", timer.ElapsedMilliseconds, url);
                    return response;
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries == MaxRetries)
                    {
                        Debug.WriteLine("Http GET of {0} failed with: {1}", url, ex.Message);
                        return default(Stream);
                    }

                    Debug.WriteLine("Http GET of {0} failed with: {1}. Retrying.", url, ex.Message);
                }
            }
        }

        private static async Task<Stream> HttpPostAsync(Uri url)
        {
            var timer = Stopwatch.StartNew();
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            int retries = 0;
            const int MaxRetries = 2;
            while (true)
            {
                try
                {
                    var response = await httpClient.PostAsync(url, new StringContent(string.Empty));
                    timer.Stop();
                    Debug.WriteLine("{0}ms for {1}", timer.ElapsedMilliseconds, url);
                    return await response.Content.ReadAsStreamAsync();
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries == MaxRetries)
                    {
                        Debug.WriteLine("Http GET of {0} failed with: {1}", url, ex.Message);
                        return default(Stream);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        private async Task GetLocalizedAviationModelOutputStatisticsAsync(string icao)
        {
            // See http://www.nws.noaa.gov/mdl/lamp/lamp_info.shtml
            var u = DateTime.UtcNow - TimeSpan.FromHours(1);
            string uri = string.Format("http://www.nws.noaa.gov/mdl/gfslamp/meteo/bullpop.php?sta={0}&forecast_time={1:D2}", icao.ToUpperInvariant(), u.Hour);
            var url = new Uri(uri);
            var html = await HttpGetAsync(url);
            if (html != null)
            {
                this.ParseLAMP(html);
            }
        }

        private void ParseLAMP(Stream htmlStream)
        {
            // See http://www.nws.noaa.gov/mdl/gfslamp/docs/LAMP_description.shtml
            const int ForecastCount = 25; // LAMP 
            for (var i = 0; i < ForecastCount; ++i)
            {
                this.mos.Add(new ModelOutputStatistic());
            }

            var reader = new StreamReader(htmlStream);
            var inTable = false;
            DateTime forecastTime = DateTime.UtcNow;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line.IndexOf(@"<PRE>", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    inTable = true;
                    var header = reader.ReadLine();
                    var match = this.dateRegex.Match(header);
                    if (match.Success)
                    {
                        DateTime.TryParseExact(match.Value, "M/d/yyyy  HHmm", new CultureInfo("en-US"), DateTimeStyles.AssumeUniversal, out forecastTime);
                    }
                }
                else if (line.IndexOf(@"</PRE>", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    break;
                }

                if (inTable)
                {
                    if (line.Length > 10)
                    {
                        var symbol = line.Substring(1, 3);
                        switch (symbol)
                        {
                            case "UTC":
                                var startHour = int.Parse(line.Substring(6, 2));
                                var u = DateTime.UtcNow - TimeSpan.FromHours(1);
                                var startTime = new DateTime(u.Year, u.Month, u.Day, 0, u.Minute, u.Second);
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    this.mos[i].ForecastTime = startTime.AddHours(startHour + i).ToLocalTime();
                                }

                                break;
                            case "TMP":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    int parsedValue;
                                    if (int.TryParse(sym, out parsedValue))
                                    {
                                        this.mos[i].Temperature = parsedValue;
                                    }
                                }
                                break;
                            case "DPT":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    int parsedValue;
                                    if (int.TryParse(sym, out parsedValue))
                                    {
                                        this.mos[i].DewPoint = parsedValue;
                                    }
                                }
                                break;
                            case "CLD":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    switch (sym)
                                    {
                                        case "CL":
                                            this.mos[i].CloudCoverage = "CLR";
                                            break;
                                        case "FW":
                                            this.mos[i].CloudCoverage = "FEW";
                                            break;
                                        case "SC":
                                            this.mos[i].CloudCoverage = "SCT";
                                            break;
                                        case "BK":
                                            this.mos[i].CloudCoverage = "BKN";
                                            break;
                                        case "OV":
                                            this.mos[i].CloudCoverage = "OVC";
                                            break;
                                        default:
                                            this.mos[i].CloudCoverage = string.Empty;
                                            break;
                                    }
                                }

                                break;
                            case "WDR":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    int parsedValue;
                                    if (int.TryParse(sym, out parsedValue))
                                    {
                                        if (parsedValue != 99)
                                        {
                                            var dir = parsedValue * 10 - (int)this.MagneticVariation;
                                            if (dir >= 360)
                                            {
                                                dir = dir - 360;
                                            }
                                            else if (dir < 0)
                                            {
                                                dir = dir + 360;
                                            }

                                            this.mos[i].WindDirection = dir;
                                        }
                                        else
                                        {
                                            this.mos[i].WindDirection = parsedValue * 10;
                                        }
                                    }
                                }
                                break;
                            case "WSP":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    int parsedValue;
                                    if (int.TryParse(sym, out parsedValue))
                                    {
                                        this.mos[i].WindSpeed = parsedValue;
                                    }
                                }
                                break;
                            case "WGS":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    if (sym == "NG")
                                    {
                                        this.mos[i].Gust = 0;
                                    }
                                    else
                                    {
                                        int parsedValue;
                                        if (int.TryParse(sym, out parsedValue))
                                        {
                                            this.mos[i].Gust = parsedValue;
                                        }
                                    }
                                }
                                break;
                            case "PPO":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    int parsedValue;
                                    if (int.TryParse(sym, out parsedValue))
                                    {
                                        this.mos[i].PrecipationProbability = parsedValue;
                                    }
                                }
                                break;
                            case "CIG":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    int parsedValue;
                                    if (int.TryParse(sym, out parsedValue))
                                    {
                                        this.mos[i].Ceiling = parsedValue;
                                    }
                                }
                                break;
                            case "VIS":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    int parsedValue;
                                    if (int.TryParse(sym, out parsedValue))
                                    {
                                        this.mos[i].Visibility = parsedValue;
                                    }
                                }
                                break;
                            case "OBV":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    if (sym == "N")
                                    {
                                        sym = string.Empty;
                                    }

                                    this.mos[i].ObstructionOfVision = sym;
                                }

                                break;

                            case "TYP":

                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    if (this.mos[i].PrecipationProbability > 35)
                                    {
                                        var sym = line.Substring(5 + i * 3, 3).Trim();
                                        if (sym == "R")
                                        {
                                            this.mos[i].Precipitation = "Rain";
                                        }
                                        else if (sym == "S")
                                        {
                                            this.mos[i].Precipitation = "Snow";
                                        }
                                        else if (sym == "Z")
                                        {
                                            this.mos[i].Precipitation = "FZ Rain";
                                        }
                                    }
                                }

                                break;

                            case "CC2":
                                for (var i = 0; i < this.mos.Count; ++i)
                                {
                                    var sym = line.Substring(5 + i * 3, 3).Trim();
                                    if (sym == "N")
                                    {
                                        sym = string.Empty;
                                    }
                                    else if (sym == "L")
                                    {
                                        sym = "low";
                                    }
                                    else if (sym == "M")
                                    {
                                        sym = "med";
                                    }
                                    else if (sym == "H")
                                    {
                                        sym = "high";
                                    }

                                    this.mos[i].Convection = sym;
                                }

                                break;
                        }
                    }
                }
            }
        }
    }
}
