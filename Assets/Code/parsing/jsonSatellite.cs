using System.Collections;
using System.Collections.Generic;

public struct jsonSatelliteStruct
{
    public string name;
    public jsonTimelineStruct positions;
    public jsonSatelliteRepresentationStruct representation;
    public jsonBodyStruct bodyData;
}

public struct jsonSatelliteRepresentationStruct
{
    public string modelPath, materialPath;
}