//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AirportBase.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public abstract class AirportBase
    {
        private static char[] buffer = new char[2048];

        public virtual string Id { get; set; }

        public virtual void Serialize(StreamWriter writer)
        {
            writer.Write(Id);
            writer.Write('|');
        }

        public virtual void Deserialize(StreamReader reader)
        {
            for (int i = 0; i < buffer.Length; ++i)
            {
                reader.Read(buffer, i, 1);
                if (buffer[i] == '|')
                {
                    this.Id = new string(buffer, 0, i);
                    return;
                }
            }

            buffer[100] = (char)0;
            var s = new string(buffer, 0, 100);
            throw new Exception("Buffer too small. First 100 characters: " + s);
        }

        public static IEnumerable<Airport> Deserialize(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string preamble = AirportBase.DeserializePreamble(reader);
                    if (preamble == "A")
                    {
                        var airport = new Airport();
                        airport.Deserialize(reader);
                        yield return airport;
                    }
                }
            }
        }

        public string DeserializeReadNext(StreamReader reader)
        {
            for (int i = 0; i < buffer.Length; ++i)
            {
                reader.Read(buffer, i, 1);
                if (buffer[i] == '|')
                {
                    return new string(buffer, 0, i);
                }
            }

            buffer[100] = (char)0;
            var s = new string(buffer, 0, 100);
            throw new Exception("Buffer too small. First 100 characters: " + s);
        }

        public static string DeserializePreamble(StreamReader reader)
        {
            var pre = new char[1];
            reader.Read(pre, 0, 1);
            return new string(pre);
        }
    }
}
