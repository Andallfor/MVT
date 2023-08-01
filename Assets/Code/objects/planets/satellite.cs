using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class satellite : body
{
    public satelliteRepresentation representation {get; private set;}
    public satelliteData data;
    public trailRenderer tr;

    public satellite(string name, satelliteData data, representationData rData) {
        if (master.allSatellites.Exists(x => x.name == name)) Debug.LogWarning("Duplicate satellite detected");

        base.name = name;
        base.positions = data.positions;
        this.data = data;
        base.init(rData);

        master.allSatellites.Add(this);
        master.requestJsonQueueUpdate();

        tr = new trailRenderer(name, representation.gameObject, positions, this);
    }

    private protected override void loadPhysicalData(representationData rData) {representation = new satelliteRepresentation(name, rData, this);}

    public override void updatePosition(object sender, EventArgs args)
    {
        localPos = pos = data.positions.find(master.time);
        if (!ReferenceEquals(parent, null)) pos += parent.pos;

        representation.setPosition(pos - master.currentPosition - master.referenceFrame, !data.positions.exists(master.time));
        if (planetOverview.instance.active) representation.setRadius(general.camera.orthographicSize * 0.2f * (master.scale / 2.0) / 4.0);

        base.updateChildren();
    }
    public override position requestPosition(Time t) => requestPosition(t.julian);

    public override position requestPosition(double julian) {
        position p = data.positions.find(julian);
        if (!ReferenceEquals(parent, null)) p += parent.requestPosition(julian);
        return p;
    }
    public override void updateScale(object sender, EventArgs args) {}
}

public class satelliteData
{
    public Timeline positions {get; private set;}

    public satelliteData(string positionPath, double timestep) {this.positions = csvParser.loadPlanetCsv(positionPath, timestep);}
    public satelliteData(TextAsset positionAsset, double timestep) {this.positions = csvParser.loadPlanetCsv(positionAsset, timestep);}
    public satelliteData(Timeline positions) {this.positions = positions;}
}