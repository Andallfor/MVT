using System.Collections;
using System.Collections.Generic;

public struct jsonFacilityStruct
{
    public string name, parentName;
    public jsonGeographicStruct geo;
    public jsonFacilityRepresentationStruct representation;
}

public struct jsonFacilityRepresentationStruct
{
    public string modelPath, materialPath;
}