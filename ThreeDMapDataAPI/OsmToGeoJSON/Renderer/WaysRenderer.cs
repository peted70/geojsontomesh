using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

namespace OsmToGeoJSON.Renderer
{
    public class WaysRenderer   : IWaysRenderer
    {
        private readonly Dictionary<string, object> _additionalPolygonFeatures = new Dictionary<string, object>();

        private readonly IFeaturePropertyBuilder _featurePropertyBuilder;
        private Dictionary<string, object> _polygonFeatures;

        public WaysRenderer( Dictionary<string, object> additionalPolygonFeatures = null)
            : this(new PolygonFeaturesLoader(), new FeaturePropertyBuilder())
        {
            _additionalPolygonFeatures = additionalPolygonFeatures ?? new Dictionary<string, object>();
            _additionalPolygonFeatures.ToList().ForEach(x => _polygonFeatures.Add(x.Key, x.Value));
        }

        public WaysRenderer(IPolygonFeaturesLoader polygonFeaturesLoader, IFeaturePropertyBuilder featurePropertyBuilder)
        {
            _featurePropertyBuilder = featurePropertyBuilder;
            _polygonFeatures =  polygonFeaturesLoader.Load();
            
        }

        public List<Feature> Render(List<Way> ways)
        {
            var features = new List<Feature>();
            foreach (var way in ways.OrderBy(x => x.Id))
            {
                if ((way.Nodes == null || way.Nodes.Count == 0)
                   || (way.IsAnInnerWithoutAnOuter && !way.HasInterestingTags)
                   || ((way.HasParent && ((Relation)way.Parent).HasBeenRendered)
                    && ((way.HasParent && ((Relation)way.Parent).IsSimpleMultiPolygon) 
                            || (way.HasParent && way.IsGeometryIncomplete) 
                            || (way.IsMultipolygonOutline && !way.HasInterestingTags) 
                            || (way.HasParent && !way.HasInterestingTags)))) continue;
                //way.IsGeometryIncomplete = false;
                
                var coordinates = new List<Coordinates>();
                foreach (var nodeId in way.Nodes)
                {
                    if (!way.ResolvedNodes.ContainsKey(nodeId)) continue;
                    var node = way.ResolvedNodes[nodeId];
                    coordinates.Add(new Coordinates { Lat = node.Lat.Value, Lon = node.Lon.Value });
                    
                }
                if (coordinates.Count <= 1 && way.Center == null)
                {
                    Debug.WriteLine("Way {0}/{1} ignored because it contains too few nodes", way.Type, way.Id);
                    continue;
                }

                IGeometryObject geometry = null;
                if (coordinates.Count == 1)
                {
                    geometry = GetPointFor(way.ResolvedNodes.Values.First());
                }
                else
                {
                    geometry = GetGeometryForCoordinates(coordinates, way);
                }

                
                var feature = new Feature(geometry, _featurePropertyBuilder.GetProperties(way));
                feature.Id = string.Format("{0}/{1}", way.Type, way.Id);
                features.Add(feature);
            }
            return features;
        }

        private IGeometryObject GetPointFor(Node node)
        {
            return new Point(new GeographicPosition(node.Lat.Value, node.Lon.Value));
        }

        private IGeometryObject GetGeometryForCoordinates(List<Coordinates> coordinates, Way way)
        {
            var wayType = "LineString"; // default
            IGeometryObject geography = null;
            if (!string.IsNullOrEmpty(way.Nodes[0])   // way has its nodes loaded
                 && way.Nodes.First() == way.Nodes.Last() // ... and forms a closed ring
                && ((way.Tags.Count > 0
                && IsPolygonFeature(way.Tags))
                || way.IsBoundsPlaceHolder))
            {
                wayType = "Polygon";
                geography = ConvertToPolygon(coordinates);
            }
            else
            {
                geography = ConverToLineString(coordinates);
            }
            return geography;
        }

        private bool IsPolygonFeature(Dictionary<string, object> tags)
        {
            if (tags.ContainsKey("area") && tags["area"].ToString() == "no") return false;

            foreach (var tag in tags)
            {
                var val = tag.Value;

                // continue with next if tag is unknown or not "categorizing"
                if (!_polygonFeatures.ContainsKey(tag.Key)) continue;

                var pfk = _polygonFeatures[tag.Key];

                // continue with next if tag is explicitely un-set ("building=no")
                if (tag.Key == "building" && val.ToString() == "no") continue;

                var boolValue = pfk as bool?;
                var tagBoolValue = tag.Value as bool?;


                if (boolValue.HasValue && boolValue.Value)
                {
                    // if tag can be convert to boolean, it should be compared
                    bool result;
                    var sVal = tag.Value.ToString().ToLower();
                    if (!Boolean.TryParse(sVal, out result))
                    {
                        if (sVal == "yes")
                        {
                            result = true;
                        }
                        else if (sVal == "no")
                        {
                            result = false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    
                    if (result == boolValue.Value) return true; 
                    
                   
                    return false;
                }

                var dictionaryValue = pfk as Dictionary<string, object>;
                if (dictionaryValue == null) continue;
                if (dictionaryValue.ContainsKey("included_values"))
                {
                    var dict = (Dictionary<string, object>)dictionaryValue["included_values"];
                    if (dict.ContainsKey(val.ToString()) && Convert.ToBoolean(dict[val.ToString()]) == true) return true;
                }

                if (dictionaryValue.ContainsKey("excluded_values"))
                {
                    var dict = (Dictionary<string, object>)dictionaryValue["excluded_values"];
                    if (dict.ContainsKey(val.ToString()) && dict[val.ToString()].Equals(true)) return false;
                    return true;
                }

            }
            return false;
        }

        private static IGeometryObject ConverToLineString(List<Coordinates> coordinates)
        {
            return new LineString(coordinates.Select(c => new GeographicPosition(c.Lat, c.Lon)).ToList<IPosition>());
        }

        private static Polygon ConvertToPolygon(List<Coordinates> coordinates)
        {
            var lineString =
                new LineString(coordinates.Select(c => new GeographicPosition(c.Lat, c.Lon)).ToList<IPosition>());
            return new Polygon(new[] { lineString }.ToList());
        }
    }
}