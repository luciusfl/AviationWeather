//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Notam.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.ViewModels
{
    public class Notam
    {
        public string Designator { get; set; }

        public string Start { get; set; }

        public string End { get; set; }

        public string Airport { get; set; }

        public bool Active { get; set; }

        public string Message { get; set; }
    }
}
