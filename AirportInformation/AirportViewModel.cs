//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AirportViewModel.cs" company="Microsoft">
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
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;
    using Windows.ApplicationModel.Core;
    using Windows.Devices.Geolocation;
    using Windows.Storage;
    using Windows.UI.Core;
    using aircraftreport;
    using airsigmet;
    using Common;
    using response = aircraftreport.response;

    public class AirportViewModel : INotifyPropertyChanged
    {
        private const int RadiusInNauticalMiles = 60;
        private const int MaxAltitude = 17999;
        private const string AwcBaseUrl = "http://aviationweather.gov/adds/dataserver_current/httpparam";
        public const int MinimumRunwayLengthInFeet = 1001;
        private readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(1);
        private const int PirepsMinutesAgo = 90;
        private const int AirSigmetsMinutesAgo = 90;
        private static object refreshLock = new object();
        private readonly AirportDatabase db = new AirportDatabase();
        private readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);
        private Timer updateTimer = null;
        private readonly Geolocator geoLocator = new Geolocator
        {
            DesiredAccuracy = PositionAccuracy.Default,
            MovementThreshold = 1000
        };

        private readonly ObservableCollection<AircraftReport> wxAircraftReports =
            new ObservableCollection<AircraftReport>();

        private ObservableCollection<Airport> airportsNearby = null;
        private ObservableCollection<Airport> airportsBookmarked = null;
        private ObservableCollection<AIRSIGMET> airsigmets = new ObservableCollection<AIRSIGMET>();
        private BasicGeoposition geoPosition;
        private ObservableCollection<AircraftReport> icingAircraftReports = new ObservableCollection<AircraftReport>();
        private DateTime lastAircraftReportQuery = DateTime.MinValue;
        private DateTime lastAirSigmetQuery = DateTime.MinValue;
        private ObservableCollection<AircraftReport> turbulenceAircraftReports =
            new ObservableCollection<AircraftReport>();

        private ObservableCollection<string> wxImageList = new ObservableCollection<string>
        {
            "http://www.wpc.ncep.noaa.gov/noaa/noaa.gif",
            "http://weather.rap.ucar.edu/progs/prog00hr.gif",
            "http://weather.rap.ucar.edu/progs/prog12hr.gif",
            "http://weather.rap.ucar.edu/progs/prog24hr.gif",
            "http://weather.rap.ucar.edu/progs/prog36hr.gif",
            "http://weather.unisys.com/satellite/sat_ir_enh_us.gif",
            "http://aviationweather.gov/adds/data/satellite/latest_US_vis.jpg",
            "http://weather.unisys.com/radar/wrad_us.gif",
            "http://weather.rap.ucar.edu/model/ruc03hr_sfc_prcp.gif",
            "http://weather.rap.ucar.edu/model/ruc12hr_850_wnd.gif",
            "http://weather.rap.ucar.edu/model/ruc12hr_700_wnd.gif",
            "http://weather.rap.ucar.edu/model/ruc12hr_500_wnd.gif",
            "http://weather.rap.ucar.edu/model/ruc03hr_sfc_sreh.gif"
        };

        private readonly List<Tuple<BasicGeoposition, string>> weatherRegions = new List<Tuple<BasicGeoposition, string>>
        {
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 46.40, Longitude = -117.00}, "LWS"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 40.97, Longitude = -117.74}, "WMC"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 36.17, Longitude = -115.14}, "LAS"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 44.53, Longitude = -109.06}, "COD"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 39.74, Longitude = -104.99}, "DEN"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 35.11, Longitude = -106.61}, "ABQ"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 44.37, Longitude = -100.35}, "PIR"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 37.69, Longitude =  -97.34}, "ICT"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 30.27, Longitude =  -97.74}, "AUS"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 44.98, Longitude =  -93.27}, "MSP"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 34.75, Longitude =  -92.29}, "LIT"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 42.33, Longitude =  -83.05}, "DTW"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 37.97, Longitude =  -87.57}, "EVV"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 32.37, Longitude =  -86.30}, "MGM"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 42.65, Longitude =  -73.76}, "ALB"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 39.30, Longitude =  -76.61}, "BWI"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 35.23, Longitude =  -80.84}, "CLT"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 27.95, Longitude =  -82.46}, "TPA"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 56.96, Longitude = -149.82}, "AK"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 21.31, Longitude = -157.86}, "HI"),
        };

        private readonly List<Tuple<BasicGeoposition, string>> geosRegions = new List<Tuple<BasicGeoposition, string>>
        {
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 40.98, Longitude = -111.04}, "W"),
            new Tuple<BasicGeoposition, string>( new BasicGeoposition{Latitude = 38.07, Longitude = -90.18}, "E")
        };

        private readonly Dictionary<string, string> areaForecastRegions = new Dictionary<string, string>
        {
            {"AK", "akcentral"},
            {"WA", "sfo"},
            {"OR", "sfo"},
            {"CA", "sfo"},

            {"ID", "slc"},
            {"MT", "slc"},
            {"WY", "slc"},
            {"NV", "slc"},
            {"UT", "slc"},
            {"CO", "slc"},
            {"AZ", "slc"},
            {"NM", "slc"},

            {"ND", "chi"},
            {"SD", "chi"},
            {"NE", "chi"},
            {"KS", "chi"},
            {"MN", "chi"},
            {"IA", "chi"},
            {"MO", "chi"},
            {"WI", "chi"},
            {"LM", "chi"},
            {"LS", "chi"},
            {"MI", "chi"},
            {"LH", "chi"},
            {"IL", "chi"},
            {"IN", "chi"},
            {"KY", "chi"},

            {"OK", "dfw"},
            {"TX", "dfw"},
            {"AR", "dfw"},
            {"TN", "dfw"},
            {"LA", "dfw"},
            {"MS", "dfw"},
            {"AL", "dfw"},

            {"ME", "bos"},
            {"NH", "bos"},
            {"VT", "bos"},
            {"MA", "bos"},
            {"RI", "bos"},
            {"CT", "bos"},
            {"NY", "bos"},
            {"LO", "bos"},
            {"NJ", "bos"},
            {"PA", "bos"},
            {"OH", "bos"},
            {"LE", "bos"},
            {"WV", "bos"},
            {"MD", "bos"},
            {"DC", "bos"},
            {"DE", "bos"},
            {"VA", "bos"},

            {"NC", "mia"},
            {"SC", "mia"},
            {"GA", "mia"},
            {"FL", "mia"},

            {"HI", "hawaii"},
        };

        /*
        private readonly Dictionary<string, WindsAloft> windsAloftStationMap = new Dictionary<string, WindsAloft>
        {
            {"ABI", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.41, Longitude = -99.68 }}},
            {"ABQ", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.04, Longitude = -106.61}}},
            {"ABR", new WindsAloft { Location = new BasicGeoposition{Latitude = 45.45, Longitude = -98.42}}},
            {"ACK", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.25, Longitude = -70.06}}},
            {"ACY", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.46, Longitude = -74.58}}},
            {"AGC", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.35, Longitude = -79.93}}},
            {"ALB", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.75, Longitude = -73.80}}},
            {"ALS", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.44, Longitude = -105.87}}},
            {"AMA", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.22, Longitude = -101.71}}},
            {"AST", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.16, Longitude = -123.88}}},
            {"ATL", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.64, Longitude = -84.43}}},
            {"AVP", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.34, Longitude = -75.72}}},
            {"AXN", new WindsAloft { Location = new BasicGeoposition{Latitude = 45.87, Longitude = -95.39}}},
            {"BAM", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.60, Longitude = -116.87}}},
            {"BCE", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.71, Longitude = -112.15}}},
            {"BDL", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.94, Longitude = -72.68}}},
            {"BFF", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.87, Longitude = -103.60}}},
            {"BGR", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.81, Longitude = -68.83}}},
            {"BHM", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.56, Longitude = -86.75}}},
            {"BIH", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.37, Longitude = -118.36}}},
            {"BIL", new WindsAloft { Location = new BasicGeoposition{Latitude = 45.81, Longitude = -108.54}}},
            {"BLH", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.62, Longitude = -114.72}}},
            {"BML", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.58, Longitude = -71.18}}},
            {"BNA", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.12, Longitude = -86.68}}},
            {"BOI", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.56, Longitude = -116.22}}},
            {"BOS", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.36, Longitude = -71.01}}},
            {"BRL", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.78, Longitude = -91.13}}},
            {"BRO", new WindsAloft { Location = new BasicGeoposition{Latitude = 25.91, Longitude = -97.43}}},
            {"BUF", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.94, Longitude = -78.73}}},
            {"CAE", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.94, Longitude = -81.12}}},
            {"CAR", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.87, Longitude = -68.02}}},
            {"CGI", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.23, Longitude = -89.57}}},
            {"CHS", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.90, Longitude = -80.04}}},
            {"CLE", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.41, Longitude = -81.85}}},
            {"CLL", new WindsAloft { Location = new BasicGeoposition{Latitude = 30.59, Longitude = -96.36}}},
            {"CMH", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.00, Longitude = -82.89}}},
            {"COU", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.82, Longitude = -92.22}}},
            {"CRP", new WindsAloft { Location = new BasicGeoposition{Latitude = 27.77, Longitude = -97.50}}},
            {"CRW", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.38, Longitude = -81.59}}},
            {"CSG", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.52, Longitude = -84.94}}},
            {"CVG", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.05, Longitude = -84.67}}},
            {"CZI", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.00, Longitude = -106.44}}},
            {"DAL", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.85, Longitude = -96.85}}},
            {"DBQ", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.40, Longitude = -90.71}}},
            {"DEN", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.86, Longitude = -104.67}}},
            {"DIK", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.80, Longitude = -102.80}}},
            {"DLH", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.84, Longitude = -92.19}}},
            {"DLN", new WindsAloft { Location = new BasicGeoposition{Latitude = 45.26, Longitude = -112.55}}},
            {"DRT", new WindsAloft { Location = new BasicGeoposition{Latitude = 29.37, Longitude = -100.93}}},
            {"DSM", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.53, Longitude = -93.66}}},
            {"ECK", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.26, Longitude = -82.72}}},
            {"EKN", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.89, Longitude = -79.86}}},
            {"ELP", new WindsAloft { Location = new BasicGeoposition{Latitude = 31.81, Longitude = -106.38}}},
            {"ELY", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.30, Longitude = -114.84}}},
            {"EMI", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.50, Longitude = -76.98}}},
            {"EVV", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.04, Longitude = -87.53}}},
            {"EYW", new WindsAloft { Location = new BasicGeoposition{Latitude = 24.56, Longitude = -81.76}}},
            {"FAT", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.78, Longitude = -119.72}}},
            {"GPI", new WindsAloft { Location = new BasicGeoposition{Latitude = 48.31, Longitude = -114.26}}},
            {"FLO", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.19, Longitude = -79.72}}},
            {"FMN", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.74, Longitude = -108.23}}},
            {"FOT", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.55, Longitude = -124.13}}},
            {"FSD", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.58, Longitude = -96.74}}},
            {"FSM", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.34, Longitude = -94.37}}},
            {"FWA", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.98, Longitude = -85.20}}},
            {"GAG", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.30, Longitude = -99.78}}},
            {"GCK", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.93, Longitude = -100.72}}},
            {"GEG", new WindsAloft { Location = new BasicGeoposition{Latitude = 47.62, Longitude = -117.54}}},
            {"GFK", new WindsAloft { Location = new BasicGeoposition{Latitude = 47.95, Longitude = -97.17}}},
            {"GGW", new WindsAloft { Location = new BasicGeoposition{Latitude = 48.21, Longitude = -106.61}}},
            {"GJT", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.12, Longitude = -108.53}}},
            {"GLD", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.37, Longitude = -101.70}}},
            {"GRB", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.48, Longitude = -88.13}}},
            {"GRI", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.97, Longitude = -98.31}}},
            {"GSP", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.90, Longitude = -82.22}}},
            {"GTF", new WindsAloft { Location = new BasicGeoposition{Latitude = 47.48, Longitude = -111.37}}},
            {"HOU", new WindsAloft { Location = new BasicGeoposition{Latitude = 29.65, Longitude = -95.28}}},
            {"HSV", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.64, Longitude = -86.78}}},
            {"ICT", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.65, Longitude = -97.43}}},
            {"ILM", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.27, Longitude = -77.90}}},
            {"IMB", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.65, Longitude = -119.71}}},
            {"IND", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.72, Longitude = -86.29}}},
            {"INK", new WindsAloft { Location = new BasicGeoposition{Latitude = 31.78, Longitude = -103.20}}},
            {"INL", new WindsAloft { Location = new BasicGeoposition{Latitude = 48.57, Longitude = -93.40}}},
            {"JAN", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.31, Longitude = -90.08}}},
            {"JAX", new WindsAloft { Location = new BasicGeoposition{Latitude = 30.49, Longitude = -81.69}}},
            {"JFK", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.64, Longitude = -73.78}}},
            {"JOT", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.52, Longitude = -88.18}}},
            {"LAS", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.08, Longitude = -115.15}}},
            {"LBB", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.66, Longitude = -101.82}}},
            {"LCH", new WindsAloft { Location = new BasicGeoposition{Latitude = 30.13, Longitude = -93.22}}},
            {"LIT", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.73, Longitude = -92.22}}},
            {"LKV", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.16, Longitude = -120.40}}},
            {"LND", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.82, Longitude = -108.73}}},
            {"LOU", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.23, Longitude = -85.66}}},
            {"LRD", new WindsAloft { Location = new BasicGeoposition{Latitude = 27.54, Longitude = -99.46}}},
            {"LSE", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.88, Longitude = -91.26}}},
            {"LWS", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.37, Longitude = -117.02}}},
            {"MBW", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.85, Longitude = -106.00}}},
            {"MCW", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.16, Longitude = -93.33}}},
            {"MEM", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.04, Longitude = -89.98}}},
            {"MGM", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.30, Longitude = -86.39}}},
            {"MIA", new WindsAloft { Location = new BasicGeoposition{Latitude = 25.80, Longitude = -80.29}}},
            {"MKC", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.12, Longitude = -94.59}}},
            {"MKG", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.17, Longitude = -86.24}}},
            {"MLB", new WindsAloft { Location = new BasicGeoposition{Latitude = 28.10, Longitude = -80.65}}},
            {"MLS", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.43, Longitude = -105.89}}},
            {"MOB", new WindsAloft { Location = new BasicGeoposition{Latitude = 30.69, Longitude = -88.24}}},
            {"MOT", new WindsAloft { Location = new BasicGeoposition{Latitude = 48.26, Longitude = -101.28}}},
            {"MRF", new WindsAloft { Location = new BasicGeoposition{Latitude = 30.37, Longitude = -104.02}}},
            {"MSP", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.88, Longitude = -93.22}}},
            {"MSY", new WindsAloft { Location = new BasicGeoposition{Latitude = 29.99, Longitude = -90.26}}},
            {"OKC", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.39, Longitude = -97.60}}},
            {"OMA", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.30, Longitude = -95.89}}},
            {"ONL", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.47, Longitude = -98.69}}},
            {"ONT", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.06, Longitude = -117.60}}},
            {"ORF", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.89, Longitude = -76.20}}},
            {"OTH", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.42, Longitude = -124.25}}},
            {"PDX", new WindsAloft { Location = new BasicGeoposition{Latitude = 45.59, Longitude = -122.60}}},
            {"PHX", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.43, Longitude = -112.01}}},
            {"PIE", new WindsAloft { Location = new BasicGeoposition{Latitude = 27.91, Longitude = -82.69}}},
            {"PIH", new WindsAloft { Location = new BasicGeoposition{Latitude = 42.91, Longitude = -112.60}}},
            {"PIR", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.38, Longitude = -100.29}}},
            {"PLB", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.69, Longitude = -73.53}}},
            {"PRC", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.65, Longitude = -112.42}}},
            {"PSB", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.88, Longitude = -78.09}}},
            {"PSX", new WindsAloft { Location = new BasicGeoposition{Latitude = 28.73, Longitude = -96.25}}},
            {"PUB", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.29, Longitude = -104.50}}},
            {"PWM", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.65, Longitude = -70.31}}},
            {"RAP", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.05, Longitude = -103.06}}},
            {"RBL", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.15, Longitude = -122.25}}},
            {"RDM", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.25, Longitude = -121.15}}},
            {"RDU", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.88, Longitude = -78.79}}},
            {"RIC", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.51, Longitude = -77.32}}},
            {"RKS", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.59, Longitude = -109.07}}},
            {"RNO", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.50, Longitude = -119.77}}},
            {"ROA", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.33, Longitude = -79.98}}},
            {"ROW", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.30, Longitude = -104.53}}},
            {"SAC", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.51, Longitude = -121.49}}},
            {"SAN", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.73, Longitude = -117.19}}},
            {"SAT", new WindsAloft { Location = new BasicGeoposition{Latitude = 29.53, Longitude = -98.47}}},
            {"SAV", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.13, Longitude = -81.20}}},
            {"SBA", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.43, Longitude = -119.84}}},
            {"SEA", new WindsAloft { Location = new BasicGeoposition{Latitude = 47.45, Longitude = -122.31}}},
            {"SFO", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.62, Longitude = -122.38}}},
            {"SGF", new WindsAloft { Location = new BasicGeoposition{Latitude = 37.25, Longitude = -93.39}}},
            {"SHV", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.45, Longitude = -93.83}}},
            {"SIY", new WindsAloft { Location = new BasicGeoposition{Latitude = 41.78, Longitude = -122.47}}},
            {"SLC", new WindsAloft { Location = new BasicGeoposition{Latitude = 40.79, Longitude = -111.98}}},
            {"SLN", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.79, Longitude = -97.65}}},
            {"SPI", new WindsAloft { Location = new BasicGeoposition{Latitude = 39.84, Longitude = -89.68}}},
            {"SPS", new WindsAloft { Location = new BasicGeoposition{Latitude = 33.99, Longitude = -98.49}}},
            {"SSM", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.41, Longitude = -84.31}}},
            {"STL", new WindsAloft { Location = new BasicGeoposition{Latitude = 38.75, Longitude = -90.37}}},
            {"SYR", new WindsAloft { Location = new BasicGeoposition{Latitude = 43.11, Longitude = -76.11}}},
            {"TCC", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.18, Longitude = -103.60}}},
            {"TLH", new WindsAloft { Location = new BasicGeoposition{Latitude = 30.40, Longitude = -84.35}}},
            {"TRI", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.48, Longitude = -82.41}}},
            {"TUL", new WindsAloft { Location = new BasicGeoposition{Latitude = 36.20, Longitude = -95.89}}},
            {"TUS", new WindsAloft { Location = new BasicGeoposition{Latitude = 32.12, Longitude = -110.94}}},
            {"TVC", new WindsAloft { Location = new BasicGeoposition{Latitude = 44.74, Longitude = -85.58}}},
            {"TYS", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.81, Longitude = -83.99}}},
            {"WJF", new WindsAloft { Location = new BasicGeoposition{Latitude = 34.74, Longitude = -118.22}}},
            {"YKM", new WindsAloft { Location = new BasicGeoposition{Latitude = 46.57, Longitude = -120.54}}},
            {"ZUN", new WindsAloft { Location = new BasicGeoposition{Latitude = 35.08, Longitude = -108.79}}},
            {"BET", new WindsAloft { Location = new BasicGeoposition{Latitude = 60.78, Longitude = -161.84}}},
            {"BRW", new WindsAloft { Location = new BasicGeoposition{Latitude = 71.28, Longitude = -156.77}}},
            {"BTI", new WindsAloft { Location = new BasicGeoposition{Latitude = 70.13, Longitude = -143.58}}},
            {"BTT", new WindsAloft { Location = new BasicGeoposition{Latitude = 66.91, Longitude = -151.53}}},
            {"CDB", new WindsAloft { Location = new BasicGeoposition{Latitude = 55.21, Longitude = -162.72}}},
            {"CZF", new WindsAloft { Location = new BasicGeoposition{Latitude = 61.78, Longitude = -166.04}}},
            {"EHM", new WindsAloft { Location = new BasicGeoposition{Latitude = 58.65, Longitude = -162.06}}},
            {"FAI", new WindsAloft { Location = new BasicGeoposition{Latitude = 64.82, Longitude = -147.86}}},
            {"FYU", new WindsAloft { Location = new BasicGeoposition{Latitude = 66.57, Longitude = -145.25}}},
            {"GAL", new WindsAloft { Location = new BasicGeoposition{Latitude = 64.74, Longitude = -156.94}}},
            {"GKN", new WindsAloft { Location = new BasicGeoposition{Latitude = 62.15, Longitude = -145.45}}},
            {"HOM", new WindsAloft { Location = new BasicGeoposition{Latitude = 59.65, Longitude = -151.48}}},
            {"JNU", new WindsAloft { Location = new BasicGeoposition{Latitude = 58.35, Longitude = -134.57}}},
            {"LUR", new WindsAloft { Location = new BasicGeoposition{Latitude = 68.88, Longitude = -166.11}}},
            {"MCG", new WindsAloft { Location = new BasicGeoposition{Latitude = 62.95, Longitude = -155.61}}},
            {"MDO", new WindsAloft { Location = new BasicGeoposition{Latitude = 59.45, Longitude = -146.31}}},
            {"OME", new WindsAloft { Location = new BasicGeoposition{Latitude = 64.51, Longitude = -165.45}}},
            {"ORT", new WindsAloft { Location = new BasicGeoposition{Latitude = 62.96, Longitude = -141.93}}},
            {"OTZ", new WindsAloft { Location = new BasicGeoposition{Latitude = 66.88, Longitude = -162.60}}},
            {"SNP", new WindsAloft { Location = new BasicGeoposition{Latitude = 57.17, Longitude = -170.22}}},
            {"TKA", new WindsAloft { Location = new BasicGeoposition{Latitude = 62.32, Longitude = -150.09}}},
            {"UNK", new WindsAloft { Location = new BasicGeoposition{Latitude = 63.89, Longitude = -160.80}}},
            {"YAK", new WindsAloft { Location = new BasicGeoposition{Latitude = 59.50, Longitude = -139.66}}},
            {"IKO", new WindsAloft { Location = new BasicGeoposition{Latitude = 52.94, Longitude = -168.85}}},
            {"AFM", new WindsAloft { Location = new BasicGeoposition{Latitude = 67.11, Longitude = -157.86}}}
        };
        */

        public AirportViewModel()
        {
            var timer = Stopwatch.StartNew();
            this.db.Initialize();
            Debug.WriteLine("Initializing database in {0}ms.", timer.ElapsedMilliseconds);
            this.wxImageList.Insert(7, string.Format("http://www.aviationweather.gov/data/obs/sat/goes/vis_goes{0}.jpg", GetClosestGeosRegionIdentifier(this.MyLocation)));
            this.geoLocator.PositionChanged += this.OnPositionChanged;
            this.updateTimer = new Timer(this.UpdateTimerCallback, null, this.UpdateInterval, this.UpdateInterval);
        }

        public BasicGeoposition MyLocation
        {
            get
            {
                if (this.geoPosition.Latitude != 0 || this.geoPosition.Longitude != 0)
                {
                    return this.geoPosition;
                }
                var pos = (string)ApplicationData.Current.LocalSettings.Values["LastPosition"];
                if (!string.IsNullOrEmpty(pos))
                {
                    var loc = pos.Split(',');
                    return new BasicGeoposition { Latitude = double.Parse(loc[0]), Longitude = double.Parse(loc[1]) };
                }

                return new BasicGeoposition { Latitude = 0, Longitude = 0 };
            }
        }

        public string AreaForecast
        {
            get { return string.Empty; }
        }

        public ObservableCollection<Airport> AirportsBookmarked
        {
            get
            {
                if (this.airportsBookmarked == null)
                {
                    this.airportsBookmarked = new ObservableCollection<Airport>();
                    this.LoadBookmarkedAirportsAsync();
                }

                return this.airportsBookmarked;
            }

        }

        public ObservableCollection<Airport> AirportsNearby
        {
            get
            {
                if (this.airportsNearby == null || this.airportsNearby.Count == 0)
                {
                    this.airportsNearby = new ObservableCollection<Airport>();
                }

                this.LoadAirportsNearbyAsync(MinimumRunwayLengthInFeet);
                return this.airportsNearby;
            }
        }

        public ObservableCollection<AircraftReport> WxPireps
        {
            get
            {
                if (DateTime.UtcNow - this.lastAircraftReportQuery > this.FiveMinutes)
                {
                    this.GetAircraftReportsAsync(PirepsMinutesAgo);
                    this.lastAircraftReportQuery = DateTime.UtcNow;
                }

                return this.wxAircraftReports;
            }

            set
            {
                this.icingAircraftReports = value;
                this.NotifyPropertyChanged("WxPireps");
            }
        }

        public ObservableCollection<AIRSIGMET> AirSigmets
        {
            get
            {
                if (DateTime.UtcNow - this.lastAirSigmetQuery > this.FiveMinutes)
                {
                    this.GetAirSigmetsAsync(AirSigmetsMinutesAgo);
                    this.lastAirSigmetQuery = DateTime.UtcNow;
                }

                return this.airsigmets;
            }

            set
            {
                this.airsigmets = value;
                this.NotifyPropertyChanged("AirSigmets");
            }
        }

        public ObservableCollection<AircraftReport> IcingPireps
        {
            get
            {
                if (DateTime.UtcNow - this.lastAircraftReportQuery > this.FiveMinutes)
                {
                    this.GetAircraftReportsAsync(PirepsMinutesAgo);
                    this.lastAircraftReportQuery = DateTime.UtcNow;
                }

                return this.icingAircraftReports;
            }

            set
            {
                this.icingAircraftReports = value;
                this.NotifyPropertyChanged("IcingPireps");
            }
        }

        public ObservableCollection<AircraftReport> TurbulencePireps
        {
            get
            {
                if (DateTime.UtcNow - this.lastAircraftReportQuery > this.FiveMinutes)
                {
                    this.GetAircraftReportsAsync(PirepsMinutesAgo);
                    this.lastAircraftReportQuery = DateTime.UtcNow;
                }

                return this.turbulenceAircraftReports;
            }

            set
            {
                this.turbulenceAircraftReports = value;
                this.NotifyPropertyChanged("TurbulencePireps");
            }
        }

        public ObservableCollection<string> WeatherImages
        {
            get { return this.wxImageList; }

            set
            {
                this.wxImageList = value;
                this.NotifyPropertyChanged("WeatherImages");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string GetClosestWeatherRegionIdentifier(BasicGeoposition location)
        {
            double closestDistance = 9999;
            string closest = null;
            foreach (var region in this.weatherRegions)
            {
                var distance = region.Item1.DistanceTo(location);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = region.Item2;
                }
            }

            return closest;
        }

        public string GetClosestGeosRegionIdentifier(BasicGeoposition location)
        {
            double closestDistance = 9999;
            string closest = null;
            foreach (var region in this.geosRegions)
            {
                var distance = region.Item1.DistanceTo(location);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = region.Item2;
                }
            }

            return closest;
        }

        private async void LoadAirportsNearbyAsync(int minimumRunwayLengthInFeet)
        {
            IEnumerable<Airport> nearest = null;
            try
            {
                if (Monitor.TryEnter(refreshLock))
                {
                    var pos = new BasicGeoposition
                    {
                        Latitude = this.MyLocation.Latitude,
                        Longitude = this.MyLocation.Longitude
                    };
                    var timer = Stopwatch.StartNew();
                    nearest = this.db.Nearby(pos, RadiusInNauticalMiles, minimumRunwayLengthInFeet).Take(70);
                    Debug.WriteLine("Found {0} nearest airports in {1}ms.", nearest.Count(), timer.ElapsedMilliseconds);
                    this.airportsNearby.Clear();
                    foreach (var airport in nearest)
                    {
                        this.airportsNearby.Add(airport);
                    }
                }
            }
            finally
            {
                Monitor.Exit(refreshLock);
            }

            await UpdateAirportsWithLatestMetarsAsync(nearest);
        }

        private async Task UpdateAirportsWithLatestMetarsAsync(IEnumerable<Airport> nearest)
        {
            try
            {
                if (Monitor.TryEnter(refreshLock))
                {
                    var airportsWithMetarStation = new StringBuilder();
                    foreach (var airport in nearest)
                    {
                        if (airport.HasMetarStation)
                        {
                            airportsWithMetarStation.Append(airport.IcaoIdenticator + " ");
                        }
                    }

                    if (airportsWithMetarStation.Length > 0)
                    {
                        var metars = await Airport.GetMetarsAsync(airportsWithMetarStation.ToString());
                        foreach (var metar in metars)
                        {
                            var airport = this.LookupAirportId(metar.station_id);
                            if (airport != null)
                            {
                                metar.dewpoint_c = metar.dewpoint_c;
                                airport.Metar = metar;
                            }
                        }
                    }
                }
            }
            finally
            {
                Monitor.Exit(refreshLock);
            }
        }

        private async void LoadBookmarkedAirportsAsync()
        {
            var bookmarks = App.GetSetting(App.BookmarksSettingKey);
            var airportsDesignators = bookmarks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var airportsWithMetarStation = new StringBuilder();
            foreach (var designator in airportsDesignators)
            {
                var airport = this.LookupAirportId(designator);
                if (airport != null)
                {
                    if (airport.HasMetarStation && airport.Metar == null)
                    {
                        airportsWithMetarStation.Append(airport.IcaoIdenticator + " ");
                    }

                    this.airportsBookmarked.Add(airport);
                }
            }

            if (airportsWithMetarStation.Length > 0)
            {
                var metars = await Airport.GetMetarsAsync(airportsWithMetarStation.ToString());
                if (metars != null)
                {
                    foreach (var metar in metars)
                    {
                        var airport = this.LookupAirportId(metar.station_id);
                        airport.Metar = metar;
                    }
                }
            }
        }

        public IEnumerable<Airport> GetAirportsNearby(BasicGeoposition pos, int radius, int minimumRunwayLengthInFeet)
        {
            return this.db.Nearby(pos, radius, minimumRunwayLengthInFeet);
        }

        public Airport LookupAirportId(string designator)
        {
            return this.db.LookupByDesignator(designator);
        }

        public List<Airport> LookupAirportByCity(string city)
        {
            return this.db.LookupByCity(city);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private async void GetAircraftReportsAsync(int minutesAgo)
        {
            var hours = (minutesAgo / 60).ToString();
            var uri =
                new Uri(string.Format("{0}?dataSource=aircraftreports&requestType=retrieve&format=xml&minAltitudeFt=0&maxAltitudeFt={1}&hoursBeforeNow={2}",
                    AwcBaseUrl, MaxAltitude, hours));
            var xmlResponse = await uri.HttpGetAsync();
            if (xmlResponse != null)
            {
                using (var reader = XmlReader.Create(xmlResponse))
                {
                    var ser = new XmlSerializer(typeof(response));
                    var response = (response)ser.Deserialize(reader);
                    if (response.data != null)
                    {
                        this.icingAircraftReports.Clear();
                        this.turbulenceAircraftReports.Clear();
                        this.wxAircraftReports.Clear();
                        foreach (var pirep in response.data.AircraftReport)
                        {
                            if (pirep.report_type != "AIREP")
                            {
                                if (pirep.icing_condition != null)
                                {
                                    this.icingAircraftReports.Add(pirep);
                                }
                                else if (pirep.turbulence_condition != null)
                                {
                                    this.turbulenceAircraftReports.Add(pirep);
                                }
                                else
                                {
                                    this.wxAircraftReports.Add(pirep);
                                }
                            }
                        }
                    }
                }
            }
        }

        private async void GetAirSigmetsAsync(int minutesAgo)
        {
            var hours = (minutesAgo / 60).ToString();
            var uri = new Uri(string.Format("{0}?dataSource=airsigmets&requestType=retrieve&format=xml&minAltitudeFt=0&maxAltitudeFt={1}&hoursBeforeNow={2}",
                AwcBaseUrl, MaxAltitude, hours));
            var xmlResponse = await uri.HttpGetAsync();
            if (xmlResponse != null)
            {
                using (var reader = XmlReader.Create(xmlResponse))
                {
                    var ser = new XmlSerializer(typeof(airsigmet.response));
                    var response = (airsigmet.response)ser.Deserialize(reader);
                    if (response.data != null)
                    {
                        this.airsigmets.Clear();
                        foreach (var airsigmet in response.data.AIRSIGMET)
                        {
                            this.airsigmets.Add(airsigmet);
                        }
                    }
                }
            }
        }

        public async Task<string> GetAreaForecast(string state)
        {
            string region;
            if (this.areaForecastRegions.TryGetValue(state, out region))
            {
                var url = new Uri("http://aviationweather.gov/areafcst/data?region=" + region);
                return await url.HttpGetStringAsync();
            }
            else
            {
                return string.Empty;
            }
        }

        private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            var previousPosition = this.MyLocation;
            this.geoPosition = args.Position.Coordinate.Point.Position;
            var pos = string.Format("{0},{1}", this.geoPosition.Latitude, this.geoPosition.Longitude);
            Debug.WriteLine("Position changed to {0}.", pos);
            ApplicationData.Current.LocalSettings.Values["LastPosition"] = pos;
            if (previousPosition.DistanceTo(this.geoPosition) > 1)
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Low,
                    () => { var nearby = AirportsNearby; });
            }
        }

        private void UpdateTimerCallback(Object state)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Low,
               () => this.UpdateAirportsWithLatestMetarsAsync(this.airportsNearby));
        }
    }
}
