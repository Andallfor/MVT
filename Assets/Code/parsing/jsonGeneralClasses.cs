using System.Collections;
using System.Collections.Generic;

public struct jsonPositionStruct
{
    public double x, y, z;
}

public struct jsonTimelineStruct
{
    public Dictionary<double, jsonPositionStruct> positions;
    public double timestep;
}

public struct jsonBodyStruct
{
    public string parent;
    public string[] children;
}

public struct jsonGeographicStruct
{
    public double lat, lon;
}

public interface IJsonFile<T>
{
    T requestJsonFile();
}