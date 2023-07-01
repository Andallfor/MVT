using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class facility
{
    public readonly string name;
    public facilityRepresentation representation {get; private set;}
    public facilityData data;

    public planet parent {get; private set;}

    public geographic geo {get {return data.geo;}}

    public facility(string name, planet parent, facilityData data, representationData rData) {
        if (master.allFacilities.Exists(x => x.name == name)) Debug.LogWarning("Duplicate facility detected");

        this.name = name;
        this.data = data;
        this.parent = parent;

        loadPhysicalData(rData);
        registerForEvents();

        master.allFacilities.Add(this);
        master.requestJsonQueueUpdate();
    }

    public void updatePosition(object sender, EventArgs args) {
        bool forceHide = !exists(master.time);
        representation.updatePos(parent, data.alt, forceHide);
    }

    public void addAntenna(antennaData ad) {
        data.antennas.Add(ad);
        representation.addAntennaFromParent(ad);
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

    public void setParent(planet p)
    {
        this.parent = p;
        this.representation.gameObject.transform.SetParent(p.representation.gameObject.transform);

        // move into representation?
        float r = 100f / (float) master.scale;
        this.representation.gameObject.transform.localScale = new Vector3(r, r, r);
    }

    public void destroy() {
        representation.destroy();
        parent.onPositionUpdate -= updatePosition;
        master.updateScheduling -= updateScheduling;
    }

    public bool containsAntenna(string name) => this.data.antennas.Exists(x => x.name == name);

    public override int GetHashCode() => name.GetHashCode();
}

public class antennaData {
    public geographic geo;
    public facility parent;
    public double alt, diameter, centerFreq, gPerT, priority;
    public string name, network, freqBand;
    public int payload, maxRate;
    public double groundPriority;
    public double serviceLevel;
    public string servicePeriod;
    public double schedulePriority;
    public List<scheduling> schedules;

    public antennaData(facility parent, int payload, string antenna, double diameter, string freqBand, double centerFreq, geographic geo, double alt, double gPerT, int maxRate, string network, double priority)
    {
        this.parent = parent;
        this.payload = payload;
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

    public antennaData(facility parent, string antenna, geographic geo, double groundPriority)
    {
        this.parent = parent;
        this.name = antenna;
        this.geo = geo;
        this.groundPriority = groundPriority;
    }

    public antennaData(facility parent, string antenna, geographic geo, double schedulePriority, double serviceLevel, string servicePeriod)
    {
        this.parent = parent;
        this.name = antenna;
        this.geo = geo;
        this.schedulePriority = schedulePriority;
        this.serviceLevel = serviceLevel;
        this.servicePeriod = servicePeriod;
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
