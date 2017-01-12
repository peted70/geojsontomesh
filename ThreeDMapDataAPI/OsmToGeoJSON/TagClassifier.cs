using System.Collections.Generic;
using System.Linq;

namespace OsmToGeoJSON
{
    public class TagClassifier : ITagClassifier
    {
        private readonly Dictionary<string, object> _additionalUninterestinTags;

        public TagClassifier(Dictionary<string, object> additionalUninterestinTags = null)
        {
            _additionalUninterestinTags = additionalUninterestinTags ?? new Dictionary<string, object>();
        }

        public bool AreInteresting(Dictionary<string, object> tags, Dictionary<string, object> tagsToIgnore = null, bool matchIgnoreValue = true)
        {
            return HasInterestingTags(tags, tagsToIgnore, matchIgnoreValue);
        }

        private bool HasInterestingTags(Dictionary<string, object> tags, Dictionary<string, object> tagsToIgnore = null, bool matchIgnoreValue = true)
        {
            if (tagsToIgnore == null) tagsToIgnore = new Dictionary<string, object>();
            var uninterestingTags = new[]
            {
                "source",
                "source_ref",
                "source:ref",
                "history",
                "attribution",
                "created_by",
                "tiger:county",
                "tiger:tlid",
                "tiger:upload_uuid"
            };

            uninterestingTags = uninterestingTags.Concat(_additionalUninterestinTags.Keys).ToArray();

            foreach (var tag in tags)
            {
                var lowerKey = tag.Key.ToLower();
                if (uninterestingTags.Contains(lowerKey) || 
                    (tagsToIgnore.ContainsKey(lowerKey) && (!matchIgnoreValue || (matchIgnoreValue && tagsToIgnore[lowerKey].Equals(tag.Value))))) continue;
                return true;
            }
            return false;

        }
    }
}