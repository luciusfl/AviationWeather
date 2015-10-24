//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="HubPage.xaml.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using Windows.ApplicationModel.Background;
    using Windows.ApplicationModel.Resources;
    using Windows.Graphics.Display;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;
    using Common;
    using Tasks;
    using ViewModels;
    using System.Threading.Tasks;

    /// <summary>
    ///     A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        private readonly ObservableCollection<Airport> searchResult = new ObservableCollection<Airport>();

        public HubPage()
        {
            this.InitializeComponent();
            this.DataContext = this;

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            this.AirportIdDefaultSearch = "KRNT";
            this.CityNameDefaultSearch = "Seattle";
            App.MainHub = this.Hub;
            Debug.WriteLine(Window.Current.Bounds.Width);
        }

        /// <summary>
        ///     Gets the <see cref="NavigationHelper" /> associated with this <see cref="Page" />.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        ///     Gets the view model for this <see cref="Page" />.
        ///     This can be changed to a strongly typed view model.
        /// </summary>
        public AirportViewModel ViewModel
        {
            get { return App.ViewModel; }
        }

        public double ScreenWidth
        {
            get { return Window.Current.Bounds.Width; }
        }

        public String AirportIdDefaultSearch { get; set; }
        public String CityNameDefaultSearch { get; set; }

        public ObservableCollection<Airport> SearchResult
        {
            get { return this.searchResult; }
            private set { ; }
        }

        /// <summary>
        ///     Populates the page with content passed during navigation.  Any saved state is also
        ///     provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event; typically <see cref="NavigationHelper" />
        /// </param>
        /// <param name="e">
        ///     Event data that provides both the navigation parameter passed to
        ///     <see cref="Frame.Navigate(Type, Object)" /> when this page was initially requested and
        ///     a dictionary of state preserved by this page during an earlier
        ///     session.  The state will be null the first time a page is visited.
        /// </param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        ///     Preserves state associated with this page in case the application is suspended or the
        ///     page is discarded from the navigation cache.  Values must conform to the serialization
        ///     requirements of <see cref="SuspensionManager.SessionState" />.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper" /></param>
        /// <param name="e">
        ///     Event data that provides an empty dictionary to be populated with
        ///     serializable state.
        /// </param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        /// <summary>
        ///     Shows the details of an item clicked on in the <see cref="ItemPage" />
        /// </summary>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
        }

        private async Task RegisterTaskAsync()
        {
            var alreadyRegistered = BackgroundTaskRegistration.AllTasks.Values.Any(t => t.Name == App.TaskName);
            if (alreadyRegistered)
            {
                return;
            }



            if (App.TaskRequiresBackgroundAccess())
            {
                var access = await BackgroundExecutionManager.RequestAccessAsync();
                Debug.WriteLine("Request background access from user. Status {0}", access);
            }

            var entryPoint = typeof (BackgroundTask).FullName;
            var builder = new BackgroundTaskBuilder { Name = App.TaskName, TaskEntryPoint = entryPoint };
            builder.SetTrigger(new TimeTrigger(15, false));
            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            var task = builder.Register();
            Debug.WriteLine("Registered {0} with id={1}.", task.Name, task.TaskId);
        }

        private void ItemListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var nav = new NavigationContext { Sender = sender, Item = e.ClickedItem, SourcePage = this };
            this.Frame.Navigate(typeof (AirportPage), nav);
        }

        private void OnSearchByAirportId(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.AirportIdDefaultSearch))
            {
                Debug.WriteLine(this.AirportIdDefaultSearch);
                var airport = App.ViewModel.LookupAirportId(this.AirportIdDefaultSearch.Trim());
                if (airport != null)
                {
                    var nav = new NavigationContext { Sender = sender, Item = airport, SourcePage = this };
                    this.Frame.Navigate(typeof (AirportPage), nav);
                }
            }
        }

        private async void OnSearchByCity(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.CityNameDefaultSearch))
            {
                Debug.WriteLine(this.CityNameDefaultSearch);
                var airports = App.ViewModel.LookupAirportByCity(this.CityNameDefaultSearch.Trim());
                this.searchResult.Clear();
                foreach (var airport in airports)
                {
                    if (airport.HasMetarStation)
                    {
                        var metar = await Airport.GetMetarsAsync(airport.IcaoIdenticator);
                        if (metar != null && metar.Length > 0)
                        {
                            airport.Metar = metar[0];
                        }
                    }

                    this.searchResult.Add(airport);
                }
            }
        }

        private void SearchBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var searchBox = (TextBox)sender;
            searchBox.Text = string.Empty;
        }

        #region NavigationHelper registration

        /// <summary>
        ///     The methods provided in this section are simply used to allow
        ///     NavigationHelper to respond to the page's navigation methods.
        ///     <para>
        ///         Page specific logic should be placed in event handlers for the
        ///         <see cref="NavigationHelper.LoadState" />
        ///         and <see cref="NavigationHelper.SaveState" />.
        ///         The navigation parameter is available in the LoadState method
        ///         in addition to page state preserved during an earlier session.
        ///     </para>
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            await this.RegisterTaskAsync();
            string hubSection = (string)e.Parameter;
            if (!string.IsNullOrEmpty(hubSection))
            {
                var section = App.MainHub.Sections.FirstOrDefault(x => x.Name == hubSection);
                if (section != null)
                {
                    App.MainHub.ScrollToSection(section);
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}
