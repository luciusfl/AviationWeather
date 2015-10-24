//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PirepPushPin.xaml.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

 // The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AirportInformation
{
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using aircraftreport;

    public sealed partial class PirepPushPin : UserControl
    {
        private AircraftReport pirep;

        public PirepPushPin(AircraftReport pirep)
        {
            this.pirep = pirep;
            this.InitializeComponent();
        }

        private void pin_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Grid parent = (Grid)sender;
        }
    }
}
