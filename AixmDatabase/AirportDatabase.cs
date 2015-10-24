//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AirportDatabase2.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Windows.Devices.Geolocation;
#if WINDOWS_PHONE_APP
    using Windows.ApplicationModel;
    using Windows.Storage;

#endif

    public sealed class AirportDatabase
    {
        private readonly List<Airport> airportLatitudes = new List<Airport>();

        // Airports by internal airport id.
        private readonly Dictionary<string, Airport> airports = new Dictionary<string, Airport>(20000);

        // Airport by three letter designator. I.e. RNT for Renton.
        private readonly Dictionary<string, Airport> airportsByDesignator = new Dictionary<string, Airport>(
            20000,
            StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<Airport>> airportsByCity = new Dictionary<string, List<Airport>>(
            20000,
            StringComparer.OrdinalIgnoreCase);

#if WINDOWS_PHONE_APP
        public void Initialize()
        {
            if (this.airportsByDesignator.Any())
            {
                throw new InvalidOperationException("Airport database already loaded.");
            }

            this.DeserializeAirports(@"Assets\aixm.db");
        }

        private void DeserializeAirports(string filename)
        {
            var fullFilename = Path.Combine(Package.Current.InstalledLocation.Path, filename);
            var asyncFile = StorageFile.GetFileFromPathAsync(fullFilename);
            var file = asyncFile.AsTask().Result.OpenStreamForReadAsync();
            foreach (var a in AirportBase.Deserialize(file.Result))
            {
                this.airports.Add(a.Designator, a);
            }

            this.CreateIndexes();
        }

#else
        public void Initialize(string filename)
        {
            if (this.airportsByDesignator.Any())
            {
                throw new InvalidOperationException("Airport database already loaded.");
            }

            using (var file = File.OpenRead(filename))
            {
                foreach (var a in AirportBase.Deserialize(file))
                {
                    this.airports.Add(a.Designator, a);
                }
            }
        
            this.CreateIndexes();
        }
#endif

        private void CreateIndexes()
        {
            foreach (var key in this.airports.Keys)
            {
                var airport = this.airports[key];
                if (!airport.IsHeliport) // Exclude helicopter pads.
                {
                    this.airportsByDesignator.Add(airport.Designator, airport);
                    if (!string.IsNullOrEmpty(airport.IcaoIdenticator) && airport.IcaoIdenticator != airport.Designator)
                    {
                        this.airportsByDesignator.Add(airport.IcaoIdenticator, airport);
                    }

                    if (!string.IsNullOrEmpty(airport.City))
                    {
                        List<Airport> list;
                        if (this.airportsByCity.TryGetValue(airport.City, out list))
                        {
                            list.Add(airport);
                        }
                        else
                        {
                            list = new List<Airport> { airport };
                            this.airportsByCity.Add(airport.City, list);
                        }
                    }

                    this.airportLatitudes.Add(airport);
                }
            }

            // Sort by latitude and longitude.
            this.airportLatitudes.Sort((a, b) => a.Location.Latitude.CompareTo(b.Location.Latitude));
        }

        public Dictionary<string, Airport> AirportTable
        {
            get { return this.airports; }
        }

        public IEnumerable<Airport> Nearby(BasicGeoposition pos, int radiusNauticalMiles, int minimumRunwayLengthInFeet)
        {
            Debug.Assert(radiusNauticalMiles > 0);
            var center = this.LatitudeBinarySearch(pos.Latitude);
            var toLatN = pos.Latitude + radiusNauticalMiles / 60f;
            var toLatS = pos.Latitude - radiusNauticalMiles / 60f;
            var delta = radiusNauticalMiles / 60f * (1 / Math.Cos(pos.Latitude * Math.PI / 180));
            var toLonW = pos.Longitude - delta; // West most boundary.
            var toLonE = pos.Longitude + delta; // East most boundary.

            var airportsNearby = new List<Airport>();

            if(!this.airportLatitudes.Any())
            {
                // This condition is possible under the rare condition, where a location update notification
                // fires prior to full database initialization.
                return airportsNearby;
            }

            // Walk latitudes and longitudes in both directions between inner and outer radius.
            var stopFlag = 0; // Determines stop condition of loop.
            for (var i = 0; stopFlag != 3; ++i)
            {
                var iLatN = center + i; // Moving North.
                var iLatS = center - i; // Moving South.

                Airport airport = null;
                airport = this.airportLatitudes[iLatN];
                if (airport.Location.Latitude <= toLatN)
                {
                    // Found airport South of Northern boundary. Now check if within the East-West boundary.
                    if (airport.Location.Longitude <= toLonE && airport.Location.Longitude >= toLonW &&
                        airport.LongestRunwayLength > minimumRunwayLengthInFeet)
                    {
                        airportsNearby.Add(airport);
                    }
                }
                else
                {
                    stopFlag |= 1; // Reached outer boundary.
                }

                airport = this.airportLatitudes[iLatS];
                if (airport.Location.Latitude >= toLatS)
                {
                    // Found airport North of Southern boundary. Now check if within the East-West boundary.
                    if (airport.Location.Longitude <= toLonE && airport.Location.Longitude >= toLonW &&
                        airport.LongestRunwayLength > minimumRunwayLengthInFeet)
                    {
                        if (i != 0) // Special case when there is an airport at the center.
                        {
                            airportsNearby.Add(airport);
                        }
                    }
                }
                else
                {
                    stopFlag |= 2; // Reached outer boundary.          
                }
            }

            Debug.Assert(airportsNearby.Count() == airportsNearby.Distinct().Count()); // There should be no duplicates.
            return airportsNearby.OrderBy(x => x.Location.DistanceTo(pos));
        }

        public int LatitudeBinarySearch(double lat)
        {
            int first = 0, last = this.airportLatitudes.Count - 1, middle = 0;
            while (first <= last)
            {
                middle = (first + last) / 2;
                if (this.airportLatitudes[middle].Location.Latitude == lat)
                {
                    return middle;
                }
                if (this.airportLatitudes[middle].Location.Latitude > lat)
                {
                    last = middle - 1;
                }
                else
                {
                    first = middle + 1;
                }
            }

            // Nothing found, so return nearest.
            return middle;
        }

        public Airport LookupByDesignator(string designator)
        {
            Airport airport = null;
            this.airportsByDesignator.TryGetValue(designator, out airport);
            return airport;
        }

        public List<Airport> LookupByCity(string city)
        {
            List<Airport> airports;
            if (this.airportsByCity.TryGetValue(city, out airports))
            {
                return airports;
            }
            return new List<Airport>(0);
        }
    }
}
