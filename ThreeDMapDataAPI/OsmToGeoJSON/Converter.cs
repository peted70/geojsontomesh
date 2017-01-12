using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using OsmToGeoJSON.Dto;
using OsmToGeoJSON.Processors;
using OsmToGeoJSON.Renderer;

namespace OsmToGeoJSON
{
    public class Converter
    {
        public static Dictionary<string, object> UninterestingTags = new Dictionary<string, object>();
        private readonly INodesProcessor _nodesProcessor;
        private readonly IWaysProcessor _waysProcessor;
        private readonly IRelationsProcessor _relationsProcessor;
        private readonly INodeRenderer _nodeRenderer;
        private readonly IWaysRenderer _waysRenderer;
        private readonly IRelationRenderer _relationRenderer;

        public Converter(Dictionary<string, object> uninterestingTags = null, Dictionary<string, object> additionalPolygonFeatures = null)
            : this(new NodesProcessor(new TagClassifier(uninterestingTags)),
            new WaysProcessor(new TagClassifier(uninterestingTags)),
            new RelationsProcessor(new TagClassifier(uninterestingTags)), 
            new PolygonFeaturesLoader(), 
            new NodeRenderer(), 
            new WaysRenderer( additionalPolygonFeatures), 
            new RelationRenderer())
        {
            
        }

        public Converter(INodesProcessor nodesProcessor, IWaysProcessor waysProcessor, IRelationsProcessor relationsProcessor, IPolygonFeaturesLoader polygonFeaturesLoader, INodeRenderer nodeRenderer, IWaysRenderer waysRenderer, IRelationRenderer relationRenderer)
        {
            _nodesProcessor = nodesProcessor;
            _waysProcessor = waysProcessor;
            _relationsProcessor = relationsProcessor;
            _nodeRenderer = nodeRenderer;
            _waysRenderer = waysRenderer;
            _relationRenderer = relationRenderer;
        }

        public FeatureCollection OsmToFeatureCollection(string json)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            var overPassResponse = JsonConvert.DeserializeObject<OverpassResponseDto>(json, settings);
            overPassResponse.Elements = overPassResponse.Elements.Where(x => x.Type != "area").ToList();
            return overPassResponse.Elements.Count == 0 ? new FeatureCollection(new List<Feature>())  : OsmToFeatureCollection(overPassResponse);
        }

        public string OsmToGeoJSON(string osmJson)
        {
            var featureCollection = OsmToFeatureCollection(osmJson);
            var serializedData = JsonConvert.SerializeObject(featureCollection, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return  Fix(serializedData);
        }


        private static string Fix(string serializedData)
        {
            return
                serializedData
                    .Replace("]\"", "]")
                    .Replace("\"[", "[")
                    .Replace("}\"", "}")
                    .Replace("\"{", "{")
                    .Replace("\\\\\"", "\\\"")
                    .Replace("\\\"", "\"");
        }
        public FeatureCollection OsmToFeatureCollection(OverpassResponseDto overPassResponseDto)
        {
            var nodes = new List<Node>();
            var ways = new List<Way>();
            var relations = new List<Relation>();

            foreach (var elementDto in overPassResponseDto.Elements)
            {
                switch (elementDto.Type)
                {
                    case "node":
                        nodes.Add(((NodeDto)elementDto).ToDomain());
                        break;
                    case "way":
                        var newWay = ((WayDto)elementDto).ToDomain();
                        ways.Add(newWay);
                        break;
                    case "relation":
                        var newRelation = ((RelationDto)elementDto).ToDomain();
                        relations.Add(newRelation);
                        break;
                    default:
                        throw new Exception("Unknown element type");

                }
            }

            var featureCollection = ProcessFeatures(nodes, ways, relations);
            //var serializedData = JsonConvert.SerializeObject(featureCollection, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), NullValueHandling = NullValueHandling.Ignore });
            return featureCollection;
        }

        private FeatureCollection ProcessFeatures(List<Node> nodes, List<Way> ways, List<Relation> relations)
        {
            var relationsProcessingResult = _relationsProcessor.Process(relations, ways, nodes);
            var inputWays = ways.Concat(relationsProcessingResult.PseudoWays).ToList();
            var inputNodes = nodes.Concat(relationsProcessingResult.PseudoNodes).ToList();
            var wayProcessingResult = _waysProcessor.Process(inputWays, inputNodes);
            inputNodes = nodes.Concat(wayProcessingResult.PseudoNodes).Concat(relationsProcessingResult.PseudoNodes).ToList();
            var nodeProcessingResult = _nodesProcessor.BuildIndex(inputNodes, ways, relations);

            var proccessedWays = wayProcessingResult.Ways;
            var processedNodes = nodeProcessingResult.Nodes;
            var processedRelations = relationsProcessingResult.Relations;

            var renderedRelationFeatures = _relationRenderer.Render(processedRelations);
            var renderedNodeFeatures = _nodeRenderer.Render(processedNodes);
            var renderedWayFeatures = _waysRenderer.Render(proccessedWays);

            return new FeatureCollection(renderedRelationFeatures.Concat(renderedWayFeatures).Concat(renderedNodeFeatures).ToList());
        }
    }


}