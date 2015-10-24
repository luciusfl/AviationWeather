//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="FaaMember.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace ParseAIXM
{
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Xml;

    [DebuggerDisplay("Id = {Id}, ArtifactType = {ArtifactType}, AirportType={AirportType}")]
    public class FaaMember
    {
        private Collection<FaaMember> child = new Collection<FaaMember>();

        public string Id { get; set; }

        public string ArtifactType { get; set; }

        public string AirportType { get; set; }

        public XmlReader Reader { get; set; }

        public Collection<FaaMember> Child
        {
            get { return this.child; }
            set { this.child = value; }
        }
    }
}
