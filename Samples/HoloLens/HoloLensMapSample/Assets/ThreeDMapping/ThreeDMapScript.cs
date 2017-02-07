using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Helpers;
using FullSerializer;
using UnityEngine;
using UnityEngine.Networking;
using Interfaces;

public class ThreeDMapScript : MonoBehaviour
{
    [Header("Latitude Max")]
    public float maxLat = 51.5140574994f;
    [Header("Longitude Max")]
    public float maxLon = -0.1145303249f;
    [Header("Latitude Min")]
    public float minLat = 51.5073134351f;
    [Header("Longitude Min")]
    public float minLon = -0.1295164166f;

    [Header("Building Level Height")]
    [Range(5.0f, 20.0f)]
    public float BuildingLevelHeight = 15.0f;

    private const double MESH_SCALAR = 0.01;

    private float progress = 0.0f;
    private bool _useProjector = false;

    private Texture2D _satelliteTexture;
    private MetadataRootobject _tileMetadata = null;
    private GameObject _tilePlane = null;

    private List<EditorCoroutine> _coRoutines = new List<EditorCoroutine>();

    private static List<CompletionHandler> Handlers = new List<CompletionHandler>();

    private GameObject _mapContainer;
    private Bounds _floorPlaneBounds;

    private Material _mapMaterial;

    private IProgress _progress;
    private IUpdateHandler _updateHandler;
    private IDialog _dialog;

    private static void WhenDone(string name, Action<object> fn, params EditorCoroutine[] routines)
    {
        Handlers.Insert(0, new CompletionHandler(name, fn, routines));
    }

    private void UpdateProgress(float incr, string msg)
    {
        progress = incr;
        _progress.UpdateProgress(msg, progress);
    }

    public void Load(IProgress progress, IUpdateHandler updateHandler, IDialog dialog)
    {
        _progress = progress;
        _updateHandler = updateHandler;
        _dialog = dialog;

        _coRoutines.Clear();
        Handlers.Clear();

        updateHandler.HookUpdate(EditorUpdate);
        UpdateProgress(0.02f, "Initialising..");

        string urlBase = "http://localhost:8165";
        string urlPath = "api/mapping/";
        string urlQuery = string.Format("maxLat={0}&maxLon={1}&minLat={2}&minLon={3}", maxLat, maxLon, minLat, minLon);

        string geomUrl = urlBase + "/" + urlPath + "geoJson?" + urlQuery;

        var geomCoroutine = new EditorCoroutine("Geom Loader", geomUrl);
        _coRoutines.Add(geomCoroutine);

        string imgUrl = urlBase + "/" + urlPath + "image?" + urlQuery;

        var imageCoroutine = new EditorCoroutine("Satellite Image Loader", imgUrl);
        _coRoutines.Add(imageCoroutine);

        string mdUrl = urlBase + "/" + urlPath + "metadata?" + urlQuery;
        var imageMetadataCoroutine = new EditorCoroutine("Image Metadata Loader", mdUrl);
        _coRoutines.Add(imageMetadataCoroutine);

        // If we are using the texture projector then these can be done in parallel
        //WhenDone("Load Geom", LoadGeoJSON, geomCoroutine);
        WhenDone("Load Map Image", LoadMapImage, imageCoroutine);
        //WhenDone("Load Map Metadata", LoadMapMetadata, imageMetadataCoroutine);

        // For UV texturing we need to load the metadata image up before we can load
        // the building geometry so we can generate the UVs inside the mesh building 
        // routines
        WhenDone("Geom and Metadata", LoadGeomAndMapImageMetadata, imageMetadataCoroutine, geomCoroutine);
        WhenDone("All Loading", LoadingComplete, imageCoroutine, imageMetadataCoroutine, geomCoroutine);

        RecreateMapContainer();

        // Create the tile plane..
        _tilePlane = new GameObject();
        _tilePlane.name = "Tile Plane";
        _tilePlane.AddComponent(typeof(MeshFilter));
        _tilePlane.AddComponent(typeof(MeshRenderer));
        var shader = Shader.Find("HoloToolkit/Unlit Configurable");
        if (shader == null)
        {
            Debug.Log("Error: HoloToolkit/Unlit Configurable Shader not available.");
            return;
        }

        _mapMaterial = new Material(shader);
        _tilePlane.GetComponent<MeshRenderer>().sharedMaterial = _mapMaterial;

        UpdateProgress(0.04f, "Initialising..");
    }

    private void LoadGeomAndMapImageMetadata(object obj)
    {
        var list = obj as List<EditorCoroutine>;
        var co = list.First() as EditorCoroutine;
        MapImageMetadataLoadingDone((UnityWebRequest)co.GetCurrent());
        var geomCo = list[1] as EditorCoroutine;
        GeoJsonLoadingDone((UnityWebRequest)geomCo.GetCurrent());
    }

    private void LoadMapMetadata(object obj)
    {
        var list = obj as List<EditorCoroutine>;
        var co = list.First() as EditorCoroutine;
        MapImageMetadataLoadingDone((UnityWebRequest)co.GetCurrent());
    }

    private void LoadMapImage(object obj)
    {
        var list = obj as List<EditorCoroutine>;
        var co = list.First() as EditorCoroutine;
        MapImageLoadingDone((UnityWebRequest)co.GetCurrent());
    }

    private void LoadGeoJSON(object obj)
    {
        var list = obj as List<EditorCoroutine>;
        var co = list.First() as EditorCoroutine;
        GeoJsonLoadingDone((UnityWebRequest)co.GetCurrent());
    }

    private void LoadingComplete(object obj)
    {
        CreateTexture((int)_floorPlaneBounds.size.x, (int)_floorPlaneBounds.size.z, _tileMetadata);

        _mapMaterial.mainTexture = _satelliteTexture;
        _coRoutines.Clear();

        UpdateProgress(1.0f, "Done");
        _progress.Clear();
        _updateHandler.UnhookUpdate(EditorUpdate);
    }

    /// <summary>
    /// Set up the map texture. The texture will be projected orthographically onto
    /// the geometry and tile plane so we need to work out the correct orthographic 
    /// size to use. We need the size (in Unity coords of the tile floor plane) and also the
    /// map image and associated metadata. The metadata will tell us which sub rect of the 
    /// supplied image we want to project onto the Tile Floor plane.
    /// </summary>
    /// <param name="TilePlaneWidth"></param>
    /// <param name="TilePlaneHeight"></param>
    /// <param name="TileMetadata"></param>
    private void CreateTexture(int TilePlaneWidth, int TilePlaneHeight, MetadataRootobject TileMetadata)
    {
        var bbox = _tileMetadata.resourceSets[0].resources[0].bbox;

        var bMinLat = bbox[0];
        var bMinLon = bbox[1];
        var bMaxLat = bbox[2];
        var bMaxLon = bbox[3];

        float propLon = (bMaxLon - bMinLon) / (maxLon - minLon);
        float propLat = (bMaxLat - bMinLat) / (maxLat - minLat);

        var prop = propLat < propLon ? propLat : propLon;
        int orthoSize = 0;
        if (propLat < propLon)
        {
            orthoSize = (int)(prop * TilePlaneHeight / 2.0f);
        }
        else
        {
            orthoSize = (int)(prop * TilePlaneWidth / 2.0f);
        }

        if (_useProjector == true)
        {
            // Don't want to call this until all  of the data is loaded..
            CreateProjector(_satelliteTexture, orthoSize, _tilePlane);
        }
    }

    private void MapImageMetadataLoadingDone(UnityWebRequest obj)
    {
        LoadMetadata(obj.downloadHandler.text);
    }

    private void MapImageLoadingDone(UnityWebRequest www)
    {
        _satelliteTexture = new Texture2D(2, 2);
        _satelliteTexture.LoadImage(www.downloadHandler.data);
    }

    private void RecreateMapContainer()
    {
        //// If we have an existing child object named MapContainer delete it
        var previousContainer = gameObject.transform.FindChild("MapContainer");
        if (previousContainer)
        {
            DestroyImmediate(previousContainer.gameObject);
        }

        GameObject containerGameObject = new GameObject("MapContainer");
        containerGameObject.transform.parent = gameObject.transform;
        _mapContainer = containerGameObject;
    }

    private void GeoJsonLoadingDone(UnityWebRequest res)
    {
        float[][][] TileBounds = new float[][][]
        {
            new float[][]
            {
                new float[] { maxLon, maxLat, 0.0f },
                new float[] { minLon, minLat, 0.0f },
            }
        };

        UpdateProgress(0.1f, "Loaded Geo JSON");

        Debug.Log(res.downloadHandler.text);
        var data = ParseData(res.downloadHandler.text);

        Debug.Log("Number of features = " + data.features.Count());

        var buildings = data.features.Where(f => f.properties != null
                            && f.properties.tags != null
                            && f.properties.tags.building != null);

        // Use the centre of the tile bounding box
        var tb = GetBoundingBox(TileBounds);

        UpdateProgress(0.15f, "Loading Building Data");
        ProcessBuildings(buildings, tb, _mapContainer);
        UpdateProgress(0.9f, "Creating Floor");
        GenerateFloorPlane(tb, _mapContainer);
    }

    public void CreateProjector(Texture tex, int orthoSize, GameObject parent)
    {
        var go = new GameObject("Satellite Image Projector");
        go.transform.Rotate(new Vector3(1, 0, 0), 90);

        go.transform.parent = parent.transform;
        var proj = go.AddComponent<Projector>();
        proj.orthographic = true;
        proj.orthographicSize = orthoSize;
        proj.nearClipPlane = -10;
        proj.farClipPlane = 10;

        var shader = Shader.Find("Projector/Multiply");
        if (shader == null)
        {
            Debug.Log("Error: Projector/Multiply Shader not available.");
            return;
        }

        var mat = new Material(shader);
        mat.SetTexture("_ShadowTex", tex);

        proj.material = mat;
    }

    private void EditorUpdate()
    {
        try
        {
            // Loop through the co-routines..
            foreach (var co in _coRoutines)
            {
                co.tick();
            }

            for (int i = Handlers.Count() - 1; i >= 0; --i)
            {
                var handler = Handlers[i];
                if (handler.IsCompleted())
                {
                    Debug.Log("Notification Handler " + handler.Name + " Complete");
                    handler.Execute();
                    Handlers.RemoveAt(i);
                }
            }
        }
        catch (Exception ex)
        {
            _coRoutines.Clear();

            UpdateProgress(1.0f, "Done");
            _progress.Clear();
            _updateHandler.UnhookUpdate(EditorUpdate);
            _dialog.DisplayDialog("Error", ex.Message);
        }
    }

    void LoadMetadata(string geoJsonData)
    {
        fsSerializer serializer = new fsSerializer();
        fsData data = null;
        data = fsJsonParser.Parse(geoJsonData);

        // step 2: deserialize the data
        serializer.TryDeserialize(data, ref _tileMetadata).AssertSuccessWithoutWarnings();
        Debug.Log(data);
    }

    private void GenerateFloorPlane(Bounds? tb, GameObject container)
    {
        // Now generate a plane and texture it with a map image..
        var poly = tb.Value.ToPolygonFromBounds();

        // Centre on the origin..
        var polyArray = poly.ToList().Select(p => (p.ToVector3xz() - tb.Value.center).ToVector2xz());
        var planeMesh = Triangulator.CreateMesh(polyArray.ToArray());
        _floorPlaneBounds = planeMesh.bounds;

        {
            Vector3[] old = planeMesh.vertices;
            int[] triangles = planeMesh.triangles;
            Vector3[] verts = new Vector3[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                verts[i] = old[triangles[i]];
                triangles[i] = i;
            }
        }

        Vector3[] vertices = planeMesh.vertices;

        {
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = MapCoordToUV(vertices[i].ToVector2xz(), _tileMetadata);
            }
            planeMesh.uv = uvs;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices[i] * (float)MESH_SCALAR;
        }
        planeMesh.vertices = vertices;
        planeMesh.RecalculateNormals();
        planeMesh.RecalculateBounds();
        var planeBounds = planeMesh.bounds;
        _tilePlane.GetComponent<MeshFilter>().mesh = planeMesh;
        _tilePlane.transform.parent = container.transform;
    }

    /// <summary>
    /// Take a 2D map coordinate (in lat/lon) and convert to a UV coordinate which will
    /// index into the map image tile
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    Vector2 MapCoordToUV(Vector2 coord, MetadataRootobject tiledata)
    {
        // bbox of the tile image in lat/lon
        var bbox = _tileMetadata.resourceSets[0].resources[0].bbox;

        var bMinLat = bbox[0];
        var bMinLon = bbox[1];
        var bMaxLat = bbox[2];
        var bMaxLon = bbox[3];

        var min = GM.LatLonToMeters(bMinLat, bMinLon);
        var max = GM.LatLonToMeters(bMaxLat, bMaxLon);

        double lon = coord.x + min.x + 0.5 * (max.x - min.x);
        double lat = coord.y + min.y + 0.5 * (max.y - min.y);

        double u = (lon - min.x) / (max.x - min.x);
        double v = (lat - min.y) / (max.y - min.y);

        return new Vector2((float)u, (float)v);
    }

    private void ProcessBuildings(IEnumerable<Feature> buildings, Bounds? tb, GameObject container)
    {
        //int buildingCount = 0;
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
                    int numLevels = 1;
                    if (!string.IsNullOrEmpty(building.properties.tags.building_levels))
                    {
                        numLevels = int.Parse(building.properties.tags.building_levels);
                    }
                    var mesh = Triangulator.CreateMesh(verts.ToArray(), numLevels * BuildingLevelHeight);
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

                    var dist = (bound.center - tb.Value.center);

                    Vector3[] vertxs = mesh.vertices;
                    Vector2[] uvs = new Vector2[vertxs.Length];

                    for (int i = 0; i < uvs.Length; i++)
                    {
                        var xz = vertxs[i].ToVector2xz();
                        xz.x += dist.x;
                        xz.y += dist.z;
                        uvs[i] = MapCoordToUV(xz, _tileMetadata);
                    }

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = vertices[i] * (float)MESH_SCALAR;
                    }

                    mesh.vertices = vertices;

                    mesh.uv = uvs;
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    g.GetComponent<MeshFilter>().mesh = mesh;

                    // also, translate the building in y by half of its height..
                    //dist.y += 
                    g.transform.Translate(dist * (float)MESH_SCALAR);

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
                    g.GetComponent<MeshRenderer>().sharedMaterial = _mapMaterial;
                    g.transform.parent = geomContainer.transform;
                }
            }
            catch (Exception)
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
                var bnds = ret.Value;
                bnds.Encapsulate(latLon);
                ret = bnds;
            }
        }
        return ret;
    }
}

internal class CompletionHandler
{
    private List<EditorCoroutine> _list;
    private Action<object> _fn;
    private string _name;
    public string Name { get { return _name; } }

    public CompletionHandler(string name, Action<object> fn, params EditorCoroutine[] list)
    {
        _name = name;
        _fn = fn;
        for (int i=0;i<list.Length;i++)
        {
            if (_list == null)
                _list = new List<EditorCoroutine>();
            _list.Add(list[i]);
        }
    }

    public bool IsCompleted()
    {
        return _list.TrueForAll(ec => ec.IsComplete());
    }

    public void Execute()
    {
        // Want to pass a list of data for each coroutine here..
        _fn(_list);
    }
}

public class EditorCoroutine : IEnumerable
{
    private bool _complete;
    private string _url;
    private string _name;

    public bool IsComplete() { return _complete;  }

    public object GetCurrent()
    {
        return _iter.Current;
    }

    public EditorCoroutine(string name, string http)
    {
        _name = name;
        _url = http;
        _iter = GetEnumerator();
    }

    private IEnumerator _iter;
    private Action<UnityWebRequest> _done;

    public void tick()
    {
        if (_complete)
            return;

        if (!_iter.MoveNext())
        {
            var www = _iter.Current as UnityWebRequest;
            if (www == null || !www.isDone)
                return;
            if (www.isError)
            {
                Debug.Log(www.error);
                throw new Exception(www.error);
            }
            else if (www.isDone)
            {
                Debug.Log("CoRoutine: " + _name + " Done.");
                _complete = true;
            }
        }
    }

    public IEnumerator GetEnumerator()
    {
        UnityWebRequest www = UnityWebRequest.Get(_url);
        yield return www.Send();
        yield return www;
    }
}

