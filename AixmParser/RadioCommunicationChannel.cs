using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirportInformation.ViewModels
{
    [DebuggerDisplay("Id = {Id}, Freq={Frequency}")]
    public sealed class RadioCommunicationChannel : AirportBase
    {
        public float Frequency { get; set; }
    }
}
