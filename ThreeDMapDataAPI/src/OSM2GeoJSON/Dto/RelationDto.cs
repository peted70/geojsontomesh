using System.Collections.Generic;
using System.Linq;

namespace OsmToGeoJSON.Dto
{
    public class RelationDto : ElementDto
    {
        public CoordinatesDto Center { get; set; }

        public BoundsDto Bounds { get; set; }
        public override string Type { get { return "relation"; } }
        public List<RelationMemberDto> Members { get; set; }

        new public RelationDto Clone()
        {
            return (RelationDto)CloneImpl();
        }

        protected virtual ElementDto CloneImpl()
        {
            var copy = (RelationDto)base.CloneImplementation();
            copy.Center = (CoordinatesDto)Center.Clone();
            copy.Members = Members.Select(m => (RelationMemberDto)m.Clone()).ToList();
            return copy;
        }
    }
}