public class MetadataRootobject
{
    public string authenticationResultCode { get; set; }
    public string brandLogoUri { get; set; }
    public string copyright { get; set; }
    public Resourceset[] resourceSets { get; set; }
    public int statusCode { get; set; }
    public string statusDescription { get; set; }
    public string traceId { get; set; }
}

public class Resourceset
{
    public int estimatedTotal { get; set; }
    public Resource[] resources { get; set; }
}

public class Resource
{
    public string __type { get; set; }
    public float[] bbox { get; set; }
    public string imageHeight { get; set; }
    public string imageWidth { get; set; }
    public Mapcenter mapCenter { get; set; }
    public object[] pushpins { get; set; }
    public string zoom { get; set; }
}

public class Mapcenter
{
    public string[] coordinates { get; set; }
}
