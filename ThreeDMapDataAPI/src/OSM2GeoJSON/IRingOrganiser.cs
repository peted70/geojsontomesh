using System.Collections.Generic;

namespace OsmToGeoJSON
{
    public interface IRingOrganiser
    {
        List<Ring> AssignToRings(List<Way> ways);
    }
}