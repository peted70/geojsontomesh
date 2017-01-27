using System;
using FullSerializer;

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
