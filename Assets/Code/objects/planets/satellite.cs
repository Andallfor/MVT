using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class satellite : body, IJsonFile<jsonSatelliteStruct>
{
    public satelliteRepresentation representation {get; private set;}
    private satelliteData data;

    public satellite(string name, satelliteData data, representationData rData)
    {
        base.name = name;
        base.positions = data.positions;
        this.data = data;
        base.init(rData);

        master.allSatellites.Add(this);
        master.requestJsonQueueUpdate();
    }

    private protected override void loadPhysicalData(representationData rData) {representation = new satelliteRepresentation(name, rData);}

    public override void updatePosition(object sender, EventArgs args)
    {
        localPos = pos = data.positions.find(master.time);
        if (!ReferenceEquals(parent, null)) pos += parent.pos;

        representation.setPosition(pos - master.currentPosition - master.referenceFrame, !data.positions.exists(master.time));
        if (planetOverview.usePlanetOverview) representation.setRadius((master.scale / 2.0) / 8.0);

        base.updateChildren();
    }
    public override position requestLocalPosition(Time t)
    {
        position p = data.positions.find(t);
        if (!ReferenceEquals(parent, null)) p += parent.requestLocalPosition(t);
        return p;
    }
    public override void updateScale(object sender, EventArgs args) {}

    public new jsonSatelliteStruct requestJsonFile()
    {
        return new jsonSatelliteStruct() {
            name = this.name,
            positions = data.positions.requestJsonFile(),
            representation = representation.requestJsonFile(),
            bodyData = base.requestJsonFile()};
    }
}

public class satelliteData
{
    public Timeline positions {get; private set;}

    public satelliteData(string positionPath, double timestep) {this.positions = csvParser.loadPlanetCsv(positionPath, timestep);}
    public satelliteData(TextAsset positionAsset, double timestep) {this.positions = csvParser.loadPlanetCsv(positionAsset, timestep);}
    public satelliteData(Timeline positions) {this.positions = positions;}
}