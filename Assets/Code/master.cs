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
    //public readonly static Time time = new Time(2460806.5, true);
    public readonly static Time time = new Time(new DateTime(2025, 12, 12), true);

    /// <summary> Total ticks (times the game has updated) since the game was initialized. </summary>
    public static int currentTick = 0;

    /// <summary> The players current position, in km. </summary>
    public static position currentPosition {get => _currentPosLast; set {
        _currentPosLast = value;
        if (alreadyStarted) master.requestPositionUpdate();
        onCurrentPositionChange(null, EventArgs.Empty);
    }}

    /// <summary> The current position of the reference frame relative to the sun. See also <see cref="requestReferenceFrame"/>. </summary>
    public static position referenceFrame {
        get {
            if (ReferenceEquals(_referenceFrame, null)) return new position(0, 0, 0);
            else return _refFrameLast;
        }
    }

    /// <summary> The current body that is the reference frame. See also <see cref="referenceFrame"/>. </summary>
    public static body requestReferenceFrame() {
        if (_referenceFrame is null) return master.sun;
        return _referenceFrame;
    }

    /// <summary> Control whether or not the game is paused. Calls <see cref="onPauseChange"/> when changed. </summary>
    public static bool pause {
        get {return _pause;}
        set {
            _pause = value;
            onPauseChange(null, EventArgs.Empty);
        }
    }

    public static bool finishedInitializing => alreadyStarted;


    private static body _referenceFrame;
    private static position _refFrameLast, _currentPosLast = new position(0, 0, 0);
    private static double _scale = 1000;
    public static bool _pause = false;


    /// <summary> Event that is called when <see cref="scale"/> is changed or <see cref="requestScaleUpdate"/> is called. </summary>
    public static event EventHandler onScaleChange = delegate {};

    /// <summary> Event that is called when <see cref="pause"/> is changed. </summary>
    public static event EventHandler onPauseChange = delegate {};
    /// <summary> Event that is called when <see cref="referenceFrame"/> is changed via calling <see cref="setReferenceFrame"/>. </summary>
    public static event EventHandler onReferenceFrameChange = delegate {};
    /// <summary> Event that is called the moment before the main loop is about to start. </summary>
    public static event EventHandler onFinalSetup = delegate {};

    public static event EventHandler onCurrentPositionChange = delegate {};


    /// <summary> Event that will update the positions of any class derived from <see cref="body"/>. Called when <see cref="requestPositionUpdate"/> is called. </summary>
    public static event EventHandler updatePositions = delegate {};

    /// <summary> Event that will update the scheduling for all Facilities and their connecting satellites. Called when <see cref="requestSchedulingUpdate"/> is called. </summary>
    public static event EventHandler updateScheduling = delegate {};

    /// <summary> Event that will update the planet/system loading queue（see <see cref="jsonParser.updateQueue"/>). Called when <see cref="requestJsonQueueUpdate"/> is called. </summary>
    public static event EventHandler updateJsonQueue = delegate {};

    /// <summary> Event that is called at the end of each tick (frame). Called when <see cref="markTickFinished"/> is called. </summary>
    public static event EventHandler finishTick = delegate {};


    /// <summary> Calls <see cref="updatePositions"/>. </summary>
    public static void requestPositionUpdate() {
        updatePositions(null, EventArgs.Empty);
        _refFrameLast = _referenceFrame.pos;
    }

    /// <summary> Calls <see cref="updateScheduling"/>. </summary>
    public static void requestSchedulingUpdate() {updateScheduling(null, EventArgs.Empty);}

    /// <summary> Calls <see cref="onScaleChange"/>. </summary>
    public static void requestScaleUpdate() {onScaleChange(null, EventArgs.Empty);}

    /// <summary> Calls <see cref="updateJsonQueue"/>. </summary>
    public static void requestJsonQueueUpdate() {updateJsonQueue(null, EventArgs.Empty);}
    /// <summary> Calls <see cref="finishTick"/>. </summary>
    public static void markTickFinished() {finishTick(null, EventArgs.Empty);}

    /// <summary> List of all <see cref="planet"/> currently loaded. </summary>
    public static List<planet> allPlanets = new List<planet>();

    /// <summary> List of all <see cref="satellite"/> currently loaded. </summary>
    public static List<satellite> allSatellites = new List<satellite>();

    /// <summary> List of all <see cref="facility"/> currently loaded. </summary>
    public static List<facility> allFacilities = new List<facility>();

    public static List<Timeline> rod = new List<Timeline>();


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

        foreach (facility f in allFacilities)
        {
            LineRenderer lr = null;
            if (f.representation.gameObject.TryGetComponent<LineRenderer>(out lr)) lr.positionCount = 0;
        }
    }

    /// <summary> Called at the start of each tick (see <see cref="currentTick"/>), before the time is updated. </summary>
    /// <remarks><paramref name="nextTime"/> The time that will be the new time. </remarks>
    public static void tickStart(Time nextTime)
    {
        _refFrameLast = _referenceFrame.requestPosition(nextTime);
    }

    /// <summary> Sets the reference frame to <paramref name="b"/>. </summary>
    /// <remarks><paramref name="b"/> The body to become the reference frame. </remarks>
    public static void setReferenceFrame(body b)
    {
        if (alreadyStarted) {
            modeController.disableAll();
            master.clearAllLines();
        }

        currentPosition = new position(0, 0, 0);
        _referenceFrame = b;

        general.notifyStatusChange();
        general.notifyTrailsChange();

        onCurrentPositionChange(null, EventArgs.Empty);
    }

    private static bool alreadyStarted = false;
    /// <summary> Tell the program that the simulation is about ready to start. Calls <see cref="onFinalSetup"/>. </summary>
    public static void markStartOfSimulation() {
        if (alreadyStarted) return;
        alreadyStarted = true;

        onFinalSetup(null, EventArgs.Empty);
    }

    /// <summary> Remove a facility from the scene </summary>
    public static void removeFacility(facility f) {
        master.allFacilities.Remove(f);
        f.destroy();
    }


    // TODO: find a better implementation of this
    /// <summary> Determines relationship between bodies (parent, child, etc) in the form parent, List(child) </summary>
    /// <remarks> Useful as it does not require a postional dependency (as with normal parenting) </remarks>
    public static Dictionary<planet, List<planet>> relationshipPlanet = new Dictionary<planet, List<planet>>();
    /// <summary> Determines relationship between bodies (parent, child, etc) in the form parent, List(child) </summary>
    /// <remarks> Useful as it does not require a postional dependency (as with normal parenting) </remarks>
    public static Dictionary<planet, List<satellite>> relationshipSatellite = new Dictionary<planet, List<satellite>>();
    /// <summary> Determines relationship between bodies (parent, child, etc) in the form parent, List(child) </summary>
    public static Dictionary<planet, List<facility>> relationshipFacility = new Dictionary<planet, List<facility>>();

    /// <summary> Stores the orbital periods of bodies in julian. </summary>
    /// <remarks> Find a better way to do this. </remarks>
    public static Dictionary<string, double> orbitalPeriods = new Dictionary<string, double>() {
        {"Earth", 365.25},
        {"Luna", 27.322},
        {"Mercury", 88},
        {"Venus", 225},
        {"Mars", 687},
        {"Jupiter", 4335.22274111},
        {"Saturn", 10827.9216305},
        {"Uranus", 30927.2376018},
        {"Neptune", 60930.0387063},
        {"Pluto", 90950.8749435},
        {"LCN-1", 0.50000030159},
        {"LCN-2", 0.50000030159},
        {"LCN-3", 0.50000030159},
        {"Moonlight-1", 0.50000030159},
        {"Moonlight-2", 0.50000030159},
        {"CubeSat-1", 0.3671969293},
        {"CubeSat-2", 0.08179930284},
        {"Deimos", 1.263},
        {"Phobos", 0.319},
        {"Miranda", 1.413},
        {"Ariel", 2.520},
        {"Umbriel", 4.144 },
        {"Titania", 8.706},
        {"Oberon", 13.463},
        {"Proteus", 1.122315},
        {"Triton", 5.876994},
        {"Charon", 6.3872},
        {"Io", 1.77},
        {"Europa", 3.55},
        {"Ganymede", 7.155},
        {"Callisto", 16.691},
        {"Titan", 15.945421 },
        {"Hyperion", 21.28 },
        {"Iapetus", 79.33 },
        {"Mimas", 0.9424218  },
        {"Enceladus", 1.370218},
        {"Tethys", 1.888 },
        {"Voyager 1", 62},
        {"Voyager 2", 62},
        {"SOLO", 62},
        {"PSP", 62},
        {"Lucy", 62},
        {"STP Sat 5", 0.001}
    };
}
