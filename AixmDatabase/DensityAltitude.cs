using System;

namespace metar
{
    public partial class METAR
    {
        public bool DensityAltitudeAvailable
        {
            get
            {
                if (this.temp_cSpecified && this.dewpoint_cSpecified &&
                    this.elevation_mSpecified)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public double DensityAltitude
        {
            get
            {
                if (this.DensityAltitudeAvailable)
                {
                    return AirportInformation.Common.Atmosphere.DensityAlitude(this);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
