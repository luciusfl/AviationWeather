//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AirportPage.xaml.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using AixmDatabase;
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Threading.Tasks;
    using taf;
    using ViewModels;
    using Windows.Devices.Geolocation;
    using Windows.Phone.UI.Input;
    using Windows.System;
    using Windows.UI.StartScreen;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;
    using Common;
    using Windows.UI.Xaml.Media;
    using Windows.UI;

    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AirportPage : Page
    {
        private readonly ObservableCollection<string> wxImageList = new ObservableCollection<string>();
        private Airport airport = new Airport();
        private Airport airportNearby = new Airport();

        public AirportPage()
        {
            this.InitializeComponent();
            HardwareButtons.BackPressed += this.OnBackPressed;
            this.DataContext = this;
        }

        public Airport Airport
        {
            get { return this.airport; }
        }

        public double ScreenWidth
        {
            get { return Window.Current.Bounds.Width; }
        }

        public ObservableCollection<ModelOutputStatistic> Mos
        {
            get { return this.Airport.Mos; }
        }

        public Airport NearbyAirport
        {
            get { return this.airportNearby; }
        }

        public Visibility ShowAlternateTerminalForecast
        {
            get { return this.airport.HasTaf ? Visibility.Collapsed : Visibility.Visible; }
        }

        public TAF[] Taf
        {
            get { return this.airport.Taf; }
        }

        public ObservableCollection<string> WeatherImages
        {
            get { return this.wxImageList; }
        }

        /// <summary>
        ///     Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">
        ///     Event data that describes how this page was reached.
        ///     This parameter is typically used to configure the page.
        /// </param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var nav = (NavigationContext)e.Parameter;
            this.airport = (Airport)nav.Item;
            var tileId = string.IsNullOrEmpty(this.airport.IcaoIdenticator)
                ? this.airport.Designator
                : this.airport.IcaoIdenticator;
            if (SecondaryTile.Exists(tileId))
            {
                this.PinBarButton.Icon = new SymbolIcon(Symbol.UnPin);
                this.PinBarButton.Label = "Unpin";
            }
            else
            {
                this.PinBarButton.Icon = new SymbolIcon(Symbol.Pin);
                this.PinBarButton.Label = "Pin to Start";
            }

            var bookmarks = App.GetSetting(App.BookmarksSettingKey);
            if (bookmarks.IndexOf(tileId) != -1)
            {
                this.BookmarkBarButton.Icon = new SymbolIcon(Symbol.UnFavorite);
                this.BookmarkBarButton.Label = "Unbookmark";
            }
            else
            {
                this.BookmarkBarButton.Icon = new SymbolIcon(Symbol.Favorite);
                this.BookmarkBarButton.Label = "Bookmark";
            }

            this.Hub.Header = tileId + " - " + this.airport.Name;
            this.GetSkewTDiagramAsync(this.airport.Location, 1, true);
            this.GetAreaForecastAsync(this.airport.State);
            this.InitializeAirportMetarAsync();
            this.InitializeAirportTafNearbyAsync();
            this.AddWeatherImages();
        }

        private static void ResetButtonBackgroundColor(StackPanel panel)
        {
            foreach(Button b in panel.Children)
            {
                b.Background = new SolidColorBrush(Colors.Black);
            }
        }

        private async void SkewTLogPDataPlus0HourButtonClick(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            ResetButtonBackgroundColor((StackPanel)but.DataContext);
            but.Background = new SolidColorBrush(Colors.Teal);
            await this.GetSkewTDiagramAsync(this.airport.Location, 1, false);
            but.Background = new SolidColorBrush(Colors.Gray);
        }

        private async void SkewTLogPDataPlus3HourButtonClick(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            ResetButtonBackgroundColor((StackPanel)but.DataContext);
            but.Background = new SolidColorBrush(Colors.Teal);
            await this.GetSkewTDiagramAsync(this.airport.Location, 3, false);
            but.Background = new SolidColorBrush(Colors.Gray);
        }

        private async void SkewTLogPDataPlus5HourButtonClick(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            ResetButtonBackgroundColor((StackPanel)but.DataContext);
            but.Background = new SolidColorBrush(Colors.Teal);
            await this.GetSkewTDiagramAsync(this.airport.Location, 5, false);
            but.Background = new SolidColorBrush(Colors.Gray);
        }

        private async void SkewTLogPDataPlus7HourButtonClick(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            ResetButtonBackgroundColor((StackPanel)but.DataContext);
            but.Background = new SolidColorBrush(Colors.Teal);
            await this.GetSkewTDiagramAsync(this.airport.Location, 7, false);
            but.Background = new SolidColorBrush(Colors.Gray);
        }

        private async void SkewTLogPDataPlus9HourButtonClick(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            ResetButtonBackgroundColor((StackPanel)but.DataContext);
            but.Background = new SolidColorBrush(Colors.Teal);
            await this.GetSkewTDiagramAsync(this.airport.Location, 9, false);
            but.Background = new SolidColorBrush(Colors.Gray);
        }

        private async void SkewTLogPDataPlus11HourButtonClick(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            ResetButtonBackgroundColor((StackPanel)but.DataContext);
            but.Background = new SolidColorBrush(Colors.Teal);
            await this.GetSkewTDiagramAsync(this.airport.Location, 11, false);
            but.Background = new SolidColorBrush(Colors.DarkGray);
        }

        private void AddWeatherImages()
        {
            this.wxImageList.Add(App.GetInfraredImageUrl(this.airport));
            var region = App.ViewModel.GetClosestWeatherRegionIdentifier(this.airport.Location);
            var visImage = string.Format("http://aviationweather.gov/adds/data/satellite/latest_{0}_vis.jpg", region);
            this.wxImageList.Add(visImage);
            this.wxImageList.Add(App.GetRadarSummaryImageUrl(this.airport));
            this.wxImageList.Add(App.GetLightningProbabilityImageUrl(this.airport));
        }

        private async Task InitializeAirportMetarAsync()
        {
            if (this.airport != null && this.airport.Metar == null && this.airport.HasMetarStation)
            {
                var metars = await Airport.GetMetarsAsync(this.airport.IcaoIdenticator);
                if (metars != null && metars.Length > 0)
                {
                    this.airport.Metar = metars[0];
                }
            }
        }

        private async Task InitializeAirportTafNearbyAsync()
        {
            const int Radius = 20;
            const int MinimumRunwayLength = 1000;
            var nearby = App.ViewModel.GetAirportsNearby(this.airport.Location, Radius, MinimumRunwayLength);
            this.airportNearby = null;
            foreach (var apt in nearby)
            {
                if (apt.HasTaf)
                {
                    this.airportNearby = apt;
                    break;
                }
            }

            if (this.NearbyAirport == null)
            {
                this.Hub.Sections[2].Header = "No TAF nearby found.";
            }
            else if (this.NearbyAirport.Id != this.airport.Id)
            {
                this.Hub.Sections[2].Header = string.Format(
                    "Nearest TAF is {0} {1:F0} nm away",
                    this.NearbyAirport.Designator,
                    this.NearbyAirport.Location.DistanceTo(this.airport.Location));
            }
        }

        private async Task GetAreaForecastAsync(string state)
        {
            this.Hub.Sections[5].Header = "Area Forecast (UTC now " + DateTime.UtcNow.ToString("HH:mm") + ")";
            var html = await App.ViewModel.GetAreaForecast(state);
            if (!string.IsNullOrEmpty(html))
            {
                int start = html.IndexOf("<pre>");
                if (start != -1)
                {
                    int end = html.IndexOf("</pre>", start);
                    if (end != -1)
                    {
                        this.Airport.AreaForecast = html.Substring(start + 6, end - start - 6);
                    }
                }
            }
        }

        private async Task GetSkewTDiagramAsync(BasicGeoposition pos, int hour, bool addSkewTLogPImage)
        {
            var url =
                new Uri(
                    string.Format(
                        "http://rucsoundings.noaa.gov/gifs/reply-skewt.cgi?data_source=Op40&lon={0}&lat={1}&add_hours={2}",
                        pos.Longitude,
                        pos.Latitude,
                        hour));
            var s = await url.HttpGetAsync();
            if (s != null)
            {
                using (var reader = new StreamReader(s))
                {
                    if (reader != null)
                    {
                        ParseSkewTLogPData(pos, reader, addSkewTLogPImage);
                    }
                }
            }
        }

        private void ParseSkewTLogPData(BasicGeoposition pos, StreamReader reader, bool addSkewTLogPImage)
        {            
            WindsAloft windsAloftTemporary = new WindsAloft();
            WindsAloft windsAloft = new WindsAloft();
            bool inRawDataTable = false;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line.StartsWith("<img src=\""))
                {
                    var end = line.IndexOf("\"", 12);
                    if (addSkewTLogPImage)
                    {
                        var skewTgif = "http://rucsoundings.noaa.gov/gifs/" + line.Substring(10, end - 10);
                        this.wxImageList.Add(skewTgif);
                    }

                    windsAloft.QueryTime = DateTime.UtcNow;
                }
                else if (line.StartsWith("data is for nearest Op40 grid point"))
                {
                    var gridpointBearing = reader.ReadLine();
                    if (gridpointBearing.EndsWith("   "))
                    {
                        gridpointBearing = gridpointBearing.Substring(0, gridpointBearing.Length - 3);
                    }

                    windsAloft.GridPointBearing = string.Format("{0}{1} {2:F0}ft", gridpointBearing, this.Airport.Designator, this.Airport.FieldElevation);
                }
                else if (line.StartsWith(" (ft)   (mb)       (kts)  (F)   (C)   (C)"))
                {
                    inRawDataTable = true;
                    reader.ReadLine(); // Forward
                }
                else if (inRawDataTable)
                {
                    if (line.Contains("</pre>"))
                    {
                        inRawDataTable = false;
                    }
                    else
                    {
                        if (!line.Contains("*****"))
                        {
                            int alt = int.Parse(line.Substring(0, 6));
                            if (alt < 25000)
                            {
                                int dir = int.Parse(line.Substring(16, 3));
                                int speed = int.Parse(line.Substring(20, 3));
                                float temp = float.Parse(line.Substring(30, 5));
                                float dew = float.Parse(line.Substring(36, 5));
                                var windAloft = new WindAloft
                                {
                                    Altitude = alt,
                                    Temperature = temp,
                                    DewPoint = dew,
                                    Direction = dir,
                                    Speed = speed
                                };

                                windsAloftTemporary.Winds.Add(windAloft);
                                windsAloftTemporary.Location = pos;
                                windsAloftTemporary.ValidFrom = DateTime.UtcNow;
                                windsAloftTemporary.ValidTo = windsAloftTemporary.ValidFrom + TimeSpan.FromHours(2);
                            }
                        }
                    }
                }
            }

            if (windsAloftTemporary.Winds.Count > 0)
            {
                this.InterpolateWindsAloftOn500FeetBoundaries(windsAloftTemporary, windsAloft);
            }

            this.Airport.WindsAloft = windsAloft;
        }

        private void InterpolateWindsAloftOn500FeetBoundaries(WindsAloft windsAloftRawData, WindsAloft windsAloft)
        {
            WindAloft first = windsAloftRawData.Winds[0];
            var start = RoundUpTo500(first.Altitude);
            for (int alt = 500; alt <= 20000; alt += 500)
            {
                if (start <= alt)
                {
                    // Find below and above.
                    WindAloft prev = windsAloftRawData.Winds[0];
                    foreach (var wind in windsAloftRawData.Winds)
                    {
                        if (wind.Altitude > alt)
                        {
                            var ratio = (prev.Altitude - wind.Altitude) / (prev.Altitude - alt);
                            var windAloft = new WindAloft
                            {
                                Temperature = prev.Temperature - (prev.Temperature - wind.Temperature) / ratio,
                                Altitude = alt,
                                DewPoint = prev.DewPoint - (prev.DewPoint - wind.DewPoint) / ratio,
                                Direction = prev.Direction - (prev.Direction - wind.Direction) / ratio,
                                Speed = prev.Speed - (prev.Speed - wind.Speed) / ratio
                            };

                            this.CheckForInversionAndFreezingPoint(windsAloft, windAloft);
                            windsAloft.Winds.Add(windAloft);
                            break;
                        }
                        else
                        {
                            prev = wind;
                        }
                    }
                }
            }
        }

        private static float RoundUpTo500(float value)
        {
            var remainder = value % 500;
            return value - remainder + 500;
        }

        private void CheckForInversionAndFreezingPoint(WindsAloft windsAloft, WindAloft windAloft)
        {
            int count = windsAloft.Winds.Count;
            if (count > 1)
            {
                var prev = windsAloft.Winds[count - 1];
                if (prev.Temperature >= 0 && windAloft.Temperature <= 0 || prev.Temperature <= 0 && windAloft.Temperature >= 0)
                {
                    // Freezing Point.
                    if (prev.Temperature != 0 && windAloft.Temperature != 0)
                    {
                        // Inject one interpolated line at zero degrees Celsius.
                        var ratio = prev.Temperature / (prev.Temperature - windAloft.Temperature);
                        var windsAloftAtFreezingPoint = new WindAloft
                        {
                            Temperature = 0,
                            Altitude = prev.Altitude - ratio * (prev.Altitude - windAloft.Altitude),
                            DewPoint = prev.DewPoint - ratio * (prev.DewPoint - windAloft.DewPoint),
                            Direction = prev.Direction - ratio * (prev.Direction - windAloft.Direction),
                            Speed = prev.Speed - ratio * (prev.Speed - windAloft.Speed)
                        };

                        if (windsAloftAtFreezingPoint.Temperature < windAloft.Temperature)
                        {
                            windAloft.Inversion = true;
                        }

                        windsAloft.Winds.Add(windsAloftAtFreezingPoint);
                    }
                }

                var windAloftMinus = windsAloft.Winds[count - 1];
                if (windAloft.Temperature > windAloftMinus.Temperature)
                {
                    windAloft.Inversion = true;
                }

                MarkWindShear(windAloft, windAloftMinus);
            }
        }

        private static void MarkWindShear(WindAloft windAloft, WindAloft windAloftMinus)
        {
            if ((Math.Abs(windAloft.Direction - windAloftMinus.Direction) > 30 &&
                Math.Abs(windAloft.Speed - windAloftMinus.Speed) > 10) ||
                Math.Abs(windAloft.Speed - windAloftMinus.Speed) > 20)
            {
                // If within 500ft altitude change wind direction changes by more than 30 degrees, 
                // or wind speed changes by more than 10 knots then set wind shear flag.
                windAloft.WindShear = true;
            }
        }

        private void OnBackPressed(object sender, BackPressedEventArgs e)
        {
            var frame = Window.Current.Content as Frame;
            if (frame != null && frame.CanGoBack)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }

        private void TogglePinButton(object sender, RoutedEventArgs e)
        {
            var icon = (SymbolIcon)((AppBarButton)sender).Icon;
            if (icon.Symbol == Symbol.Pin)
            {
                App.PinToStart(this.airport);
                this.PinBarButton.Icon = new SymbolIcon(Symbol.UnPin);
                this.PinBarButton.Label = "Unpin";
            }
            else
            {
                App.Unpin(this.airport);
                this.PinBarButton.Icon = new SymbolIcon(Symbol.Pin);
                this.PinBarButton.Label = "Pin to Start";
            }
        }

        private void ToggleBookmarkButton(object sender, RoutedEventArgs e)
        {
            var icon = (SymbolIcon)((AppBarButton)sender).Icon;
            var tileId = string.IsNullOrEmpty(this.airport.IcaoIdenticator)
                ? this.airport.Designator
                : this.airport.IcaoIdenticator;
            if (icon.Symbol == Symbol.Favorite)
            {
                var bookmarks = App.GetSetting(App.BookmarksSettingKey);
                bookmarks += tileId + ",";
                App.SaveSetting(App.BookmarksSettingKey, bookmarks);
                this.BookmarkBarButton.Icon = new SymbolIcon(Symbol.UnFavorite);
                this.BookmarkBarButton.Label = "Unbookmark";
                App.ViewModel.AirportsBookmarked.Add(this.airport);
            }
            else
            {
                var bookmarks = App.GetSetting(App.BookmarksSettingKey);
                var start = bookmarks.IndexOf(tileId + ",");
                if (start != -1)
                {
                    bookmarks = bookmarks.Remove(start, tileId.Length + 1);
                    App.SaveSetting(App.BookmarksSettingKey, bookmarks);
                    this.BookmarkBarButton.Icon = new SymbolIcon(Symbol.Favorite);
                    this.BookmarkBarButton.Label = "Bookmark";
                    App.ViewModel.AirportsBookmarked.Remove(this.airport);
                }
            }
        }

        private void Home_OnClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HubPage));
        }

        private void FindBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HubPage), "SearchHubSection");
        }

        private void WebBrowser_OnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            // We can't use the WebView control without this events, if we want to open PDF docs.
            // See http://blogs.msdn.com/b/wsdevsol/archive/2014/04/03/ten-things-you-need-to-know-about-webview-_2d00_-an-update-for-windows-8.1.aspx
            if (args.Uri.OriginalString.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                Launcher.LaunchUriAsync(args.Uri);
            }
        }

        private void WebBrowser_OnNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            // We can't use the WebView control without this events, if we want to open PDF docs.
            // See http://blogs.msdn.com/b/wsdevsol/archive/2014/04/03/ten-things-you-need-to-know-about-webview-_2d00_-an-update-for-windows-8.1.aspx
            sender.NavigationStarting += WebBrowser_OnNavigationStarting;
            sender.NavigationCompleted -= WebBrowser_OnNavigationCompleted;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
