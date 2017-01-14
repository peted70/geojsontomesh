using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Helpers;
using FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class ThreeDMapScript : MonoBehaviour 
{
    public float maxLat = 51.5140574994f;
    public float maxLon = -0.1145303249f;
    public float minLat = 51.5073134351f;
    public float minLon = -0.1295164166f;

    private IEnumerator co;

    // Use this for initialization
    void Start ()
    {
		
	}

    public IEnumerator LoadMapAsync()
    {
        // call this 
        string url = string.Format("http://localhost:8165/api/mapping?maxLat={0}&maxLon={1}&minLat={2}&minLon={3}",
            maxLat, maxLon, minLat, minLon);

        UnityWebRequest myWr = UnityWebRequest.Get(url);
        yield return myWr.Send();
        yield return myWr;
    }

    public void Load()
    {
        EditorApplication.update += EditorUpdate;
        EditorUtility.DisplayProgressBar("Loading Map Data..", "", 5.0f);

        co = LoadMapAsync();
    }

    private void EditorUpdate()
    {
        float[][][] TileBounds = new float[][][]
            {
                        new float[][]
                        {
                            new float[] { maxLon, maxLat, 0.0f },
                            new float[] { minLon, minLat, 0.0f },
                        }
            };

        if (!co.MoveNext())
        {
            Debug.Log("In move next");

            var res = co.Current as UnityWebRequest;
            if (res == null || !res.isDone)
                return;

            if (res.isError)
            {
                Debug.Log("Error");
            }
            else
            {
                EditorUtility.DisplayProgressBar("Loaded Data..", "", 10.0f);

                Debug.Log(res.downloadHandler.text);
                var data = ParseData(res.downloadHandler.text);

                Debug.Log("Number of features = " + data.features.Count());

                var buildings = data.features.Where(f => f.properties != null
                                    && f.properties.tags != null
                                    && f.properties.tags.building != null);


                // Need to know the centre of the 'tile' so we can create the buildings at
                // the origin and then translate to the correct positions.
                // When we are calling an API we will know the lat lon of the requested tile
                // until then we can use a bounding box around all of the buildings..
                //var tb = GetBoundingBoxForBuilding(buildings.First());
                //foreach (var building in buildings)
                //{
                //    if (building == buildings.First())
                //        continue;
                //    var bounds = GetBoundingBoxForBuilding(building);
                //    if (bounds == null)
                //        continue;
                //    tb.Value.Encapsulate(bounds.Value);
                //}

                // Use the centre of the tile bounding box
                var tb = GetBoundingBox(TileBounds);

                // If we have an existing child object named MapContainer delete it
                var previousContainer = gameObject.transform.FindChild("MapContainer");
                if (previousContainer)
                {
                    DestroyImmediate(previousContainer.gameObject);
                }

                GameObject containerGameObject = new GameObject("MapContainer");
                containerGameObject.transform.parent = gameObject.transform;

                EditorUtility.DisplayProgressBar("Loading Building Data", "", 15.0f);
                ProcessBuildings(buildings, tb, containerGameObject);
                EditorUtility.DisplayProgressBar("Creating Floor", "", 90.0f);
                GenerateFloorPlane(tb, containerGameObject);
            }

            EditorUtility.DisplayProgressBar("Done", "", 100.0f);

            EditorUtility.ClearProgressBar();

            EditorApplication.update -= EditorUpdate;
        }
    }

    private void GenerateFloorPlane(Bounds? tb, GameObject container)
    {
        // Now generate a plane and texture it with a map image..
        var poly = tb.Value.ToPolygonFromBounds();

        // Centre on the origin..
        var polyArray = poly.ToList().Select(p => (p.ToVector3xz() - tb.Value.center).ToVector2xz());
        var planeMesh = Triangulator.CreateMesh(polyArray.ToArray());

        {
            Vector3[] old = planeMesh.vertices;
            int[] triangles = planeMesh.triangles;
            Vector3[] vertices = new Vector3[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                vertices[i] = old[triangles[i]];
                triangles[i] = i;
            }
        }

        //planeMesh.uv = uvs;
        planeMesh.RecalculateNormals();
        planeMesh.RecalculateBounds();

        var gobj = new GameObject();
        gobj.name = "Tile Plane";
        gobj.AddComponent(typeof(MeshFilter));
        gobj.AddComponent(typeof(MeshRenderer));
        gobj.GetComponent<MeshFilter>().mesh = planeMesh;
        Material mt = new Material(Shader.Find("Standard"));
        mt.color = Color.white;
        gobj.GetComponent<MeshRenderer>().material = mt;
        gobj.transform.parent = container.transform;
    }

    private void ProcessBuildings(IEnumerable<Feature> buildings, Bounds? tb, GameObject container)
    {
        int buildingCount = 0;
        var geomContainer = new GameObject("Geometry");
        geomContainer.transform.parent = container.transform;

        foreach (var building in buildings)
        {
            try
            {
                //if (++buildingCount != 1)
                //    continue;

                if (building.geometry.coordinates == null)
                    continue;

                foreach (var coords in building.geometry.coordinates)
                {
                    // Create Vector2 vertices
                    List<Vector2> verts = new List<Vector2>();

                    foreach (var crd in coords)
                    {
                        // remember lat/lon are reversed in geoJSON
                        verts.Add(GM.LatLonToMeters(crd[1], crd[0]));
                    }

                    // Create the Vector3 vertices - 
                    // So, x corresponds to latitude 
                    // z corresponds to longitude
                    List<Vector3> verts3 = verts.Select(v => v.ToVector3xz()).ToList();

                    // Calculate axis aligned bounding box for polygon
                    var bound = new Bounds(verts3[0], Vector3.zero);
                    foreach (var vtx in verts3)
                    {
                        bound.Encapsulate(vtx);
                    }

                    // Move the polygon to the origin by subtracting the centre of the bounding box from 
                    // each vertex.
                    verts = verts3.Select(vtx => (vtx - bound.center).ToVector2xz()).ToList();

                    Debug.Log(string.Format("NUM VERTS (before triangulation) = {0}", verts.Count));

                    // Work out the height of the building either from height or estimate from 
                    // number of levels or failing that, just one level..
                    const float oneLevel = 16.0f;
                    int numLevels = 1;
                    if (!string.IsNullOrEmpty(building.properties.tags.building_levels))
                    {
                        numLevels = int.Parse(building.properties.tags.building_levels);
                    }
                    var mesh = Triangulator.CreateMesh(verts.ToArray(), numLevels * oneLevel);
                    var g = new GameObject();
                    g.AddComponent(typeof(MeshFilter));
                    g.AddComponent(typeof(MeshRenderer));

                    // If you want to get flat shading then you need a unique vertex for each 
                    // face. I haven't processed the data like that so have run this code as a post 
                    // process to generate the required vertices 
                    // (see http://answers.unity3d.com/questions/798510/flat-shading.html)
                    Vector3[] oldVerts = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    Vector3[] vertices = new Vector3[triangles.Length];
                    for (int i = 0; i < triangles.Length; i++)
                    {
                        vertices[i] = oldVerts[triangles[i]];
                        triangles[i] = i;
                    }
                    mesh.vertices = vertices;
                    mesh.triangles = triangles;

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    g.GetComponent<MeshFilter>().mesh = mesh;

                    var dist = bound.center - tb.Value.center;

                    // also, translate the building in y by half of its height..
                    //dist.y += 
                    g.transform.Translate(dist);

                    Material m = new Material(Shader.Find("Standard"));
                    m.color = Color.grey;
                    if (building.properties.tags != null)
                    {
                        if (!string.IsNullOrEmpty(building.properties.tags.name))
                            g.name = building.properties.tags.name;
                        else if (!string.IsNullOrEmpty(building.properties.tags.addrhousename))
                        {
                            g.name = building.properties.tags.addrhousename;
                        }
                        else if (!string.IsNullOrEmpty(building.properties.tags.addrstreet))
                        {
                            g.name = building.properties.tags.addrstreet;
                        }
                    }
                    g.GetComponent<MeshRenderer>().material = m;
                    g.transform.parent = geomContainer.transform;
                }
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("Building load failed"));
            }
        }
    }

    private GeoJsonRoot ParseData(string geoJsonData)
    {
        fsSerializer serializer = new fsSerializer();
        serializer.Config.GetJsonNameFromMemberName = GetJsonNameFromMemberName;
        serializer.AddConverter(new Converter());
        fsData data = null;
        data = fsJsonParser.Parse(geoJsonData);

        // step 2: deserialize the data
        GeoJsonRoot deserialized = null;

        serializer.TryDeserialize(data, ref deserialized).AssertSuccessWithoutWarnings();
        return deserialized;
    }

    private string GetJsonNameFromMemberName(string arg1, MemberInfo arg2)
    {
        int idx = arg1.IndexOf("_");
        if (idx != -1)
        {
            arg1 = arg1.Replace("_", ":");
        }
        return arg1;
    }

    private Bounds? GetBoundingBoxForBuilding(Feature building)
    {
        if (building.geometry == null || building.geometry.coordinates == null)
            return null;
        return GetBoundingBox(building.geometry.coordinates);
    }

    private Bounds? GetBoundingBox(float[][][] coords)
    {
        Bounds? ret = null;
        foreach (var coord in coords)
        {
            // remember lat/lon are reversed in geoJSON
            var latLons = coord.Select(fs => GM.LatLonToMeters(fs[1], fs[0]).ToVector3xz());
            if (ret == null)
                ret = new Bounds(latLons.First(), Vector3.zero);
            foreach (var latLon in latLons)
            {
                var toset = latLon;
                var bnds = ret.Value;
                bnds.Encapsulate(latLon);
                ret = bnds;
            }
        }
        return ret;
    }
}
