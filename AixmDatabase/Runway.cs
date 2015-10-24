//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Runway.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.ViewModels
{
    using System.Diagnostics;
    using System.IO;

    [DebuggerDisplay("Id = {Id}, Designator = {Designator}, Length = {Length}")]
    public sealed class Runway : AirportBase
    {
        public string Designator { get; set; }

        public string AirportId { get; set; }

        public int Length { get; set; }

        public int Width { get; set; }

        public string Composition { get; set; }

        public RunwayDirection Base { get; set; }

        public RunwayDirection Reciprocal { get; set; }

        public override string ToString()
        {
            return string.Format("{0}, {1} {2}ft x {3}ft", this.Id, this.Designator, this.Length, this.Width);
        }

        public override void Serialize(StreamWriter writer)
        {
            base.Serialize(writer);
            writer.Write(this.Designator);
            writer.Write('|');
            writer.Write(this.AirportId);
            writer.Write('|');
            writer.Write(this.Length);
            writer.Write('|');
            writer.Write(this.Width);
            writer.Write('|');
            writer.Write(this.Composition);
            writer.Write('|');
            if (this.Base == null)
            {
                writer.Write((char)0);
                writer.Write('|');
            }
            else
            {
                writer.Write('B');
                writer.Write('|');
                this.Base.Serialize(writer);
            }

            if (this.Reciprocal == null)
            {
                writer.Write((char)0);
                writer.Write('|');
            }
            else
            {
                writer.Write('R');
                writer.Write('|');
                this.Reciprocal.Serialize(writer);
            }
        }

        public override void Deserialize(StreamReader reader)
        {
            base.Deserialize(reader); // Call base serializer.
            this.Designator = base.DeserializeReadNext(reader);
            this.AirportId = base.DeserializeReadNext(reader);
            this.Length = int.Parse(base.DeserializeReadNext(reader));
            this.Width = int.Parse(base.DeserializeReadNext(reader));
            this.Composition = base.DeserializeReadNext(reader);
            char hasRunwayDesignator = base.DeserializeReadNext(reader)[0];
            if (hasRunwayDesignator != 0)
            {
                if (hasRunwayDesignator == 'B')
                {
                    this.Base = new RunwayDirection();
                    this.Base.Deserialize(reader);
                }
                else
                {
                    this.Reciprocal = new RunwayDirection();
                    this.Reciprocal.Deserialize(reader);
                }
            }

            hasRunwayDesignator = base.DeserializeReadNext(reader)[0];
            if (hasRunwayDesignator != 0)
            {
                if (hasRunwayDesignator == 'B')
                {
                    this.Base = new RunwayDirection();
                    this.Base.Deserialize(reader);
                }
                else
                {
                    this.Reciprocal = new RunwayDirection();
                    this.Reciprocal.Deserialize(reader);
                }
            }
        }
    }
}
