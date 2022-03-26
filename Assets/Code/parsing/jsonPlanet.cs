using System.Collections;
using System.Collections.Generic;

public struct jsonPlanetStruct
{
    public double radius;
    public string name;
    public int planetType;
    public bool rotate;
    public jsonTimelineStruct positions;
    public jsonPlanetRepresentationStruct representationData;
    public jsonBodyStruct bodyData;
}

public struct jsonPlanetRepresentationStruct
{
    public string modelPath, materialPath;
}