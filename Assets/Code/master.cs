using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary> Static class that holds most important information regarding backend stuff. </summary>
/// <remarks>See <see cref="general"/> for the frontend version of <see cref="master"/>. </remarks>
public static class master
{
    /// <summary> The Sun. Must be created in all simulations. By default is (0, 0, 0).</summary>
    public static planet sun;

    /// <summary> The current scale used by the game, in km. Calls <see cref="onScaleChange"/> when changed.</summary>
    public static double scale {
        get {return _scale;}
        set {
            _scale = value;
            onScaleChange(null, EventArgs.Empty);
        }
    }

    /// <summary> The current time in game. </summary>
    public readonly static Time time = new Time(2460806.5, true);

    /// <summary> Total ticks (times the game has updated) since the game was initialized. </summary>
    public static int currentTick = 0;

    /// <summary> The players current position, in km. </summary>
    public static position currentPosition = new position(0, 0, 0);

    /// <summary> The current position of the reference frame relative to the sun. See also <see cref="requestReferenceFrame"/>. </summary>
    public static position referenceFrame {
        get {
            if (ReferenceEquals(_referenceFrame, null)) return new position(0, 0, 0);
            else return _refFrameLast;
        }
    }

    /// <summary> The current body that is the reference frame. See also <see cref="referenceFrame"/>. </summary>
    public static body requestReferenceFrame() => _referenceFrame;

    /// <summary> Control whether or not the game is paused. Calls <see cref="onPauseChange"/> when changed. </summary>
    public static bool pause {
        get {return _pause;}
        set {
            _pause = value;
            onPauseChange(null, EventArgs.Empty);
        }
    }


    private static body _referenceFrame;
    private static position _refFrameLast;
    private static double _scale = 1000;
    public static bool _pause = false;


    /// <summary> Event that is called when <see cref="scale"/> is changed or <see cref="requestScaleUpdate"/> is called. </summary>
    public static event EventHandler onScaleChange = delegate {};

    /// <summary> Event that is called when <see cref="pause"/> is changed. </summary>
    public static event EventHandler onPauseChange = delegate {};


    /// <summary> Event that will update the positions of any class derived from <see cref="body"/>. Called when <see cref="requestPositionUpdate"/> is called. </summary>
    public static event EventHandler updatePositions = delegate {};

    /// <summary> Event that will update the scheduling for all facilites and their connecting satellites. Called when <see cref="requestSchedulingUpdate"/> is called. </summary>
    public static event EventHandler updateScheduling = delegate {};

    /// <summary> Event that will update the planet/system loading queue（see <see cref="jsonParser.updateQueue"/>). Called when <see cref="requestJsonQueueUpdate"/> is called. </summary>
    public static event EventHandler updateJsonQueue = delegate {};


    /// <summary> Calls <see cref="updatePositions"/>. </summary>
    public static void requestPositionUpdate() {updatePositions(null, EventArgs.Empty);}

    /// <summary> Calls <see cref="updateScheduling"/>. </summary>
    public static void requestSchedulingUpdate() {updateScheduling(null, EventArgs.Empty);}

    /// <summary> Calls <see cref="onScaleChange"/>. </summary>
    public static void requestScaleUpdate() {onScaleChange(null, EventArgs.Empty);}

    /// <summary> Calls <see cref="updateJsonQueue"/>. </summary>
    public static void requestJsonQueueUpdate() {updateJsonQueue(null, EventArgs.Empty);}

    /// <summary> List of all <see cref="planet"/> currently loaded. </summary>
    public static List<planet> allPlanets = new List<planet>();

    /// <summary> List of all <see cref="satellite"/> currently loaded. </summary>
    public static List<satellite> allSatellites = new List<satellite>();

    /// <summary> List of all <see cref="facility"/> currently loaded. </summary>
    public static List<facility> allFacilites = new List<facility>();


    /// <summary> Clear all <see cref="LineRenderer"/> components on <see cref="planet"/>, <see cref="satellite"/>, and <see cref="facility"/>. </summary>
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

    /// <summary> Called at the start of each tick (see <see cref="currentTick"/>), before the time is updated. </summary>
    /// <remarks><paramref name="nextTime"/> The time that will be the new time. </remarks>
    public static void tickStart(Time nextTime)
    {
        _refFrameLast = _referenceFrame.requestLocalPosition(nextTime);
    }

    /// <summary> Sets the reference frame to <paramref name="b"/>. </summary>
    /// <remarks><paramref name="b"/> The body to become the reference frame. </remarks>
    public static void setReferenceFrame(body b)
    {
        currentPosition = new position(0, 0, 0);
        _referenceFrame = b;
    }

    // TODO: find a better implementation of this
    /// <summary> Determines relationship between bodies (parent, child, etc) in the form parent, List(child) </summary>
    /// <remarks> Useful as it does not require a postional dependency (as with normal parenting) </remarks>
    public static Dictionary<planet, List<planet>> relationshipPlanet = new Dictionary<planet, List<planet>>();
    /// <summary> Determines relationship between bodies (parent, child, etc) in the form parent, List(child) </summary>
    /// <remarks> Useful as it does not require a postional dependency (as with normal parenting) </remarks>
    public static Dictionary<planet, List<satellite>> relationshipSatellite = new Dictionary<planet, List<satellite>>();
}
