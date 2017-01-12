using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

namespace OsmToGeoJSON.Renderer
{
    public class NodeRenderer : INodeRenderer
    {
        private readonly IFeaturePropertyBuilder _featurePropertyBuilder;

        public NodeRenderer() : this(new FeaturePropertyBuilder())
        {
            
        }

        public NodeRenderer(IFeaturePropertyBuilder featurePropertyBuilder)
        {
            _featurePropertyBuilder = featurePropertyBuilder;
        }

        public List<Feature> Render(List<Node> nodes)
        {
            var features = new List<Feature>();
            foreach (var node in nodes)
            {
                // Probably doesn't need to be rendered
                if (node.Parent != null &&
                    !node.IsReferencedByARelation &&
                    !node.HasInterestingTags) continue;
                if (!node.Lat.HasValue || !node.Lon.HasValue) continue;
                if (node.IsBounds|| node.IsAGeometryNode) continue;
                var point = new Point(new GeographicPosition(node.Lat.Value, node.Lon.Value));
                var feature = new Feature(point, _featurePropertyBuilder.GetProperties(node), string.Format("{0}/{1}", node.Type, node.Id));
                if (node.IsCentrePlaceHolder) feature.Properties["geometry"] = "center";
                features.Add(feature);
            }
            return features;
        }
    }
}