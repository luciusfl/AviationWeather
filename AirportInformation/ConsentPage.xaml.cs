//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ConsentPage.xaml.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace AirportInformation
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConsentPage : Page
    {
        public ConsentPage()
        {
            this.InitializeComponent();
        }

        private void OnAcceptConsent(object sender, RoutedEventArgs e)
        {
            App.SaveSetting("Consent", "Accepted");
            this.Frame.Navigate(typeof(HubPage));
        }

        private void OnDeclineConsent(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}
