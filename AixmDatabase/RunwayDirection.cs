//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="RunwayDirection.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.ViewModels
{
    using System.Diagnostics;
    using System.IO;

    [DebuggerDisplay("Id = {Id}, Designator = {Designator}, Traffic = {TrafficDirection}, {TrueBearing}°")]
    public sealed class RunwayDirection : AirportBase
    {
        public RunwayDirection()
        {
            this.TrafficDirection = 'L';
        }

        public string Designator { get; set; }

        public char TrafficDirection { get; set; }

        public int TrueBearing { get; set; }

        public override string ToString()
        {
            return string.Format("{0}, {1}-Traffic {2}°", this.Designator, this.TrafficDirection, this.TrueBearing);
        }

        public override void Serialize(StreamWriter writer)
        {
            base.Serialize(writer);
            writer.Write(this.Designator);
            writer.Write('|');
            writer.Write(this.TrafficDirection);
            writer.Write('|');
            writer.Write(this.TrueBearing);
            writer.Write('|');
        }

        public override void Deserialize(StreamReader reader)
        {
            base.Deserialize(reader); // Call base serializer.
            this.Designator = base.DeserializeReadNext(reader);
            this.TrafficDirection = base.DeserializeReadNext(reader)[0];
            this.TrueBearing = int.Parse(base.DeserializeReadNext(reader));
        }
    }
}
