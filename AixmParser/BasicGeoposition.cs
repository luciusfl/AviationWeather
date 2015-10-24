namespace Windows.Devices.Geolocation
{
    // BasicGeoposition is only supported for Phone 8.1, so create a dummy class 
    // with the same siganture.

    public struct BasicGeoposition
    {
        public double Altitude;

        public double Latitude;

        public double Longitude;
    }
}
