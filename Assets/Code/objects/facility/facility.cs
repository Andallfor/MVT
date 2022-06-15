using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class facility : IJsonFile<jsonFacilityStruct>
{
    public readonly string name;
    public facilityRepresentation representation {get; private set;}
    private facilityData data;

    private planet parent;

    public geographic geo {get {return data.geo;}}
    public planet facParent {get {return parent;}}
    
    public facility(string name, planet parent, facilityData data, representationData rData)
    {
        this.name = name;
        this.data = data;
        this.parent = parent;

        loadPhysicalData(rData);
        registerForEvents();

        master.allFacilites.Add(this);
        master.requestJsonQueueUpdate();
    }

    public void updatePosition(object sender, EventArgs args) {representation.updatePos(parent, parent.radius);}

    public void updateScheduling(object sender, EventArgs args) {if (!ReferenceEquals(data.schedules, null)) representation.drawSchedulingConnections(data.schedules);}

    private void loadPhysicalData(representationData rData) {representation = facilityRepresentation.createFacility(name, data.geo, parent.representation.gameObject, rData);}
    private void registerForEvents()
    {
        parent.onPositionUpdate += updatePosition;
        master.updateScheduling += updateScheduling;
    }
    public void registerScheduling(List<scheduling> s) {this.data.schedules = s;}

    public jsonFacilityStruct requestJsonFile()
    {
        return new jsonFacilityStruct() {
            name = this.name,
            parentName = parent.name,
            groundStation = data.groundStation,
            network = data.network,
            freqBand = data.freqBand,
            alt = data.alt,
            diameter = data.diameter,
            centerFreq = data.centerFreq,
            gPerT = data.gPerT,
            payload = data.payload,
            maxRate = data.maxRate,
            priority = data.priority,
            geo = data.geo.requestJsonFile(),
            representation = representation.requestJsonFile()};
    }

    public void setParent(planet p)
    {
        this.parent = p;
        this.representation.transform.SetParent(p.representation.transform);

        // move into representation?
        float r = 100f / (float) master.scale;
        this.representation.gameObject.transform.localScale = new Vector3(r, r, r);
    }
}

public class facilityData
{
    public geographic geo;
    public List<scheduling> schedules;
    public double alt, diameter, centerFreq, gPerT, priority;
    public string name, groundStation, network, freqBand;
    public int payload, maxRate;

    public facilityData(int payload, string groundStation, string antenna, double diameter, string freqBand, double centerFreq, geographic geo, double alt, double gPerT, int maxRate, string network, double priority)
    {
        this.payload = payload;
        this.groundStation = groundStation;
        this.name = antenna;
        this.diameter = diameter;
        this.freqBand = freqBand;
        this.centerFreq = centerFreq;
        this.geo = geo;
        this.alt = alt;
        this.gPerT = gPerT;
        this.maxRate = maxRate;
        this.network = network;
        this.priority = priority;
    }
}
