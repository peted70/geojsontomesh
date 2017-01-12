using System.Collections.Generic;

namespace OsmToGeoJSON
{
    public class RelationMember
    {
        public string Type { get; set; }
        public string Ref { get; set; }
        public string Role { get; set; }

        public double? Lat { get; set; }

        public double? Lon { get; set; }

        public List<Coordinates> Geometry { get; set; }

       
    }
}