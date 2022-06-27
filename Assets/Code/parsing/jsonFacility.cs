using System.Collections;
using System.Collections.Generic;

public struct jsonFacilityStruct
{
    public List<jsonAntennaStruct> antennas;
    public string name, parent;
    public jsonGeographicStruct geo;
    public jsonFacilityRepresentationStruct representation;
}

public struct jsonAntennaStruct {
    public string name, parentName, groundStation, network, freqBand;
    public double alt, diameter, centerFreq, gPerT, priority;
    public int payload, maxRate;
    public jsonGeographicStruct geo;
}

public struct jsonFacilityRepresentationStruct
{
    public string modelPath, materialPath;
}