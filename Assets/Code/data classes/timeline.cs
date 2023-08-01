using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class Timeline : ITimeline
{
    private TimelinePosition tp;
    private TimelineKepler tk;
    public TimelineSelection selection {get; private set;}
    public Timeline(Dictionary<double, position> data, double timestep, bool alwaysExist = true)
    {
        tp = new TimelinePosition(data, timestep, alwaysExist);
        selection = TimelineSelection.positions;
    }

    public Timeline(double semiMajorAxis, double eccentricity, double inclination, double argOfPerigee, double longOfAscNode, double meanAnom, double mass, double startingEpoch, double mu)
    {
        tk = new TimelineKepler(semiMajorAxis, eccentricity, inclination, argOfPerigee, longOfAscNode, meanAnom, mass, startingEpoch, mu);
        selection = TimelineSelection.kepler;
    }

    public Timeline(double semiMajorAxis, double eccentricity, double inclination, double argOfPerigee, double longOfAscNode, double meanAnom, double mass, double startingEpoch, double mu, Time start, Time end)
    {
        tk = new TimelineKepler(semiMajorAxis, eccentricity, inclination, argOfPerigee, longOfAscNode, meanAnom, mass, startingEpoch, mu, start, end);
        selection = TimelineSelection.kepler;
    }

    /// <summary> if timeline is positions start and end is ignored </summary>
    public void enableExistanceTime(Time start, Time end) {
        if (selection == TimelineSelection.positions) tp.alwaysExist = false;
        else {
            tk.start = start;
            tk.end = end;
            tk.alwaysExist = false;
        }
    }

    public void disableExistanceTime() {
        if (selection == TimelineSelection.positions) tp.alwaysExist = true;
        else tp.alwaysExist = true;
    }

    public position find(Time t)
    {
        if (selection == TimelineSelection.positions) return tp.find(t.julian);
        else return tk.find(t.julian);
    }

    public position find(double julian) {
        if (selection == TimelineSelection.positions) return tp.find(julian);
        else return tk.find(julian);
    }

    public double findOrbitalPeriod()
    {
        if (selection == TimelineSelection.positions) throw new NotImplementedException("Cannot query orbital period for positional timeline");
        else return tk.findOrbitalPeriod();
    }

    public double returnSemiMajorAxis()
    {
        if (selection == TimelineSelection.positions) throw new NotImplementedException("Cannot query semi major axis for positional timeline");
        else return tk.returnSemiMajorAxis();
    }

    public bool exists(Time t) {
        if (selection == TimelineSelection.positions) return tp.exists(t);
        else return tk.exists(t);
    }

    public double tryGetStartTime() {
        if (selection == TimelineSelection.positions) return tp.first;
        else throw new NotImplementedException("Cannot query start time for kepler timeline");
    }
    public double tryGetEndTime() {
        if (selection == TimelineSelection.positions) return tp.last;
        else throw new NotImplementedException("Cannot query end time for kepler timeline");
    }
}

public class TimelinePosition : ITimeline
{
    private Dictionary<double, position> data = new Dictionary<double, position>();
    private List<double> index = new List<double>();
    private TimelineComparer tlc;
    private double timestep;
    public double first, last;
    public bool alwaysExist;

    // assumes data is sorted
    public TimelinePosition(Dictionary<double, position> data, double timestep, bool alwaysExist)
    {
        this.data = data;
        this.tlc = new TimelineComparer(timestep);
        this.timestep = timestep;
        this.index = data.Keys.ToList();
        this.alwaysExist = alwaysExist;

        this.first = index.First();
        this.last = index.Last();
    }


    public position find(double julian)
    {
        if (data.ContainsKey(julian)) return data[julian];
        if (julian <= first) return data[index[0]];
        if (julian >= last) return data[index[index.Count - 1]];

        double closestTime;
        int timeIndex;
        try { // TODO dont
            timeIndex = this.index.BinarySearch(julian, tlc);
            closestTime = index[timeIndex];
        } catch {
            timeIndex = this.index.BinarySearch(julian, tlc);
            Debug.Log(julian);
            Debug.Log(timeIndex);
            Debug.Log(index.Count);
            closestTime = index[timeIndex];
        }


        double difference = julian - closestTime;
        double percent = Math.Abs(difference) / (timestep);

        if (difference < 0) return position.interpLinear(data[index[timeIndex - 1]], data[closestTime], 1 - percent);
        else return position.interpLinear(data[closestTime], data[index[timeIndex + 1]], percent);
    }

    public bool exists(Time t) {
        if (alwaysExist) return true;
        else return t.julian > first && t.julian < last;
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

public class TimelineKepler : ITimeline
{
    private double semiMajorAxis, eccentricity, inclination, argOfPerigee, longOfAscNode, mu, startingEpoch, meanAngularMotion, orbitalPeriod, startingMeanAnom;
    public Time start, end;
    public bool alwaysExist = true;
    private planet referenceFrame;

    private const double degToRad = Math.PI / 180.0;
    private const double radToDeg = 180.0 / Math.PI;

    public double findOrbitalPeriod()
    {
        return Math.Sqrt(Math.Pow(semiMajorAxis / 149597870.69099998, 3)) * 365.25 * 580;
    }

    public double returnSemiMajorAxis()
    {
        return semiMajorAxis;
    }

    public position find(double julian) {
        double meanAnom = startingMeanAnom;

        if (julian == startingEpoch) meanAnom = startingMeanAnom;
        else meanAnom = startingMeanAnom + 86400.0 * (julian - startingEpoch) * Math.Sqrt((mu / Math.Pow(semiMajorAxis, 3)));

        double EA = meanAnom;

        double k = 1;
        double error = .0001;

        double y = 0;

        while (k > error) {
          y = meanAnom + eccentricity * Math.Sin(EA);
          k = Math.Abs(Math.Abs(EA) - Math.Abs(y));
          EA = y;
        }

        double trueAnom1 = (Math.Sqrt(1 - eccentricity * eccentricity) * Math.Sin(EA)) / (1 - eccentricity * Math.Cos(EA));
        double trueAnom2 = (Math.Cos(EA) - eccentricity) / (1 - eccentricity * Math.Cos(EA));

        double trueAnom = Math.Atan2(trueAnom1, trueAnom2);

        double theta = trueAnom + argOfPerigee;

        double radius = (semiMajorAxis * (1 - eccentricity * eccentricity)) / (1 + eccentricity * Math.Cos(trueAnom));

        double xp = radius * Math.Cos(theta);
        double yp = radius * Math.Sin(theta);

        position pos = new position(
            xp * Math.Cos(longOfAscNode) - yp * Math.Cos(inclination) * Math.Sin(longOfAscNode),
            xp * Math.Sin(longOfAscNode) + yp * Math.Cos(inclination) * Math.Cos(longOfAscNode),
            yp * Math.Sin(inclination));

        return pos.swapAxis();
    }

    public bool exists(Time t) {
        if (alwaysExist) return true;
        else return t > start && t < end;
    }

    /// <summary> give in degrees </summary>
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

    public TimelineKepler(double semiMajorAxis, double eccentricity, double inclination, double argOfPerigee, double longOfAscNode, double meanAnom, double mass, double startEpoch, double mu, Time start, Time end)
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
        this.start = start;
        this.end = end;
        this.alwaysExist = false;
    }
}

public interface ITimeline
{
    position find(double julian);
}

public enum TimelineSelection
{
    positions, kepler
}
