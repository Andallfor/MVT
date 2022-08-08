using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class facility : IJsonFile<jsonFacilityStruct>
{
    public readonly string name;
    public facilityRepresentation representation {get; private set;}
    public facilityData data;

    private planet parent;

    public geographic geo {get {return data.geo;}}
    public planet facParent {get {return parent;}}

    public facility(string name, planet parent, facilityData data, representationData rData) {
        this.name = name;
        this.data = data;
        this.parent = parent;

        loadPhysicalData(rData);
        registerForEvents();

        master.allFacilites.Add(this);
        master.requestJsonQueueUpdate();
    }

    public void updatePosition(object sender, EventArgs args) {
        bool forceHide = !exists(master.time);
        representation.updatePos(parent, data.alt, forceHide);
    }

    public bool exists(Time t) => data.alwaysExist || (t > data.start &&  t < data.end);

    public void updateScheduling(object sender, EventArgs args) {representation.drawSchedulingConnections(data.antennas);}

    private void loadPhysicalData(representationData rData) {representation = new facilityRepresentation(name, data.antennas, data.geo, parent, rData);}
    private void registerForEvents()
    {
        parent.onPositionUpdate += updatePosition;
        master.updateScheduling += updateScheduling;
    }
    public void registerScheduling(string antenna, List<scheduling> s) {this.data.antennas.First(x => x.name == antenna).schedules = s;}

    public jsonFacilityStruct requestJsonFile()
    {
        List<jsonAntennaStruct> ants = new List<jsonAntennaStruct>();
        foreach (antennaData a in data.antennas) ants.Add(a.requestJsonFile());

        return new jsonFacilityStruct() {
            name = this.name,
            parent = parent.name,
            antennas = ants,
            geo = data.geo.requestJsonFile(),
            representation = representation.requestJsonFile()};
    }

    public void setParent(planet p)
    {
        this.parent = p;
        this.representation.gameObject.transform.SetParent(p.representation.gameObject.transform);

        // move into representation?
        float r = 100f / (float) master.scale;
        this.representation.gameObject.transform.localScale = new Vector3(r, r, r);
    }

    public bool containsAntenna(string name) => this.data.antennas.Exists(x => x.name == name);
}

public class antennaData : IJsonFile<jsonAntennaStruct> {
    public geographic geo;
    public double alt, diameter, centerFreq, gPerT, priority;
    public string name, parent, groundStation, network, freqBand;
    public int payload, maxRate;
    public double groundPriority;
    public double serviceLevel;
    public string servicePeriod;
    public double schedulePriority;
    public List<scheduling> schedules;

    public antennaData(int payload, string groundStation, string antenna, double diameter, string freqBand, double centerFreq, geographic geo, double alt, double gPerT, int maxRate, string network, double priority)
    {
        this.payload = payload;
        this.groundStation = groundStation;
        this.name = antenna;
        this.diameter = diameter;
        this.freqBand = freqBand;
        this.centerFreq = centerFreq;
        this.geo = geo;
        this.alt = alt * 1000.0; // current file we are given has them in meters
        this.gPerT = gPerT;
        this.maxRate = maxRate;
        this.network = network;
        this.priority = priority;
    }

    public antennaData(string groundStation, string antenna, geographic geo, double groundPriority)
    {

        this.groundStation = groundStation;
        this.name = antenna;
        this.geo = geo;
        this.groundPriority = groundPriority;
    }

    public antennaData(string groundStation, string antenna, geographic geo, double schedulePriority, double serviceLevel, string servicePeriod)
    {
        this.groundStation = groundStation;
        this.name = antenna;
        this.geo = geo;
        this.schedulePriority = schedulePriority;
        this.serviceLevel = serviceLevel;
        this.servicePeriod = servicePeriod;
    }

    public jsonAntennaStruct requestJsonFile() {
        return new jsonAntennaStruct() {
            name = this.name,
            parentName = this.parent,
            groundStation = this.groundStation,
            network = this.network,
            freqBand = this.freqBand,
            alt = this.alt,
            diameter = this.diameter,
            centerFreq = this.centerFreq,
            gPerT = this.gPerT,
            priority = this.priority,
            payload = this.payload,
            maxRate = this.maxRate,
            geo = this.geo.requestJsonFile()
        };
    }
}

public class facilityData
{
    public geographic geo;
    public string name;
    public double alt;
    public List<antennaData> antennas;
    public Time start {get; private set;}
    public Time end {get; private set;}
    public bool alwaysExist = true;

    // alt is in km
    public facilityData(string name, geographic geo, double alt, List<antennaData> antennas) {
        this.geo = geo;
        this.name = name;
        this.alt = alt;
        this.antennas = antennas;
    }

    public facilityData(string name, geographic geo, double alt, List<antennaData> antennas, Time start, Time end) {
        this.geo = geo;
        this.name = name;
        this.alt = alt;
        this.antennas = antennas;
        this.start = start;
        this.end = end;
        this.alwaysExist = false;
    }
}
