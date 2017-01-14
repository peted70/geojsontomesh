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
    public Tags tags { get; set; }
}

[Serializable]
public class Geometry
{
    public string type { get; set; }
    public float[][][] coordinates { get; set; }
    //public List<List<List<float>>> coordinates { get; set; }
}

[Serializable]
public class Tags
{
    public string amenity { get; set; }
    public string building { get; set; }
    public string description { get; set; }
    public string fee { get; set; }
    public string layer { get; set; }
    public string name { get; set; }
    public string parking { get; set; }
    public string surface { get; set; }
    public string wheelchair { get; set; }
    public string addrcity { get; set; }
    public string addrhousenumber { get; set; }
    public string addrpostcode { get; set; }
    public string addrstreet { get; set; }
    public string fixme { get; set; }
    public string addrhousename { get; set; }
    public string building_levels { get; set; }
    public string note { get; set; }
    public string opening_hours { get; set; }
    public string phone { get; set; }
    public string shop { get; set; }
    public string source { get; set; }
    public string denomination { get; set; }
    public string nameko { get; set; }
    public string religion { get; set; }
    public string surveydate { get; set; }
    public string website { get; set; }
    public string line { get; set; }
    public string naptanAtcoCode { get; set; }
    public string networksubway { get; set; }
    public string networktrain { get; set; }
    public string _operator { get; set; }
    public string public_transport { get; set; }
    public string railway { get; set; }
    public string _ref { get; set; }
    public string wikipedia { get; set; }
    public string zone { get; set; }
}
