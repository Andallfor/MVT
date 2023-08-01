using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;

public class controller : MonoBehaviour
{
    public static planet earth, moon;
    public static planet mars;
    public static planet defaultReferenceFrame;
    public static double speed = 0.0000116;
    public static int tickrate = 7200;
    private Vector3 planetFocusMousePosition, planetFocusMousePosition1, facilityFocusMousePosition, facilityFocusMousePosition1;
    private Coroutine loop;
    public static bool useTerrainVisibility = false;
    public static controller self;
    public static double scenarioStart;
    public static bool accessRunning = false;


    public static float _logBase = 35;

    private void Awake() { self = this; }

    private void Start()
    {
        general.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.planetParent = GameObject.FindGameObjectWithTag("planet/parent");
        uiHelper.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.camera = Camera.main;

        resLoader.initialize();
        web.initialize();

        //master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/sun", 0.0416666665, planetType.planet),
        master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/sun", 0.0416666665, planetType.planet),
            new representationData("planet", "sunTex"));

        /*
        string date = DateTime.Now.ToString("MM-dd_hhmm");
        if (!File.Exists(DBReader.mainDBPath))
        {
            UnityEngine.Debug.Log("Generating main.db");
            UnityEngine.Debug.Log("command: " + $"{DBReader.data.get("2023EarthAssets")} {DBReader.mainDBPath}");
            System.Diagnostics.Process.Start(DBReader.apps.excelParser, $"{DBReader.data.get("2023EarthAssetsWithOrbits.xlsx")} {DBReader.mainDBPath}").WaitForExit();
        }*/
        /*var missionStructure = DBReader.getData();
        Debug.Log("epoch: " + missionStructure["EarthTest"].epoch);
        DBReader.output.setOutputFolder(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), date));
        string json = JsonConvert.SerializeObject(missionStructure, Formatting.Indented);
        DBReader.output.write("MissionStructure_2023.txt", json);
        Debug.Log("Generating windows.....");
        ScheduleStructGenerator.genDB(missionStructure, "EarthTest", "DFSTestwindows.json", date, "PreconWindows");
        Debug.Log("Generating conflict list.....");
        ScheduleStructGenerator.createConflictList(date);
        ScheduleStructGenerator.genDBNoJSON(missionStructure, date, "cut1Windows");
        ScheduleStructGenerator.createConflictList(date);
        Debug.Log("Doing DFS.....");
        ScheduleStructGenerator.doDFS(date);
        Debug.Log(DBReader.output.getClean("PostDFSUsers.txt"));
        //System.Diagnostics.Process.Start(DBReader.apps.heatmap, $"{DBReader.output.getClean("PostDFSUsers.txt")} {DBReader.output.get("PostDFSUsers", "png")} 0 1 6");
        //System.Diagnostics.Process.Start(DBReader.apps.heatmap, $"{DBReader.output.getClean("PreDFSUsers.txt")} {DBReader.output.get("PreDFSUsers", "png")} 0 1 6");
        System.Diagnostics.Process.Start(DBReader.apps.schedGen, $"{DBReader.output.get("ScheduleCSV", "csv")} source destination 0 1 {DBReader.output.get("sched", "png")} 0");
        */

        loadingController.start(new Dictionary<float, string>() {
            {0, "Generating Planets"},
            {0.10f, "Generating Satellites"},
            {0.75f, "Generating Terrain"}
        });

        StartCoroutine(start());

        //csvParser.loadAndCreateFacilities("CSVS/stationlist", earth);
    }

    IEnumerator start()
    {
        //yield return StartCoroutine(Artemis3());
        yield return StartCoroutine(JPL());
        defaultReferenceFrame = earth;
        //onlyEarth();

        yield return new WaitForSeconds(0.1f);

        //runScheduling();
        //csvParser.loadScheduling("CSVS/SCHEDULING/July 2021 NSN DTE Schedule");

        master.setReferenceFrame(master.allPlanets.First(x => x.name == "Earth"));
        master.pause = false;
        general.camera = Camera.main;

        //runDynamicLink();
        //linkBudgeting.accessCalls("C:/Users/akazemni/Desktop/connections.txt");

        master.markStartOfSimulation();

        loadingController.addPercent(0.26f);

        modeController.registerMode(planetOverview.instance);
        modeController.registerMode(planetFocus.instance);
        modeController.registerMode(uiMap.instance);
        modeController.registerMode(facilityFocus.instance);
        modeController.initialize();

        modeController.disableAll();

        if (planetFocus.instance.lunarTerrainFilesExist)
        {
            general.pt = loadTerrain();
            general.plt = loadPoles();
        }

        startMainLoop();

        //Debug.Log(position.J2000(new position(0, 1, 0), new position(0, 0, -1), new position(0, 1, 0)));
        //facility f1 = new facility("1", earth, new facilityData("1", new geographic(-33.60579, -78.88177), 10, new List<antennaData>()), new representationData("facility", "defaultMat"));
    }

    public static void runWindows()
    {
        master.time.addJulianTime((double)2461021.5 - (double)master.time.julian);
        master.requestPositionUpdate();
        dynamicLinkOptions options = new dynamicLinkOptions();
        options.callback = (data) =>
        {
            //windows.jsonWindows(data);
            windows.jsonWindows(data);
        };
        options.debug = true;
        options.blocking = true;
        //options.outputPath = Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "data.txt");


        useTerrainVisibility = true;

        List<object> users = new List<object>();
        List<object> providers = new List<object>();

        foreach (var u in linkBudgeting.users)
        {
            if (u.Value.Item1) users.Add(master.allFacilities.Find(x => x.name == u.Key));
            else users.Add(master.allSatellites.Find(x => x.name == u.Key));
        }

        foreach (var p in linkBudgeting.providers)
        {
            if (p.Value.Item1) providers.Add(master.allFacilities.Find(x => x.name == p.Key));
            else providers.Add(master.allSatellites.Find(x => x.name == p.Key));
        }

        visibility.raycastTerrain(users, providers, master.time.julian, master.time.julian + 30, speed, options, false);


    }

    public static void runWindowsNoRate()
    {
        master.time.addJulianTime((double)2461021.5 - (double)master.time.julian);
        master.requestPositionUpdate();
        dynamicLinkOptions options = new dynamicLinkOptions();
        options.callback = (data) =>
        {
            WNRs.jsonWindows(data);
        };
        options.debug = true;
        options.blocking = false;
        options.outputPath = Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "data.txt");
        //options.outputPath = "/Users/arya/Downloads/data.txt";

        useTerrainVisibility = true;

        List<object> users = new List<object>();
        List<object> providers = new List<object>();

        foreach (var u in linkBudgeting.users)
        {
            if (u.Value.Item1) users.Add(master.allFacilities.Find(x => x.name == u.Key));
            else users.Add(master.allSatellites.Find(x => x.name == u.Key));
        }

        foreach (var p in linkBudgeting.providers)
        {
            if (p.Value.Item1) providers.Add(master.allFacilities.Find(x => x.name == p.Key));
            else providers.Add(master.allSatellites.Find(x => x.name == p.Key));
        }

        visibility.raycastTerrain(users, providers, master.time.julian, master.time.julian + 1, speed, options, true);
    }

    public static void runDynamicLink()
    {
        master.time.addJulianTime((double)2461021.5 - (double)master.time.julian);
        master.requestPositionUpdate();
        dynamicLinkOptions options = new dynamicLinkOptions();
        options.callback = (data) =>
        {
            //System.IO.Directory.CreateDirectory("User/arya/Downloads/Access Call Results");

            foreach (KeyValuePair<string, (bool t, double start, double end)> provider in linkBudgeting.providers)
            {
                foreach (KeyValuePair<string, (bool t, double start, double end)> user in linkBudgeting.users)
                {

                    List<double> time = data[(provider.Key, user.Key)].Item1;
                    List<double> distance = data[(provider.Key, user.Key)].Item2;
                    List<string> final = new List<string>();

                    if (time.Count == 0) continue;
                    for (int x = 0; x < time.Count; x++)
                    {
                        final.Add("Time:" + time[x] + " Distance: " + distance[x]);
                    }

                    System.IO.File.WriteAllLines(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads)) + "/Access Call Results/" + provider.Key + " to " + user.Key + ".txt", final);
                }
            }
        };
        options.debug = true;
        options.blocking = false;
        options.outputPath = Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "data.txt");
        //options.outputPath = "/Users/arya/Downloads/data.txt";


        useTerrainVisibility = true;

        List<object> users = new List<object>();
        List<object> providers = new List<object>();

        foreach (var u in linkBudgeting.users)
        {
            if (u.Value.Item1) users.Add(master.allFacilities.Find(x => x.name == u.Key));
            else users.Add(master.allSatellites.Find(x => x.name == u.Key));
        }

        foreach (var p in linkBudgeting.providers)
        {
            if (p.Value.Item1) providers.Add(master.allFacilities.Find(x => x.name == p.Key));
            else providers.Add(master.allSatellites.Find(x => x.name == p.Key));
        }

        visibility.raycastTerrain(providers, users, master.time.julian, master.time.julian + 30, speed, options, false);
    }

    public void startMainLoop(bool force = false)
    {
        if (loop != null && force == false) return;

        loop = StartCoroutine(general.internalClock(tickrate, int.MaxValue, (tick) =>
        {
            if (!general.blockMainLoop)
            {
                if (master.pause)
                {
                    master.tickStart(master.time);
                    master.time.addJulianTime(0);
                }
                else
                {
                    Time t = new Time(master.time.julian);
                    t.addJulianTime(speed);
                    master.tickStart(t);
                    master.time.addJulianTime(speed);
                }
            }

            if (planetFocus.instance.lunarTerrainFilesExist) general.pt.updateTerrain();

            if (!planetOverview.instance.active) master.requestSchedulingUpdate();
            master.currentTick = tick;

            master.markTickFinished();
        }, null));
    }

    public void LateUpdate() {
        playerControls.lastMousePos = Input.mousePosition;
    }

    public void Update() {
        playerControls.update();
        modeController.update();

        if (Input.GetKeyDown("o"))
        {
            Vector3 v1 = (Vector3)(geographic.toCartesian(new geographic(0, 0), earth.radius).swapAxis());
            Vector3 v2 = (Vector3)(geographic.toCartesianWGS(new geographic(0, 0), 0).swapAxis());

            UnityEngine.Debug.Log("regular: " + v1);
            UnityEngine.Debug.Log("wgs: " + v2);

            runWindowsNoRate();
        }

        if (Input.GetKeyDown("k"))
        {
            string src = Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth/juan");
            var f = new universalTerrainJp2File(Path.Combine(src, "data.jp2"), Path.Combine(src, "metadata.txt"));

            Material m = Resources.Load<Material>("Materials/vis/juanVis");
            f.load(Vector2.zero, Vector2.one, 0, 0, default(position)).drawAll(m, resLoader.load<GameObject>("planetMesh"), new string[0], earth.representation.gameObject.transform);
        }

        if (Input.GetKeyDown("g"))
        {
            string src = Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth");
            string p = Directory.GetDirectories(src)[stationIndex];
            universalTerrainJp2File f = new universalTerrainJp2File(Path.Combine(p, "data.jp2"), Path.Combine(p, "metadata.txt"), true);
            UnityEngine.Debug.Log(p);

            if (prevDist != null) prevDist.clear();
            geographic offset = new geographic(1, 1);
            prevDist = f.load(f.center, 0, f.getBestResolution(f.center - offset, f.center + offset, 5_000_000), offset: 1);

            prevDist.drawAll(earth.representation.gameObject.transform);

            stationIndex++;

            if (stationIndex >= 20) stationIndex = 0;
        }

        if (Input.GetKeyDown("y"))
        {
            if (accessRunning == false)
            {
                master.ID = 0;
                accessRunning = true;
                List<satellite> users = new List<satellite>();
                List<ScheduleStructGenerator.Window> windows = new List<ScheduleStructGenerator.Window>();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                foreach (var u in linkBudgeting.users)
                {
                    users.Add(master.allSatellites.Find(x => x.name == u.Key));
                }

                foreach (var p in linkBudgeting.providers)
                {
                    facility provider = master.allFacilities.Find(x => x.name == p.Key);

                    accessCallGeneratorWGS access = new accessCallGeneratorWGS(earth, provider.geo, users, p.Key);
                    access.initialize(Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth/" + p.Key), 2);
                    var output = access.findTimes(new Time(scenarioStart), new Time(scenarioStart + 30), 0.00069444444, 0.00001157407 / 2.0, true); // ADD TO WINDOWS LIST HERE
                    //StartCoroutine(stall(access));
                    foreach (ScheduleStructGenerator.Window w in output)
                    {
                        windows.Add(w);
                    }
                }
                ScheduleStructGenerator.scenario.aryasWindows = windows;
                master.ID = 0;
                accessRunning = false;
                stopWatch.Stop();

                TimeSpan ts = stopWatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                UnityEngine.Debug.Log("RunTime " + elapsedTime);
            }
        }

        if (Input.GetKeyDown("b"))
        {
            web.sendMessage((byte)constantWebHandles.ping, new byte[] { 15 });
        }
    }

    /*private IEnumerator stall(accessCallGeneratorWGS access)
    { // TODO: replace with a physics update call
        yield return new WaitForSeconds(1);
        //var output = access.findTimes(new Time(2461022.77871296), new Time(2461022.78237024), 0.00069444444, 0.00001157407 / 2.0);
        
        //var output = access.findTimes(new Time(2461021.77854328), new Time(2461029.93452393), 0.00069444444, 0.00001157407 / 2.0);
        var output = access.findTimes(new Time(2459560.84525522), new Time(2459560.84525522 + 1000), 0.00069444444, 0.00001157407 / 2.0);
        //var output = access.bruteForce(new Time(2461021.77854328), new Time(2461022.93452393), 0.00001157407);
        // access.saveResults(output);
        //Debug.Log("done");
    }*/

    int stationIndex = 0;
    meshDistributor<universalTerrainMesh> prevDist;

    private planetTerrain loadTerrain() {
        planetTerrain pt = new planetTerrain(moon, "Materials/planets/moon/moon", 1737.4, 1);

        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MVT/terrain");

        pt.generateFolderInfos(new string[4] {
            Path.Combine(p, "lunaBinary/1"),
            Path.Combine(p, "lunaBinary/2"),
            Path.Combine(p, "lunaBinary/3"),
            Path.Combine(p, "lunaBinary/4")
        });

        return pt;
    }

    private poleTerrain loadPoles()
    {
        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MVT/terrain");
        //string p = Path.Combine(Application.dataPath, "terrain");
        return new poleTerrain(new Dictionary<int, string>() {
            {5,  Path.Combine(p, "polesBinary/25m")},
            {10, Path.Combine(p, "polesBinary/50m")},
            {20, Path.Combine(p, "polesBinary/100m")}
        }, moon.representation.gameObject.transform);
    }

    private IEnumerator JPL()
    {
        representationData rd = new representationData("planet", "defaultMat");
        representationData frd = new representationData("facility", "defaultMat");

        var data = DBReader.getData();

        double prevTime = master.time.julian;
        double startTime = Time.strDateToJulian(data["EarthTest"].epoch);
        scenarioStart = startTime;
        master.time.addJulianTime(Time.strDateToJulian(data["EarthTest"].epoch) - prevTime);

        double EarthMu = 398600.0;
        double moonMu = 4900.0;
        double sunMu = 132712e+6;
        double epoch = 2459945.5000000;

        List<satellite> earthSats = new List<satellite>();
        List<satellite> moonSats = new List<satellite>();


        earth = new planet("Earth", new planetData(6356.75, rotationType.earth, new Timeline(149548442.3703442, 1.638666603580831e-02, 3.094435789048925e-03, 2.514080725047589e+02, 2.130422595065601e+02, 3.556650570783973e+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "earthTex"));
        moon = new planet("Luna", new planetData(1738.1, rotationType.none, new Timeline(3.809339850602024E+05, 5.715270081780017E-02, 5.100092074801750, 2.355576097735322E+02, 4.145906044051883E+01, 1.102875277317696E+02, 1, epoch, EarthMu), planetType.moon), new representationData("planet", "moonTex"), earth);
        planet mercury = new planet("Mercury", new planetData(2439.7, rotationType.none, new Timeline(5.790908989105575E+07, 2.056261098757466E-01, 7.003572595914431, 2.919239643694458E+01, 4.830139915269011E+01, 3.524475388676764E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "mercuryTex"));
        planet venus = new planet("Venus", new planetData(6051.8, rotationType.none, new Timeline(1.082084649394783E+08, 6.763554426926404E-03, 3.394414241082599, 5.465872015572022E+01, 7.661682984113415E+01, 1.894020065218014E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "venusTex"));
        planet jupiter = new planet("Jupiter", new planetData(71492, rotationType.none, new Timeline(7.784060565591711E+08, 4.849069877916937E-02, 1.303574691375196, 2.734684496159951E+02, 1.005141062239384E+02, 3.583753533457323E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "jupiterTex"));
        planet saturn = new planet("Saturn", new planetData(60268, rotationType.none, new Timeline(1.432942173696950E+09, 5.347693294083503E-02, 2.486204025043723, 3.351085660472628E+02, 1.135934765410469E+02, 2.425433471952230E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "saturnTex"));
        planet uranus = new planet("Uranus", new planetData(25559, rotationType.none, new Timeline(2.884647050482038E+09, 4.382306037308097E-02, 7.711154256612637E-01, 9.259562572483993E+01, 7.406386852962497E+01, 2.449588470478398E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "uranusTex")); 
        planet neptune = new planet("Neptune", new planetData(24764, rotationType.none, new Timeline(4.533353284339735E+09, 1.474287247824008E-02, 1.768865936090221, 2.519545559747149E+02, 1.317418592957744E+02, 3.314896534126764E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "neptuneTex"));
        planet pluto = new planet("Pluto", new planetData(1188.3, rotationType.none, new Timeline(5.921110734220912E+09, 2.497736459257524E-01, 1.734771133563329E+01, 1.140877773351901E+02, 1.104098121556590E+02, 4.784240505983976E+01, 1, epoch, sunMu), planetType.planet), new representationData("planet", "moonTex"));
        planet mars = new planet("Mars", new planetData(3389.92, rotationType.none, new Timeline(2.279254603773820E+08, 9.344918986180577E-02, 1.847925718684767, 2.866284864604108E+02, 4.948907666319641E+01, 1.014635329635219E+02, 1, epoch, sunMu), planetType.planet), new representationData("planet", "marsTex"));

        master.relationshipPlanet[earth] = new List<planet>() { moon };

        /*yield return new WaitForSeconds(0.1f);
        loadingController.addPercent(0.11f);*/
        List<string> facs = new List<string>();
        List<facility> earthfacs = new List<facility>();

        foreach (KeyValuePair<string, dynamic> x in data["EarthTest"].satellites)
        {
            var dict = data["EarthTest"].satellites[x.Key];
            if (x.Key == "My Satellite") continue;

            double start = 0, stop = 0;
            if (dict["TimeInterval_start"] is string) start = Double.Parse(dict["TimeInterval_start"], System.Globalization.NumberStyles.Any);
            else start = (double)dict["TimeInterval_start"];

            if (dict["TimeInterval_stop"] is string) stop = Double.Parse(dict["TimeInterval_stop"], System.Globalization.NumberStyles.Any);
            else stop = (double)dict["TimeInterval_stop"];

            if (dict["Type"] == "Satellite")
            {
                satellite sat = null;

                double A = 0, E = 0, I = 0, RAAN = 0, W = 0, M = 0;

                if (dict["SemimajorAxis"] is string) A = Double.Parse(dict["SemimajorAxis"], System.Globalization.NumberStyles.Any);
                else A = (double)dict["SemimajorAxis"];
                if (dict["Eccentricity"] is string) E = Double.Parse(dict["Eccentricity"], System.Globalization.NumberStyles.Any);
                else E = (double)dict["Eccentricity"];
                if (dict["Inclination"] is string) I = Double.Parse(dict["Inclination"], System.Globalization.NumberStyles.Any);
                else I = (double)dict["Inclination"];
                if (dict["Arg_of_Perigee"] is string) W = Double.Parse(dict["Arg_of_Perigee"], System.Globalization.NumberStyles.Any);
                else W = (double)dict["Arg_of_Perigee"];
                if (dict["RAAN"] is string) RAAN = Double.Parse(dict["RAAN"], System.Globalization.NumberStyles.Any);
                else RAAN = (double)dict["RAAN"];
                if (dict["MeanAnomaly"] is string) M = Double.Parse(dict["MeanAnomaly"], System.Globalization.NumberStyles.Any);
                else M = (double)dict["MeanAnomaly"];

                if (dict["CentralBody"] == "Moon")
                {
                    sat = new satellite(x.Key, new satelliteData(new Timeline(A, E, I, W, RAAN, M, 1, Time.strDateToJulian(dict["OrbitEpoch"]), moonMu)), rd, moon);
                    //satellite.addFamilyNode(moon, sat);
                    moonSats.Add(sat);
                }
                else if (dict["CentralBody"] == "Earth")
                {
                    sat = new satellite(x.Key, new satelliteData(new Timeline(A, E, I, W, RAAN, M, 1, Time.strDateToJulian(dict["OrbitEpoch"]), EarthMu)), rd, earth);
                    //satellite.addFamilyNode(earth, sat);
                    earthSats.Add(sat);
                }

                if (dict["user_provider"] == "user" || dict["user_provider"] == "user/provider") linkBudgeting.users.Add(x.Key, (false, startTime + start, startTime + stop));
                /*if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (false, startTime + start, startTime + stop));
                if (dict["user_provider"] == "user/provider")
                {
                    linkBudgeting.users.Add(x.Key, (false, startTime + start, startTime + stop));
                    linkBudgeting.providers.Add(x.Key, (false, startTime + start, startTime + stop));
                }*/
            }
            else if (dict["Type"] == "Facility")
            {
                double lat = 0, lon = 0, alt = 0;
                if (dict["Lat"] is string) lat = Double.Parse(dict["Lat"], System.Globalization.NumberStyles.Any);
                else lat = (double)dict["Lat"];
                if (dict["Long"] is string) lon = Double.Parse(dict["Long"], System.Globalization.NumberStyles.Any);
                else lon = (double)dict["Long"];
                if (dict["AltitudeConstraint"] is string) alt = Double.Parse(dict["AltitudeConstraint"], System.Globalization.NumberStyles.Any);
                else alt = (double)dict["AltitudeConstraint"];

                string facilityName = Regex.Replace(x.Key, @"[\d]", String.Empty);

                if (master.fac2ant.ContainsKey(facilityName)) master.fac2ant[facilityName].Add(x.Key);
                else master.fac2ant[facilityName] = new List<string> { x.Key };


                if (dict["CentralBody"] == "Moon")
                {
                    if (!facs.Contains(facilityName))
                    {
                        facility fd = new facility(facilityName, moon, new facilityData(facilityName, new geographic(lat, lon), alt, new List<antennaData>(), new Time(startTime + start), new Time(startTime + stop)), frd);
                        facs.Add(facilityName);
                        if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(facilityName, (true, startTime + start, startTime + stop));
                    }
                }
                else if (dict["CentralBody"] == "Earth")
                {
                    if (!facs.Contains(facilityName))
                    {
                        facility fd = new facility(facilityName, earth, new facilityData(facilityName, new geographic(lat, lon), alt, new List<antennaData>(), new Time(startTime + start), new Time(startTime + stop)), frd);
                        facs.Add(facilityName);
                        earthfacs.Add(fd);
                        if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(facilityName, (true, startTime + start, startTime + stop));
                    }
                }

                //if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, startTime + start, startTime + stop));
                /*if (dict["user_provider"] == "user/provider")
                {
                    linkBudgeting.users.Add(x.Key, (true, startTime + start, startTime + stop));
                    linkBudgeting.providers.Add(x.Key, (true, startTime + start, startTime + stop));
                }*/
            }
        }
        master.relationshipSatellite[earth] = earthSats;
        master.relationshipSatellite[moon] = moonSats;
        master.relationshipFacility[earth] = earthfacs;

        yield return new WaitForSeconds(0.1f);
        loadingController.addPercent(0.11f);
        loadingController.addPercent(1);

    }

    public void OnApplicationQuit()
    {
        IMesh.clearCache();
    }
}
