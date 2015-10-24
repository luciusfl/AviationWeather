//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="NavigationContext.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation
{
    using Windows.UI.Xaml.Controls;

    internal class NavigationContext
    {
        public Page SourcePage { get; set; }
        public object Sender { get; set; }
        public object Item { get; set; }
    }
}
