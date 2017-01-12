using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using OsmToGeoJSON.Util;

namespace OsmToGeoJSON.Renderer
{
    public class RelationRenderer : IRelationRenderer
    {
        private readonly IFeaturePropertyBuilder _featurePropertyBuilder;
        private readonly IRingOrganiser _ringOrganiser;
        private readonly IClusterCleanser _clusterCleanser;

        public RelationRenderer() : this(new FeaturePropertyBuilder(), new RingOrganiser(), new ClusterCleanser())
        {
            
        }

        public RelationRenderer(IFeaturePropertyBuilder featurePropertyBuilder, IRingOrganiser ringOrganiser, IClusterCleanser clusterCleanser)
        {
            _featurePropertyBuilder = featurePropertyBuilder;
            _ringOrganiser = ringOrganiser;
            _clusterCleanser = clusterCleanser;
        }

        public List<Feature> Render(List<Relation> relations)
        {
            var featureList = new List<Feature>();
            foreach (var relation in relations)
            {
                if (!relation.IsMultiPolygon || relation.Members.All(x => x.Role != "outer")) continue;
                var ways = relation.ResolvedWays;
                if (!relation.IsSimpleMultiPolygon)
                {
                    var feature = ConstructMultiPolygon(relation, ways.Values.ToList());
                    if (feature != null) featureList.Add(feature);
                }
                    
                else
                {
//                    foreach (var member in relation.Members)
//                    {
                    var outerWayRef = relation.Members.Where(m => m.Role == "outer").ToList()[0].Ref;

                    if (!ways.ContainsKey(outerWayRef))
                    {
                        Debug.WriteLine("Multipolygon relation{0}/{1} ignored because outerway  is missing", relation.Type, relation.Id);
                        continue;
                    }
                    var outerway = ways[outerWayRef];
                    Feature feature = ConstructMultiPolygon(outerway, ways.Values.ToList());

                    if (feature == null)
                    {
                        Debug.WriteLine("Multipolygon relation{0}/{1} ignored because it has invalid geometry", relation.Type, relation.Id);
                        continue;
                    }
                    featureList.Add(feature);

//                    }
                }
                relation.HasBeenRendered = true;

            }
            return featureList;
        }

        private Feature ConstructMultiPolygon(Element element, IEnumerable<Way> ways)
        {
            var multiPolygonGeoemtryType = element is Way ? "way" : "relation";
            var validWays = ways.Where(x => x.IsGeometryIncomplete == false).ToList();
            var outerRings = _ringOrganiser.AssignToRings(validWays.Where(w => w.RoleInRelation == "outer").ToList());
            var innerRings = _ringOrganiser.AssignToRings(validWays.Where(w => w.RoleInRelation == "inner").ToList());
            var clusters = new List<Cluster>();
            outerRings.ToList().ForEach(x => clusters.Add(new Cluster(new [] {x})));

            foreach (var innerRing in innerRings)
            {
                var matchingOuterIndex = FindOuterIndex(innerRing, outerRings);
                if (matchingOuterIndex != -1)
                    clusters[matchingOuterIndex].Add(innerRing);
                else
                {
                    Debug.WriteLine("Multipolygon{0}/{1} contains an inner ring with no container outer", multiPolygonGeoemtryType, element.Id);
                }
            }

            _clusterCleanser.SanitzeClusters(clusters);

            if (clusters.Count == 0 || clusters.All(c => !c.Any()))
            {
                Debug.WriteLine("Multipolygon{0}/{1} contains no coordinates", multiPolygonGeoemtryType, element.Id);
                return null;
            }

            IGeometryObject geometryObject = clusters.Count == 1 ? ConvertToPolygon(clusters[0]) : ConvertToMultiPolygon(clusters);
            var properties = _featurePropertyBuilder.GetProperties(element);
            return new Feature(geometryObject, properties) { Id = string.Format("{0}/{1}", multiPolygonGeoemtryType, element.Id) };
        }

        private static IGeometryObject ConvertToMultiPolygon(List<Cluster> clusters)
        {
            var polygons = new List<Polygon>();
            foreach (var cluster in clusters)
            {
                polygons.AddRange(ConvertToPolygons(cluster));
            }
            return new MultiPolygon(polygons);
        }

        private static Polygon ConvertToPolygon(Cluster cluster)
        {
            var lines = new List<LineString>();
            foreach (var ring in cluster)
            {
                lines.Add(new LineString(ring.Select(n => new GeographicPosition(n.Lat.Value, n.Lon.Value)).ToList<IPosition>()));
            }
            return new Polygon(lines);
        }

        private static List<Polygon> ConvertToPolygons(Cluster cluster)
        {
            var polygons = new List<Polygon>();
            foreach (var ring in cluster)
            {
                polygons.Add(new Polygon((new [] { new LineString(ring.Select(n => new GeographicPosition(n.Lat.Value, n.Lon.Value)).ToList<IPosition>())}).ToList()));
            }
            return polygons;
        }


        private static int FindOuterIndex(IEnumerable<Node> inner, List<Ring> outerRings)
        {
            var copyInner = new List<Node>(inner);
            for (int i = 0; i < outerRings.Count; i++)
            {
                var outer = outerRings.ToArray()[i];
                if (Geometry.PolygonIntersectsPolygon(outer, copyInner)) return i;
            }
            return -1;
        }
    }
}