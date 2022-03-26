using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class Timeline : ITimeline
{
    private TimelinePosition tp;
    private TimelineKepler tk;
    private TimelineSelection selection;
    public Timeline(Dictionary<double, position> data, double timestep)
    {
        tp = new TimelinePosition(data, timestep);
        selection = TimelineSelection.positions;
    }

    public Timeline(double semiMajorAxis, double eccentricity, double inclination, double argOfPerigee, double longOfAscNode, double meanAnom, double mass, double startingEpoch, double mu)
    {
        tk = new TimelineKepler(semiMajorAxis, eccentricity, inclination, argOfPerigee, longOfAscNode, meanAnom, mass, startingEpoch, mu);
        selection = TimelineSelection.kepler;
    }

    public jsonTimelineStruct requestJsonFile()
    {
        if (selection == TimelineSelection.positions) return tp.requestJsonFile();
        else return tk.requestJsonFile();
    }

    public position find(Time t)
    {
        if (selection == TimelineSelection.positions) return tp.find(t);
        else return tk.find(t);
    }
}

public class TimelinePosition : ITimeline
{
    private Dictionary<double, position> data = new Dictionary<double, position>();
    private List<double> index = new List<double>();
    private TimelineComparer tlc;
    private double timestep;
    private double first, last;

    // assumes data is sorted
    public TimelinePosition(Dictionary<double, position> data, double timestep)
    {
        this.data = data;
        this.tlc = new TimelineComparer(timestep);
        this.timestep = timestep;
        this.index = data.Keys.ToList();

        this.first = index.First();
        this.last = index.Last();
    }

    public position find(Time t)
    {
        if (data.ContainsKey(t.julian)) return data[t.julian];
        if (t.julian < first) return data[index[0]];
        if (t.julian > last) return data[index[index.Count - 1]];

        int timeIndex = this.index.BinarySearch(t.julian, tlc);
        double closestTime = index[timeIndex];

        double difference = t.julian - closestTime;
        double percent = Math.Abs(difference) / (timestep);

        if (difference < 0) return position.interpLinear(data[index[timeIndex - 1]], data[closestTime], 1 - percent);
        else return position.interpLinear(data[closestTime], data[index[timeIndex + 1]], percent);
    }

    public jsonTimelineStruct requestJsonFile()
    {
        Dictionary<double, jsonPositionStruct> pos = new Dictionary<double, jsonPositionStruct>();
        foreach (KeyValuePair<double, position> kvp in data)
        {
            pos.Add(kvp.Key, kvp.Value.requestJsonFile());
        }

        return new jsonTimelineStruct() {
            timestep = this.timestep,
            positions = pos};
    }
}

public class TimelineComparer : IComparer<double>
{
    private readonly double timestep;

    public TimelineComparer(double timestep) {this.timestep = timestep;}

    public int Compare(double x, double y)
    {
        double difference = Math.Abs(x - y);
        if (difference <= timestep) return 0;
        else return x.CompareTo(y);
    }
}

public class TimelineKepler : ITimeline, IJsonFile<jsonTimelineStruct>
{
    private double semiMajorAxis, eccentricity, inclination, argOfPerigee, longOfAscNode, mu, startingEpoch, meanAngularMotion, orbitalPeriod, startingMeanAnom;
    private planet referenceFrame;

    private const double degToRad = Math.PI / 180.0;
    private const double radToDeg = 180.0 / Math.PI;

    public position find(Time t)
    {
        // https://drive.google.com/file/d/1so93guuhCO94PEU8vFvDLv_-k9vJBcFs/view
        // offset by elevation angle, in order to make the kepler and earth share the same up direction
        double meanAnom = (controller.earth.representation.gameObject.transform.eulerAngles.x + startingMeanAnom - 360.0 * (meanAngularMotion * (master.time.julian - this.startingEpoch))) * degToRad;
        double EA = meanAnom;
        for (int i = 0; i < 50; i++) EA = meanAnom + eccentricity * Math.Sin(EA);

        double trueAnom = 2.0 * Math.Atan(Math.Sqrt((1.0 + eccentricity) / (1.0 - eccentricity)) * Math.Tan(EA / 2.0));

        double radius = (semiMajorAxis * (1 - eccentricity * eccentricity)) / (1 + eccentricity * Math.Cos(trueAnom));
        double p = semiMajorAxis * (1 - eccentricity * eccentricity);
        double h = Math.Sqrt(mu * semiMajorAxis * (1 - eccentricity * eccentricity));

        position pos = new position(
            radius * (Math.Cos(longOfAscNode) * Math.Cos(argOfPerigee + trueAnom) - Math.Sin(longOfAscNode) * Math.Sin(argOfPerigee + trueAnom) * Math.Cos(inclination)),
            radius * (Math.Sin(longOfAscNode) * Math.Cos(argOfPerigee + trueAnom) + Math.Cos(longOfAscNode) * Math.Sin(argOfPerigee + trueAnom) * Math.Cos(inclination)),
            radius * (Math.Sin(inclination) * Math.Sin(argOfPerigee + trueAnom)));
        
        position vel = new position(
            ((pos.x * h * eccentricity) / (radius * p)) * Math.Sin(trueAnom) - (h / radius) * (Math.Cos(longOfAscNode) * Math.Sin(argOfPerigee + trueAnom) + Math.Sin(longOfAscNode) * Math.Cos(argOfPerigee + trueAnom) * Math.Cos(inclination)),
            ((pos.y * h * eccentricity) / (radius * p)) * Math.Sin(trueAnom) - (h / radius) * (Math.Sin(longOfAscNode) * Math.Sin(argOfPerigee + trueAnom) - Math.Cos(longOfAscNode) * Math.Cos(argOfPerigee + trueAnom) * Math.Cos(inclination)),
            ((pos.z * h * eccentricity) / (radius * p)) * Math.Sin(trueAnom) + (h / radius) * (Math.Sin(inclination) * Math.Cos(argOfPerigee + trueAnom)));
        
        if (double.IsNaN(pos.x)) return new position(0, 0, 0);

        position rot = controller.earth.representation.gameObject.transform.eulerAngles;
        return (pos.rotate(rot.y * degToRad, 0, 0));
    }

    // give in degrees
    public TimelineKepler(double semiMajorAxis, double eccentricity, double inclination, double argOfPerigee, double longOfAscNode, double meanAnom, double mass, double startEpoch, double mu)
    {
        this.semiMajorAxis = semiMajorAxis;
        this.eccentricity = eccentricity;
        this.inclination = inclination * degToRad;
        this.argOfPerigee = argOfPerigee * degToRad;
        this.longOfAscNode = longOfAscNode * degToRad;
        this.mu = mu;
        this.startingMeanAnom = meanAnom;
        this.orbitalPeriod = 2.0 * Math.PI * Math.Sqrt((semiMajorAxis * semiMajorAxis * semiMajorAxis) / mu);
        this.meanAngularMotion = 86400.0 / (this.orbitalPeriod);
        this.startingEpoch = startEpoch;
    }

    public jsonTimelineStruct requestJsonFile() => new jsonTimelineStruct();
}

public interface ITimeline
{
    position find(Time t);
    jsonTimelineStruct requestJsonFile();
}

internal enum TimelineSelection
{
    positions, kepler
}