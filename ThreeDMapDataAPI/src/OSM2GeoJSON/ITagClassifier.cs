using System.Collections.Generic;

namespace OsmToGeoJSON
{
    public interface ITagClassifier
    {
        bool AreInteresting(Dictionary<string, object> tags, Dictionary<string, object> tagsToIgnore = null, bool matchIgnoreValue = true);
    }
}