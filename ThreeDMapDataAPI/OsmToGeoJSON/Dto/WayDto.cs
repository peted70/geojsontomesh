using System.Collections.Generic;

namespace OsmToGeoJSON.Dto
{
    public class WayDto : ElementDto
    {
        public CoordinatesDto Center { get; set; }

        public BoundsDto Bounds { get; set; }

        public List<CoordinatesDto> Geometry { get; set; }

        public override string Type { get { return "way"; } }
        public List<long> Nodes { get; set; }
        
        new public WayDto Clone()
        {
            return (WayDto)CloneImpl();
        }

        protected virtual ElementDto CloneImpl()
        {
            var copy = (WayDto)base.CloneImplementation();
            copy.Center = (CoordinatesDto)Center.Clone();
            copy.Nodes = new List<long>(Nodes);
            return copy;
        }
    }
}