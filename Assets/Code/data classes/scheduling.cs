using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scheduling
{
    public satellite connectingTo;
    public List<schedulingTime> times = new List<schedulingTime>();

    public scheduling(satellite connectingTo, List<schedulingTime> times)
    {
        this.connectingTo = connectingTo;
        this.times = times;
    }
}

public readonly struct schedulingTime
{
    public readonly double start, end;
    public bool between(double j) => j >= start && j <= end;

    public schedulingTime(double start, double end)
    {
        this.start = start;
        this.end = end;
    }
}
