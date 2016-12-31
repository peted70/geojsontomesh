using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GeoJsonRoot
{
    public string type { get; set; }
    public string generator { get; set; }
    public string copyright { get; set; }
    public DateTime timestamp { get; set; }
    public Feature[] features { get; set; }
}

[Serializable]
public class Feature
{
    public string type { get; set; }
    public string id { get; set; }
    public Properties properties { get; set; }
    public Geometry geometry { get; set; }
}

[Serializable]
public class Properties
{
    public string id { get; set; }
    public string addrhousename { get; set; }
    public string addrhousenumber { get; set; }
    public string addrpostcode { get; set; }
    public string amenity { get; set; }
    public string atm { get; set; }
    public string building { get; set; }
    public string building_levels { get; set; }
    public string name { get; set; }
    public string sourceaddrpostcode { get; set; }
}

[Serializable]
public class Geometry
{
    public string type { get; set; }
    public float[][][] coordinates { get; set; }
    //public List<List<List<float>>> coordinates { get; set; }
}
