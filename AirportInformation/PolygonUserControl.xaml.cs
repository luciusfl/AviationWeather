//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PolygonUserControl.xaml.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;
    using Windows.Devices.Geolocation;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Maps;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Shapes;
    using aircraftreport;
    using airsigmet;
    using Common;
    using gairmet;
    using response = gairmet.response;

    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PolygonUserControl : UserControl
    {
        public static readonly DependencyProperty TagProp = DependencyProperty.Register(
            "AirSigmet",
            typeof (object),
            typeof (AIRSIGMET),
            new PropertyMetadata(null));

        public PolygonUserControl()
        {
            this.InitializeComponent();
            var airmets = App.ViewModel.AirSigmets;
        }

        public async void ShowGairmet(GairmetWeatherPhenomena phenomena)
        {
            // GAirmet are preferred, but for some reason the url return an internal server error.
            //
            var gairmets = await GetGairmetsAsync();
            foreach (var gairmet in gairmets)
            {
                if (this.GairmetQualifies(gairmet.hazard.type, phenomena))
                {
                    foreach (var a in gairmet.area)
                    {
                        var positions = new List<BasicGeoposition>(int.Parse(a.num_points));
                        positions.AddRange(
                            a.point.Select(
                                pt => new BasicGeoposition { Latitude = pt.latitude, Longitude = pt.longitude }));
                        var shape = new MapPolygon();
                        shape.Path = new Geopath(positions);
                        var c = this.MapWeatherPhenomenaToShapeColor(gairmet.hazard.type);
                        shape.StrokeColor = c;
                        shape.FillColor = new Color { A = 30, B = c.B, G = c.G, R = c.R };
                        shape.StrokeThickness = 2;
                        this.Map.MapElements.Add(shape);
                    }
                }
            }
        }

        public async void ShowAirSigmet(AirSigmetWeatherPhenomena phenomena)
        {
            foreach (var airsigmet in App.ViewModel.AirSigmets)
            {
                if (this.AirSigmetQualifies(airsigmet, phenomena))
                {
                    foreach (var a in airsigmet.area)
                    {
                        var positions = new List<BasicGeoposition>(int.Parse(a.num_points));
                        foreach (var pt in a.point)
                        {
                            positions.Add(new BasicGeoposition { Latitude = pt.latitude, Longitude = pt.longitude });
                        }

                        var shape = new MapPolygon();
                        shape.Path = new Geopath(positions);
                        var c = this.MapWeatherPhenomenaToShapeColor(airsigmet.hazard.type);
                        shape.StrokeColor = c;
                        shape.FillColor = new Color { A = 30, B = c.B, G = c.G, R = c.R };
                        shape.StrokeThickness = 2;
                        shape.SetValue(TagProp, airsigmet);
                        this.Map.MapElements.Add(shape);

                        // We need to draw some text over the area, with information, such as valid time, severity etc...
                        // Not sure how to do this best.
                        var icon = new MapIcon();
                        var sb = new StringBuilder();
                        if (airsigmet.altitude != null)
                        {
                            if (airsigmet.altitude.min_ft_mslSpecified)
                            {
                                sb.AppendFormat("From {0}ft", airsigmet.altitude.min_ft_msl);
                            }

                            if (airsigmet.altitude.max_ft_mslSpecified)
                            {
                                sb.AppendFormat(" to {0}ft", airsigmet.altitude.max_ft_msl);
                            }
                        }

                        icon.Title = sb.ToString();
                        this.Map.MapElements.Add(icon);
                    }
                }
            }
        }

        private void ShowPireps(PirepType pirepType)
        {
            var pireps = pirepType == PirepType.Icing ? App.ViewModel.IcingPireps : App.ViewModel.TurbulencePireps;
            foreach (var pirep in pireps)
            {
                this.AddPushpin(pirep);
            }
        }

        private void ShowHighSeverityPireps()
        {
            foreach (var pirep in App.ViewModel.IcingPireps)
            {
                if (this.IsSeverePirep(pirep))
                {
                    this.AddPushpin(pirep);
                }
            }
        }

        private bool IsSeverePirep(AircraftReport pirep)
        {
            if (pirep.turbulence_condition != null)
            {
                foreach (var c in pirep.turbulence_condition)
                {
                    if (c.turbulence_intensity == "EXTM" || c.turbulence_intensity == "SEV-EXTM" ||
                        c.turbulence_intensity == "SEV" || c.turbulence_intensity == "MOD-SEV")
                    {
                        return true;
                    }
                }
            }

            if (pirep.icing_condition != null)
            {
                foreach (var c in pirep.icing_condition)
                {
                    if (c.icing_intensity == "SEV" || c.icing_intensity == "HVY" || c.icing_intensity == "MOD-SEV")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private async void ShowWX()
        {
            foreach (var pirep in App.ViewModel.WxPireps)
            {
                if (pirep.turbulence_condition == null && pirep.icing_condition == null)
                {
                    this.AddPushpin(pirep);
                }
            }
        }

        private bool IsNow(AIRSIGMET airsigmet)
        {
            var from = DateTime.Parse(airsigmet.valid_time_from).ToUniversalTime();
            var to = DateTime.Parse(airsigmet.valid_time_to).ToUniversalTime();
            var utc = DateTime.UtcNow;
            return (from < utc && to > utc);
        }

        private bool GairmetQualifies(string hazard, GairmetWeatherPhenomena phenomena)
        {
            // IFR | MT_OBSC | TURB-HI | TURB-LO | ICE | FZLVL | M_FZLVL | SFC_WND | LLWS
            // See http://aviationweather.gov/dataserver/fields?datatype=gairmet
            if (hazard == "IFR" && (phenomena & GairmetWeatherPhenomena.Ifr) == GairmetWeatherPhenomena.Ifr)
            {
                return true;
            }
            if (hazard == "MT_OBSC" &&
                (phenomena & GairmetWeatherPhenomena.MountainfObscuration) ==
                GairmetWeatherPhenomena.MountainfObscuration)
            {
                return true;
            }
            if (hazard == "TURB-HI" &&
                (phenomena & GairmetWeatherPhenomena.TurbulenceHigh) == GairmetWeatherPhenomena.TurbulenceHigh)
            {
                return true;
            }
            if (hazard == "TURB-LO" &&
                (phenomena & GairmetWeatherPhenomena.TurbulenceLow) == GairmetWeatherPhenomena.TurbulenceLow)
            {
                return true;
            }
            if (hazard == "ICE" && (phenomena & GairmetWeatherPhenomena.Icing) == GairmetWeatherPhenomena.Icing)
            {
                return true;
            }
            if (hazard == "FZLVL" &&
                (phenomena & GairmetWeatherPhenomena.FreezingLevel) == GairmetWeatherPhenomena.FreezingLevel)
            {
                return true;
            }
            if (hazard == "M_FZLVL" &&
                (phenomena & GairmetWeatherPhenomena.MFreezingLevel) == GairmetWeatherPhenomena.MFreezingLevel)
            {
                return true;
            }
            if (hazard == "SFC_WND" &&
                (phenomena & GairmetWeatherPhenomena.SurfaceWind) == GairmetWeatherPhenomena.SurfaceWind)
            {
                return true;
            }
            if (hazard == "LLWS" &&
                (phenomena & GairmetWeatherPhenomena.LowLevelWindShear) == GairmetWeatherPhenomena.LowLevelWindShear)
            {
                return true;
            }

            return true; // Always show unknown phenomena.
        }

        private bool AirSigmetQualifies(AIRSIGMET airsigmet, AirSigmetWeatherPhenomena phenomena)
        {
            // MTN OBSCN | IFR | TURB | ICE | CONVECTIVE | ASH
            // See http://aviationweather.gov/dataserver/fields?datatype=airsigmet
            if (this.IsNow(airsigmet))
            {
                var hazard = airsigmet.hazard.type;
                switch (hazard)
                {
                    case "ICE":
                        return phenomena == AirSigmetWeatherPhenomena.Icing;
                    case "IFR":
                        return phenomena == AirSigmetWeatherPhenomena.Ifr;
                    case "MTN OBSCN":
                        return phenomena == AirSigmetWeatherPhenomena.MountainObscuration;
                    case "TURB":
                        return phenomena == AirSigmetWeatherPhenomena.Turbulence;
                    case "ASH":
                        return phenomena == AirSigmetWeatherPhenomena.Ash;
                    case "CONVECTIVE":
                        return phenomena == AirSigmetWeatherPhenomena.Convective;
                    default:
                        return true;
                }
            }

            return false;
        }

        private Color MapWeatherPhenomenaToShapeColor(string phenomena)
        {
            //MTN OBSCN | IFR | TURB | ICE | CONVECTIVE | ASH
            switch (phenomena)
            {
                case "ICE":
                    return Colors.Blue;
                case "IFR":
                    return Colors.DarkMagenta;
                case "MT_OBSC":
                    return Colors.Magenta;
                case "MTN OBSCN":
                    return Colors.Magenta;
                case "TURB":
                    return Colors.Green;
                case "TURB-LO":
                    return Colors.Green;
                case "TURB-HI":
                    return Colors.Green;
                case "CONVECTIVE":
                    return Colors.Red;
                case "LLWS":
                    return Colors.Orange;
                case "SFC_WND":
                    return Colors.Orange;
                case "ASH":
                    return Colors.Black;
                case "FZLVL":
                    return Colors.LightBlue;
                case "M_FZLVL":
                    return Colors.LightBlue;
                default:
                    return Colors.Gray;
            }
        }

        private void AddPushpin(AircraftReport pirep)
        {
            DateTime observationTime;
            if (DateTime.TryParse(pirep.observation_time, out observationTime))
            {
                // Only consider pireps younger than 2 hours.
                if (pirep.longitudeSpecified && pirep.latitudeSpecified)
                {
                    SolidColorBrush brush = null;
                    if (pirep.icing_condition != null && pirep.icing_condition.Length > 0)
                    {
                        brush = this.PirepIntensityColor(pirep.icing_condition[0].icing_intensity);
                    }
                    else if (pirep.turbulence_condition != null && pirep.turbulence_condition.Length > 0)
                    {
                        brush = this.PirepIntensityColor(pirep.turbulence_condition[0].turbulence_intensity);
                    }
                    else
                    {
                        brush = new SolidColorBrush(Colors.LightSkyBlue);
                    }

                    if (brush != null)
                    {
                        const int PinSize = 35;
                        var pin = new Grid { Width = PinSize, Height = PinSize, IsTapEnabled = true };
                        pin.Tapped += this.OnPinTapped;
                        pin.Tag = pirep;
                        pin.Children.Add(
                            new Ellipse
                            {
                                Fill = brush,
                                Stroke = new SolidColorBrush(Colors.White),
                                StrokeThickness = 1,
                                Width = PinSize,
                                Height = PinSize
                            });

                        var textBlock = new TextBlock
                        {
                            Text =
                                pirep.altitude_ft_mslSpecified ? (pirep.altitude_ft_msl / 100).ToString() : string.Empty,
                            FontSize = 10,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Colors.White),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        /* TODO: Make tooltip working.
                        ToolTip tooltip = new ToolTip();
                        tooltip.Content = pirep.raw_text;
                        ToolTipService.SetToolTip(textBlock, tooltip);
                         * */
                        pin.Children.Add(textBlock);
                        MapControl.SetLocation(
                            pin,
                            new Geopoint(
                                new BasicGeoposition { Latitude = pirep.latitude, Longitude = pirep.longitude }));
                        this.Map.Children.Add(pin);
                    }
                }
            }
        }

        private void OnPinTapped(object sender, TappedRoutedEventArgs e)
        {
            var pushPin = (Grid)sender;
            var pirep = (AircraftReport)pushPin.Tag;
            var sb = new StringBuilder();
            DateTime observationTime;
            if (DateTime.TryParse(pirep.observation_time, out observationTime))
            {
                var observationTimeUtc = observationTime.ToUniversalTime();
                var timeAgo = DateTime.UtcNow - observationTimeUtc;
                var minutesAgo = timeAgo.Hours * 60 + timeAgo.Minutes;
                sb.AppendFormat("PIREP {0} mins ago: ", minutesAgo);
            }

            sb.Append(pirep.raw_text);
            this.RawText.Text = sb.ToString();
        }

        private SolidColorBrush PirepIntensityColor(string intensity)
        {
            switch (intensity)
            {
                case "EXTM":
                case "SEV-EXTM":
                case "SEV":
                case "MOD-SEV":
                case "HVY":
                    return new SolidColorBrush(Colors.Red);
                case "MOD":
                case "LGT-MOD":
                    return new SolidColorBrush(Colors.Orange);
                case "LGT":
                case "SMTH-LGT":
                case "TRC-LGT":
                case "TRC":
                    return new SolidColorBrush(Colors.Green);
                default:
                    return null;
            }
        }

        private static async Task<GAIRMET[]> GetGairmetsAsync()
        {
            var url =
                "http://aviationweather.gov/adds/dataserver_current/httpparam?dataSource=gairmets&requestType=retrieve&format=xml&hoursBeforeNow=1";
            var uri = new Uri(url);
            var xmlResponse = await uri.HttpGetAsync();
            if (xmlResponse != null)
            {
                var reader = XmlReader.Create(xmlResponse);
                var ser = new XmlSerializer(typeof (response));
                var response = (response)ser.Deserialize(reader);
                if (response.data != null)
                {
                    return response.data.GAIRMET;
                }
            }

            return new GAIRMET[0];
        }

        private void OnZoomLevelChanged(MapControl sender, object args)
        {
        }

        private void OnShowIcing(object sender, RoutedEventArgs e)
        {
            this.ResetMapControl();
            this.IcingButton.Background = new SolidColorBrush(Colors.DarkBlue);
            this.ShowAirSigmet(AirSigmetWeatherPhenomena.Icing);
            this.ShowPireps(PirepType.Icing);
        }

        private void OnShowTurbulence(object sender, RoutedEventArgs e)
        {
            this.ResetMapControl();
            this.TurbulenceButton.Background = new SolidColorBrush(Colors.DarkGreen);
            this.ShowAirSigmet(AirSigmetWeatherPhenomena.Turbulence);
            this.ShowPireps(PirepType.Turbulence);
        }

        private void OnShowIfr(object sender, RoutedEventArgs e)
        {
            this.ResetMapControl();
            this.IfrButton.Background = new SolidColorBrush(Colors.DarkMagenta);
            this.ShowAirSigmet(AirSigmetWeatherPhenomena.Ifr);
        }

        private void OnShowMountainObscuration(object sender, RoutedEventArgs e)
        {
            this.ResetMapControl();
            this.ObscurationButton.Background = new SolidColorBrush(Colors.Magenta);
            this.ShowAirSigmet(AirSigmetWeatherPhenomena.MountainObscuration);
        }

        private void OnShowConvection(object sender, RoutedEventArgs e)
        {
            this.ResetMapControl();
            this.ShowAirSigmet(AirSigmetWeatherPhenomena.Convective);
        }

        private void OnWX(object sender, RoutedEventArgs e)
        {
            this.ResetMapControl();
            this.WxButton.Background = new SolidColorBrush(Colors.LightSkyBlue);
            this.ShowWX();
        }

        private async void OnShowSigmets(object sender, RoutedEventArgs e)
        {
            this.ResetMapControl();
            this.ShowSigmet();
            this.SigmetButton.Background = new SolidColorBrush(Colors.Red);
        }

        private void ResetMapControl()
        {
            this.RawText.Text = string.Empty;
            this.Map.MapElements.Clear();
            this.Map.Children.Clear();
            this.SigmetButton.Background = new SolidColorBrush(Colors.Black);
            this.IcingButton.Background = new SolidColorBrush(Colors.Black);
            this.TurbulenceButton.Background = new SolidColorBrush(Colors.Black);
            this.IfrButton.Background = new SolidColorBrush(Colors.Black);
            this.ObscurationButton.Background = new SolidColorBrush(Colors.Black);
            this.WxButton.Background = new SolidColorBrush(Colors.Black);
        }

        private void ShowSigmet()
        {
            foreach (var airsigmet in App.ViewModel.AirSigmets)
            {
                if (this.IsNow(airsigmet) && airsigmet.airsigmet_type == "SIGMET" && airsigmet.hazard.severity != "NONE")
                {
                    foreach (var a in airsigmet.area)
                    {
                        var positions = new List<BasicGeoposition>(int.Parse(a.num_points));
                        positions.AddRange(
                            a.point.Select(
                                pt => new BasicGeoposition { Latitude = pt.latitude, Longitude = pt.longitude }));
                        var shape = new MapPolygon();
                        shape.Path = new Geopath(positions);
                        var c = this.MapWeatherPhenomenaToShapeColor(airsigmet.hazard.type);
                        shape.StrokeColor = c;
                        shape.FillColor = new Color { A = 30, B = c.B, G = c.G, R = c.R };
                        shape.StrokeThickness = 2;
                        shape.SetValue(TagProp, airsigmet);
                        //this.Map.Children.Add(shape);
                        this.Map.MapElements.Add(shape);
                    }
                }
            }

            this.ShowHighSeverityPireps();
        }

        private void Map_OnMapTapped(MapControl sender, MapInputEventArgs args)
        {
            AIRSIGMET airsigmet = null;
            foreach (var mapElement in sender.FindMapElementsAtOffset(args.Position))
            {
                var a = (AIRSIGMET)mapElement.GetValue(TagProp);
                if (airsigmet == null)
                {
                    airsigmet = a;
                }
                else
                {
                    if (a.airsigmet_type == "SIGMET")
                    {
                        airsigmet = a;
                    }
                }
            }

            if (airsigmet == null)
            {
                this.RawText.Text = string.Empty;
                return;
            }

            var sb = new StringBuilder(airsigmet.airsigmet_type);
            if (airsigmet.altitude != null && airsigmet.altitude.min_ft_mslSpecified)
            {
                sb.AppendFormat(" from {0}", airsigmet.altitude.min_ft_msl);
            }

            if (airsigmet.altitude != null && airsigmet.altitude.max_ft_mslSpecified)
            {
                sb.AppendFormat(" to {0} feet", airsigmet.altitude.max_ft_msl);
            }

            DateTime to;
            if (DateTime.TryParse(airsigmet.valid_time_to, out to))
            {
                sb.AppendFormat(" until {0:MM/dd h:mmtt} (={1:HH:mm} UTC).", to, to.ToUniversalTime());
            }

            sb.AppendLine();
            sb.Append(airsigmet.raw_text.Replace(" \n", " "));
            this.RawText.Text = sb.ToString();
        }
    }

    [Flags]
    public enum GairmetWeatherPhenomena
    {
        Ifr = 1 << 0,
        MountainfObscuration = 1 << 1,
        TurbulenceLow = 1 << 2,
        TurbulenceHigh = 1 << 3,
        Icing = 1 << 4,
        SurfaceWind = 1 << 5,
        LowLevelWindShear = 1 << 6,
        FreezingLevel = 1 << 7,
        MFreezingLevel = 1 << 8
    };

    [Flags]
    public enum AirSigmetWeatherPhenomena
    {
        Ifr = 1 << 0,
        MountainObscuration = 1 << 1,
        Turbulence = 1 << 2,
        Icing = 1 << 3,
        Convective = 1 << 4,
        Ash = 1 << 5
    };

    public enum PirepType
    {
        Icing,
        Turbulence
    }
}
