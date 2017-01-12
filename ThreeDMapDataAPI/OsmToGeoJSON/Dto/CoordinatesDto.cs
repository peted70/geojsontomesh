using System;

namespace OsmToGeoJSON.Dto
{
    public class CoordinatesDto : ICloneable
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public object Clone()
        {
            return new CoordinatesDto {Lat = Lat, Lon = Lon};
        }
    }
}