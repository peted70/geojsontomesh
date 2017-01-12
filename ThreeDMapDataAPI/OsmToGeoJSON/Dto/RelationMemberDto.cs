using System;
using System.Collections.Generic;

namespace OsmToGeoJSON.Dto
{
    public class RelationMemberDto : ICloneable
    {
        public string Type { get; set; }
        public long Ref { get; set; }
        public string Role { get; set; }

        public double? Lat { get; set; }

        public double? Lon { get; set; }

        public List<CoordinatesDto> Geometry { get; set; }
        
        public object Clone()
        {
            return new RelationMemberDto { Type = Type, Ref = Ref, Role = Role };
        }
    }
}