//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="App.xaml.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Diagnostics;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.ApplicationModel.Background;
    using Windows.ApplicationModel.Core;
    using Windows.Storage;
    using Windows.UI.Core;
    using Windows.UI.StartScreen;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Navigation;
    using ViewModels;

    /// <summary>
    ///     Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        public const string BookmarksSettingKey = "Bookmarks";
        public const string TaskName = "TimeTriggeredTask";
        private const string NorthWestStates = "WA,OR,ID,MT,WY";
        private const string SouthWestStates = "CA,NV,UT,AZ,NM,CO";
        private const string MidWestStates = "ND,SD,NE,MN,WI,IL";
        private const string NorthEastStates = "MI,IN,OH,WV,PA,NY,VT,NH,ME,MA,RI,CT,NJ,DE,MD,DC";
        private const string SouthEastStates = "LA,AR,NC,MS,AL,GA,FL";
        private const string CentralPlainStates = "NE,KS,OK,IA,MO,AR";
        private const string SouthernPlainStates = "NM,TX,OK,LA";
        private const string AtlanticStates = "TN,NC,SC,KY,VA";
        private TransitionCollection transitions;

        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            //Use to debug DataBinding error 
            Current.DebugSettings.BindingFailed += this.DebugSettings_BindingFailed;
            this.InitializeComponent();
            ViewModel = new AirportViewModel();
            this.Suspending += this.OnSuspending;
        }

        public static Hub MainHub { get; set; }
        public static AirportViewModel ViewModel { get; private set; }

        public static void UnregisterAllTasks()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks.Values)
            {
                task.Unregister(true);
            }
        }

        /// <summary>
        ///     Determine if task with given name requires background access.
        /// </summary>
        public static bool TaskRequiresBackgroundAccess()
        {
#if WINDOWS_PHONE_APP
            return true;
#else
            if (name == TaskName)
            {
                return true;
            }
            else
            {
                return false;
            }
#endif
        }

        private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }

        private async void CheckAppVersion()
        {
            String appVersion = String.Format("{0}.{1}.{2}.{3}",
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Revision);

            if (ApplicationData.Current.LocalSettings.Values["AppVersion"] != appVersion)
            {
                // Our app has been updated
                ApplicationData.Current.LocalSettings.Values["AppVersion"] = appVersion;

                // http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh977051.aspx
                BackgroundExecutionManager.RemoveAccess();
            }

            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
        }


        /// <summary>
        ///     Invoked when the application is launched normally by the end user.  Other entry points
        ///     will be used when the application is launched to open a specific file, to display
        ///     search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //GetGeoPosition();
                    //locationUpdateTimer.Enabled = true;
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            var consent = GetSetting("Consent");
            if (string.IsNullOrEmpty(consent))
            {
                if (!rootFrame.Navigate(typeof (ConsentPage)))
                {
                    throw new Exception("Failed to create consent page");
                }
            }
            else if (e.Arguments.Length > 1)
            {
                var designator = e.Arguments;
                var airport = ViewModel.LookupAirportId(designator);
                var nav = new NavigationContext { Item = airport };
                rootFrame.Navigate(typeof (AirportPage), nav);
            }
            else if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof (HubPage), e.Arguments))
                {
                    throw new Exception("Failed to create hub page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        ///     Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            var transitionCollection = new TransitionCollection { new NavigationThemeTransition() };
            rootFrame.ContentTransitions = this.transitions ?? transitionCollection;
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        ///     Invoked when application execution is being suspended.  Application state is saved
        ///     without knowing whether the application will be terminated or resumed with the contents
        ///     of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        public static void PinToStart(Airport airport)
        {
            var tileId = string.IsNullOrEmpty(airport.IcaoIdenticator) ? airport.Designator : airport.IcaoIdenticator;
            var tile = new SecondaryTile(tileId, tileId, tileId, new Uri("ms-appx:///Assets/150x150BlankTile.png"), TileSize.Square150x150);
            SecondaryTileUpdater.UpdateAsync(tile, airport.Metar);
        }

        public static void Unpin(Airport airport)
        {
            var tileId = string.IsNullOrEmpty(airport.IcaoIdenticator) ? airport.Designator : airport.IcaoIdenticator;
            if (SecondaryTile.Exists(tileId))
            {
                var secondaryTile = new SecondaryTile(tileId);
                secondaryTile.RequestDeleteAsync();
            }
        }

        public static void SaveSetting(string key, object value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Low,
                () =>
                {
                    ApplicationData.Current.RoamingSettings.Values.Remove(key);
                    ApplicationData.Current.RoamingSettings.Values.Add(key, value);
                });
        }

        public static string GetSetting(string key)
        {
            object obj;
            ApplicationData.Current.RoamingSettings.Values.TryGetValue(key, out obj);
            return obj == null ? string.Empty : obj.ToString();
        }

        public static string GetRadarSummaryImageUrl(Airport airport)
        {
            var state = airport.State;
            if (NorthWestStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_nw.gif";
            }
            if (SouthWestStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_sw.gif";
            }
            if (MidWestStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_mw.gif";
            }
            if (NorthEastStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_ne.gif";
            }
            if (SouthEastStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_se.gif";
            }
            if (CentralPlainStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_cp.gif";
            }
            if (SouthernPlainStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_sp.gif";
            }
            if (AtlanticStates.Contains(state))
            {
                return "http://weather.unisys.com/radar/wrad_at.gif";
            }
            if (state == "AK")
            {
                return "http://aawu.arh.noaa.gov/fcstgraphics/sfc.gif";
            }
            Debug.WriteLine("{0} not found. Returning US radar summary image.", state);
            return "http://weather.unisys.com/radar/wrad_us.gif";
        }

        public static string GetInfraredImageUrl(Airport airport)
        {
            var state = airport.State;
            if (NorthWestStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_nw.gif";
            }
            if (SouthWestStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_sw.gif";
            }
            if (MidWestStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_mw.gif";
            }
            if (NorthEastStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_ne.gif";
            }
            if (SouthEastStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_se.gif";
            }
            if (CentralPlainStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_cp.gif";
            }
            if (SouthernPlainStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_sp.gif";
            }
            if (AtlanticStates.Contains(state))
            {
                return "http://weather.unisys.com/satellite/sat_ir_enh_at.gif";
            }
            if (state == "AK")
            {
                return "http://pafc.arh.noaa.gov/data/sat/current4gv.png";
            }
            Debug.WriteLine("{0} not found. Returning US radar summary image.", state);
            return "http://weather.unisys.com/satellite/sat_ir_enh_us.gif";
        }

        public static string GetLightningProbabilityImageUrl(Airport airport)
        {
            var state = airport.State;
            if (NorthWestStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/NW_probltg.gif";
            }
            if (SouthWestStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/SW_probltg.gif";
            }
            if (MidWestStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/NE_probltg.gif";
            }
            if (NorthEastStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/NE_probltg.gif";
            }
            if (SouthEastStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/SE_probltg.gif";
            }
            if (CentralPlainStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/NC_probltg.gif";
            }
            if (SouthernPlainStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/SC_probltg.gif";
            }
            if (AtlanticStates.Contains(state))
            {
                return "http://www.nws.noaa.gov/mdl/radar/SE_probltg.gif";
            }
            if (state == "AK")
            {
                return "http://pafc.arh.noaa.gov/data/tvwx/today.jpg";
            }
            Debug.WriteLine("{0} not found. Returning US radar summary image.", state);
            return "http://weather.unisys.com/satellite/sat_ir_enh_us.gif";
        }
    }
}
