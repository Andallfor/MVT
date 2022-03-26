using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class master
{
    public static planet sun;
    public static double scale {get {return _scale;}}
    public readonly static Time time = new Time(2459396.5, true);
    public static int currentTick = 0;

    public static position currentPosition = new position(0, 0, 0);
    public static position referenceFrame {
        get {
            if (ReferenceEquals(_referenceFrame, null)) return new position(0, 0, 0);
            else return _refFrameLast;
        }
    }
    private static body _referenceFrame;
    private static position _refFrameLast;

    private static double _scale = 1000;

    public static event EventHandler onScaleChange = delegate {};
    public static event EventHandler updatePositions = delegate {};
    public static event EventHandler updateScheduling = delegate {};
    public static event EventHandler updateJsonQueue = delegate {};

    public static void requestPositionUpdate() {updatePositions(null, EventArgs.Empty);}
    public static void requestSchedulingUpdate() {updateScheduling(null, EventArgs.Empty);}
    public static void requestScaleUpdate() {onScaleChange(null, EventArgs.Empty);}
    public static void requestJsonQueueUpdate() {updateJsonQueue(null, EventArgs.Empty);}

    public static List<planet> allPlanets = new List<planet>();
    public static List<satellite> allSatellites = new List<satellite>();
    public static List<facility> allFacilites = new List<facility>();

    public static void clearAllLines()
    {
        foreach (planet p in allPlanets)
        {
            LineRenderer lr = null;
            TrailRenderer tr = null;
            if (p.representation.gameObject.TryGetComponent<LineRenderer>(out lr)) lr.positionCount = 0;
            if (p.representation.gameObject.TryGetComponent<TrailRenderer>(out tr)) tr.Clear();
        }

        foreach (facility f in allFacilites)
        {
            LineRenderer lr = null;
            if (f.representation.gameObject.TryGetComponent<LineRenderer>(out lr)) lr.positionCount = 0;
        }
    }

    public static bool pause {
        get {return _pause;}
        set {
            _pause = value;
            onPauseChange(null, EventArgs.Empty);
        }
    }
    public static bool _pause = false;
    public static event EventHandler onPauseChange = delegate {};

    public static void tickStart(Time nextTime)
    {
        _refFrameLast = _referenceFrame.requestLocalPosition(nextTime);
    }

    public static void setReferenceFrame(body b)
    {
        currentPosition = new position(0, 0, 0);
        _referenceFrame = b;
    }

    public static body requestReferenceFrame() => _referenceFrame;
}