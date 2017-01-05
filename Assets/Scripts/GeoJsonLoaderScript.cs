using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Helpers;
using FullSerializer;
using UnityEngine;

public class GeoJsonLoaderScript : MonoBehaviour
{
    public class Converter : fsDirectConverter
    {
        public override Type ModelType
        {
            get
            {
                return typeof(Geometry);
            }
        }

        public override object CreateInstance(fsData data, Type storageType)
        {
            return new Geometry();
        }

        public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
        {
            var dict = data.AsDictionary;
            var type = dict["type"].AsString;
            Geometry value = (Geometry)instance;
            value.type = type;
            if (type == "Polygon")
            {
                float[][][] coordinates;

                var ret = DeserializeMember(dict, null, "coordinates", out coordinates);
                value.coordinates = coordinates;
                return ret;
            }
            if (dict["type"].AsString == "Point")
            {

            }

            return fsResult.Success;
        }

        public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
        {
            throw new NotImplementedException();
        }
    }

    private Bounds? GetBoundingBoxForBuilding(Feature building)
    {
        if (building.geometry == null || building.geometry.coordinates == null)
            return null;
        return GetBoundingBox(building.geometry.coordinates);
    }

    private Bounds? GetBoundingBox(float [][][] coords)
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

    // Use this for initialization
    void Start()
    {
        // MIN 51.579687, -0.341837
        // MAX 51.580780, -0.333930
        const float minLat = 51.579687f;
        const float maxLat = 51.580780f;
        const float minLon = -0.341837f;
        const float maxLon = -0.333930f;

        float[][][] TileBounds = new float[][][] 
        { 
            new float[][] 
            { 
                new float[] { maxLon, maxLat, 0.0f },
                new float[] { minLon, minLat, 0.0f },
            }
        };

        // Can get the OSM data using something like the following..
        var httpStr = @"http://overpass-api.de/api/interpreter?data=[out:json];(node[""building""](51.579687,-0.341837,51.580780,-0.333930);way[""building""](51.579687,-0.341837,51.580780,-0.333930);relation[""building""](51.579687,-0.341837,51.580780,-0.333930););out body;>;out skel qt;";

        //var geoJson = Resources.Load("santander") as TextAsset;
        var geoJson = Resources.Load("out") as TextAsset;

        fsSerializer serializer = new fsSerializer();
        serializer.Config.GetJsonNameFromMemberName = GetJsonNameFromMemberName;
        serializer.AddConverter(new Converter());
        fsData data = fsJsonParser.Parse(geoJson.text);

        // step 2: deserialize the data
        GeoJsonRoot deserialized = null;

        try
        {
            serializer.TryDeserialize(data, ref deserialized).AssertSuccessWithoutWarnings();
        }
        catch (Exception ex)
        {
            int x = 3;
        }

        var buildings = deserialized.features.Where(f => f.properties != null && f.properties.tags.building != null);

        // Need to know the centre of the 'tile' so we can create the buildings at
        // the origin and then translate to the correct positions.
        // When we are calling an API we will know the lat lon of the requested tile
        // until then we can use a bounding box around all of the buildings..
        var tileBounds = GetBoundingBoxForBuilding(buildings.First());
        foreach (var building in buildings)
        {
            if (building == buildings.First())
                continue;
            var bounds = GetBoundingBoxForBuilding(building);
            if (bounds == null)
                continue;
            tileBounds.Value.Encapsulate(bounds.Value);
        }

        // Use the centre of the tile bounding box

        int buildingCount = 0;

        foreach (var building in buildings)
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
                const float oneLevel = 30.0f;
                int numLevels = 1;
                if (!string.IsNullOrEmpty(building.properties.tags.building_levels))
                    numLevels = int.Parse(building.properties.tags.building_levels);
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

                var dist = tileBounds.Value.center - bound.center;

                // also, translate the building in y by half of its height..
                //dist.y += 
                g.transform.Translate(dist);

                Material m = new Material(Shader.Find("Standard"));
                m.color = Color.green;
                if (!string.IsNullOrEmpty(building.properties.name))
                    g.name = building.properties.name;
                g.GetComponent<MeshRenderer>().material = m;
                g.transform.parent = gameObject.transform;
            }
        }

        // Now generate a plane and texture it with a map image..
        var tb = GetBoundingBox(TileBounds);
        var poly = tb.Value.ToPolygonFromBounds();

        // Centre on the origin..
        var polyArray = poly.ToList().Select(p => (p.ToVector3xz() - tb.Value.center).ToVector2xz());
        var planeMesh = Triangulator.CreateMesh(polyArray.ToArray());

        // Set the UVs:
        Vector2[] uvs = new Vector2[4];

        uvs[0] = new Vector2(0.0f, 0.0f);
        uvs[1] = new Vector2(1.0f, 0.0f);
        uvs[2] = new Vector2(1.0f, 1.0f);
        uvs[3] = new Vector2(0.0f, 1.0f);

        planeMesh.uv = uvs;
        planeMesh.RecalculateNormals();
        planeMesh.RecalculateBounds();

        var gobj = new GameObject();
        gobj.name = "Tile Plane";
        gobj.AddComponent(typeof(MeshFilter));
        gobj.AddComponent(typeof(MeshRenderer));
        gobj.GetComponent<MeshFilter>().mesh = planeMesh;
        Material mt = new Material(Shader.Find("Standard"));
        mt.color = Color.blue;
        //gobj.GetComponent<MeshRenderer>().material = mt;
        Texture my_img = (Texture)Resources.Load("harrowtestimg");
        gobj.GetComponent<MeshRenderer>().material.mainTexture = my_img;
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

#if DELAUNAY

            // Create the Vector3 vertices
            List<Vector3> vertices3 = verts.Select(v => v.ToVector3xz()).ToList();
            Debug.Log(string.Format("NUM VERTS = {0}", vertices3.Count));

            // Calculate axis aligned bounding box for polygon
            var bounds = new Bounds(vertices3[0], Vector3.zero);
            foreach (var vtx in vertices3)
            {
                bounds.Encapsulate(vtx);
            }

            // Move the polygon to the origin by subtracting the centre of the bounding box from 
            // each vertex.
            vertices3 = vertices3.Select(vtx => vtx - bounds.center).ToList();

            // http://luminaryapps.com/blog/triangulating-3d-polygons-in-unity/
            Poly2Mesh.Polygon poly = new Poly2Mesh.Polygon();
            poly.outside = vertices3;

            // Set up game object with mesh;
            var go = Poly2Mesh.CreateGameObject(poly);

            // now let's have a go at extruding the polygon in the y-axis
            var orginalVerts = go.GetComponent<MeshFilter>().mesh.vertices;
            var vrts = new List<Vector3>(go.GetComponent<MeshFilter>().mesh.vertices);
            Debug.Log(string.Format("NUM VERTS (after triangulation) = {0}", vrts.Count));

            // using 30 for placeholder for building height for now..
            for (int n = 0; n < vrts.Count(); n++)
            {
                vrts[n] = new Vector3(vrts[n].x, vrts[n].y + 30, vrts[n].z);
            }

            var lst = orginalVerts.ToList();
            lst.AddRange(vrts);

            go.GetComponent<MeshFilter>().mesh.vertices = lst.ToArray();

            Debug.Log("verts start ------------------------------------------------");
            foreach (var vr in go.GetComponent<MeshFilter>().mesh.vertices)
            {
                Debug.Log(string.Format("{0}, {1}, {2}", vr.x, vr.y, vr.z));
            }
            Debug.Log("verts end ------------------------------------------------");

            var origTris = go.GetComponent<MeshFilter>().mesh.triangles;
            int numTris = origTris.Count();

            // Now update the indices as well..
            var tris = new List<int>(origTris);
            for (int n = 0;n < tris.Count(); n+=3)
            {
                tris[n] = origTris[n] + vrts.Count();
                tris[n+1] = origTris[n+2] + vrts.Count();
                tris[n+2] = origTris[n+1] + vrts.Count();
            }

            var newTris = origTris.ToList();
            newTris.AddRange(tris);
            go.GetComponent<MeshFilter>().mesh.triangles = newTris.ToArray();

            Debug.Log("tris start ------------------------------------------------");
            foreach (var tr in go.GetComponent<MeshFilter>().mesh.triangles)
            {
                Debug.Log(tr);
            }
            Debug.Log("tris end ------------------------------------------------");

            var meshTris = go.GetComponent<MeshFilter>().mesh.triangles.ToList();

            // Now add the walls - no more verts needed just indices..
            // (this won't work because we don't know if verts are on the polygon edge)
            int vrtsCount = lst.Count()/2;
            for (int v = 0; v < vrtsCount; v++)
            {
                if (v != 0)// && v != vrtsCount - 1)
                {
                    //continue;

                    int i1 = v;
                    int i2 = (i1 + 1) % (vrtsCount);
                    int i3 = i1 + vrtsCount;
                    int i4 = i2 + vrtsCount;

                    Debug.Log(string.Format("P1 - {0},{1},{2}", i1, i3, i4));
                    meshTris.Add(i1);
                    meshTris.Add(i3);
                    meshTris.Add(i4);

                    Debug.Log(string.Format("P2 - {0},{1},{2}", i1, i4, i2));
                    meshTris.Add(i1);
                    meshTris.Add(i4);
                    meshTris.Add(i2);
                }
#if BLAH

                int i1 = v;
                int i2 = v + vrtsCount;
                int i3 = (v + 1) % vrtsCount;

                Debug.Log(string.Format("P1 - {0},{1},{2}", i1, i2, i3));
                meshTris.Add(i1);
                meshTris.Add(i2);
                meshTris.Add(i3);

                int j1 = v + vrtsCount;
                int j2 = (v + (vrtsCount + 1)) % (vrtsCount * 2);
                int j3 = (v + 1) % (vrtsCount * 2);

                Debug.Log(string.Format("P2 - {0},{1},{2}", j1, j2, j3));
                meshTris.Add(j3);
                meshTris.Add(j2);
                meshTris.Add(j1);
#endif
            }
            //for (int v=0;v<vrts.Count()-1; v++)
            //{
            //    meshTris.Add(v);
            //    meshTris.Add(v + vrts.Count());
            //    meshTris.Add(v + 1);

            //    meshTris.Add(v + vrts.Count());
            //    meshTris.Add(v + vrts.Count() + 1);
            //    meshTris.Add(v + 1);
            //}

            //meshTris.Add(vrts.Count() - 1);
            //meshTris.Add(vrts.Count() - 1 + vrts.Count());
            //meshTris.Add(0);

            //meshTris.Add(vrts.Count() - 1 + vrts.Count());
            //meshTris.Add(vrts.Count());
            //meshTris.Add(0);

            // ----------------------------------------------

            go.GetComponent<MeshFilter>().mesh.triangles = meshTris.ToArray();

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            go.GetComponent<Renderer>().material = mat;
            go.transform.parent = gameObject.transform;

            go.GetComponent<MeshFilter>().mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().mesh.RecalculateNormals();
        }

#endif
    // Update is called once per frame
    void Update()
    {

    }
}
