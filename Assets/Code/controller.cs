using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.EventSystems;
using System.IO;
using System.Text;

public class controller : MonoBehaviour
{
    public static planet earth, moon;
    public static planet mars;
    public static planet defaultReferenceFrame;
    public static double speed = 0.0000116 * 60;
    public static int tickrate = 7200;
    private Vector3 planetFocusMousePosition, planetFocusMousePosition1, facilityFocusMousePosition, facilityFocusMousePosition1;
    private Coroutine loop;
    public static bool useTerrainVisibility = false;
    public static controller self;
    private satellite sat1;

    public static float _logBase = 35;

    private void Awake() {self = this;}

    private void Start() {
        general.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.planetParent = GameObject.FindGameObjectWithTag("planet/parent");
        uiHelper.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.camera = Camera.main;

        resLoader.initialize();

        //master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/sun", 0.0416666665, planetType.planet),
        master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/JPL/dec2025/PLANETS/sun", 0.0416666665, planetType.planet),
            new representationData("planet", "sunTex"));

        loadingController.start(new Dictionary<float, string>() {
            {0, "Generating Planets"},
            {0.10f, "Generating Satellites"},
            {0.75f, "Generating Terrain"}
        });

        StartCoroutine(start());

        csvParser.loadAndCreateFacilities("CSVS/stationlist", earth);
    }

    IEnumerator start()
    {
        //yield return StartCoroutine(Artemis3());
        yield return StartCoroutine(JPL());
        defaultReferenceFrame = earth;
        //onlyEarth();

        yield return new WaitForSeconds(0.1f);

        master.onScaleChange += (s, e) => {
            if (general.showingTrails) {
                foreach (planet p in master.allPlanets) p.tr.enable();
                trailRenderer.drawAllSatelliteTrails(master.allSatellites);
            }
        };

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

        startMainLoop();

        //Debug.Log(position.J2000(new position(0, 1, 0), new position(0, 0, -1), new position(0, 1, 0)));
        //facility f1 = new facility("1", earth, new facilityData("1", new geographic(-33.60579, -78.88177), 10, new List<antennaData>()), new representationData("facility", "defaultMat"));
    }

    public static void runWindows()
    {
        master.time.addJulianTime((double)2461021.5 - (double)master.time.julian);
        master.requestPositionUpdate();
        dynamicLinkOptions options = new dynamicLinkOptions();
        options.callback = (data) => {
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
        options.callback = (data) => {
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

    public static void runDynamicLink() {
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

                    System.IO.File.WriteAllLines(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads)) + "/Access Call Results/" + provider.Key + " to " + user.Key +".txt", final);
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

    private void runScheduling() {
        string date = DateTime.Now.ToString("MM-dd_hhmm");
        //testing git on reset computer

        if (!File.Exists(ScheduleStructGenerator.path("main.db"))) {
            Debug.Log("Generating main.db");
            ScheduleStructGenerator.runExe(
                "parser.exe",
                $"{ScheduleStructGenerator.path("ScenarioAssetsSTK_2_w_pivot.xlsx")} {ScheduleStructGenerator.path("main.db")}",
                true);
        }

        var missionStructure = DBReader.getData();
        System.IO.Directory.CreateDirectory(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), date));
        //string json = JsonConvert.SerializeObject(missionStructure, Formatting.Indented);
        //System.IO.File.WriteAllText (@"NewMissionStructure.txt", json
        Debug.Log("Generating windows.....");
        ScheduleStructGenerator.genDB(missionStructure, "RAC_2-1", ScheduleStructGenerator.path("LunarWindows-RAC2_1_07_19_22.json"), date, "PreconWindows");
        Debug.Log("Generating conflict list.....");
        ScheduleStructGenerator.createConflictList(date);
        //Debug.Log("Regenerating windows");
        //ScheduleStructGenerator.genDB(missionStructure, "RAC_2-1", "LunarWindows-RAC2_1_07_19_22.json", date, "PostconWindows");
        Debug.Log("Doing DFS.....");
        ScheduleStructGenerator.doDFS(date);
        ScheduleStructGenerator.runExe(
            "heatmap.exe",
            $"{ScheduleStructGenerator.output("PreDFSUsers.txt", date)} {ScheduleStructGenerator.output($"PreDFSUsers_{date}.png", date)}");
        ScheduleStructGenerator.runExe(
            "heatmap.exe",
            $"{ScheduleStructGenerator.output("PostDFSUsers.txt", date)} {ScheduleStructGenerator.output($"PostDFSUsers_{date}.png", date)}",
            callback: () => Debug.Log("Scheduling finished"));
    }

    public void startMainLoop(bool force = false) {
        if (loop != null && force == false) return;

        loop = StartCoroutine(general.internalClock(tickrate, int.MaxValue, (tick) => {
            if (!general.blockMainLoop) {
                if (master.pause) {
                    master.tickStart(master.time);
                    master.time.addJulianTime(0);
                } else {
                    Time t = new Time(master.time.julian);
                    t.addJulianTime(speed);
                    master.tickStart(t);
                    master.time.addJulianTime(speed);
                }
            }

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

        if (Input.GetKeyDown("o")) {
            Vector3 v1 = (Vector3) (geographic.toCartesian(new geographic(0, 0), earth.radius).swapAxis());
            Vector3 v2 = (Vector3) (geographic.toCartesianWGS(new geographic(0, 0), 0).swapAxis());

            Debug.Log("regular: " + v1);
            Debug.Log("wgs: " + v2);

            runWindowsNoRate();
        }
        
        if (Input.GetKeyDown("k")) {
            string src = Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth/juan");
            var f = new universalTerrainJp2File(Path.Combine(src, "data.jp2"), Path.Combine(src, "metadata.txt"));

            Material m = Resources.Load<Material>("Materials/vis/juanVis");
            f.load(Vector2.zero, Vector2.one, 0, 0, default(position)).drawAll(m, resLoader.load<GameObject>("planetMesh"), new string[0], earth.representation.gameObject.transform);
        }

        if (Input.GetKeyDown("g")) {
            string src = Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth");
            string p = Directory.GetDirectories(src)[stationIndex];
            universalTerrainJp2File f = new universalTerrainJp2File(Path.Combine(p, "data.jp2"), Path.Combine(p, "metadata.txt"), true);
            Debug.Log(p);

            if (prevDist != null) prevDist.clear();
            geographic offset = new geographic(1, 1);
            prevDist = f.load(f.center, 0, f.getBestResolution(f.center - offset, f.center + offset, 5_000_000), offset: 1);

            prevDist.drawAll(earth.representation.gameObject.transform);

            stationIndex++;

            if (stationIndex >= 20) stationIndex = 0;
        }

        if (Input.GetKeyDown("y")) {
            accessCallGeneratorWGS access = new accessCallGeneratorWGS(earth, new geographic(-35.398522, 148.981904), sat1);
            access.initialize(Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth/canberra"), 2);
            //var output = access.findTimes(new Time(2461021.77854328), new Time(2461029.93452393), 0.00069444444, 0.00001157407 / 2.0);
            //var output = access.findTimes(new Time(2461021.77854328 + 0.0002), new Time(2461021.77991930), 0.00069444444, 0.00001157407 / 2.0);
            //access.saveResults(output);
            StartCoroutine(stall(access));
        }
    }

    private IEnumerator stall(accessCallGeneratorWGS access) { // TODO: replace with a physics update call
        yield return new WaitForSeconds(1);
        //var output = access.findTimes(new Time(2461022.77871296), new Time(2461022.78237024), 0.00069444444, 0.00001157407 / 2.0);

        //var output = access.findTimes(new Time(2461021.77854328), new Time(2461029.93452393), 0.00069444444, 0.00001157407 / 2.0);
        var output = access.findTimes(new Time(2459560.84525522), new Time(2459570.27537285), 0.00069444444, 0.00001157407 / 2.0);
        //var output = access.bruteForce(new Time(2461021.77854328), new Time(2461022.93452393), 0.00001157407);
        access.saveResults(output);
    }

    int stationIndex = 0;
    meshDistributor<universalTerrainMesh> prevDist;

    private IEnumerator JPL() {
        representationData rd = new representationData("planet", "defaultMat");

        representationData frd = new representationData("facility", "defaultMat");

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666667;

        string header = "dec2025";

        double EarthMu = 398600.0;

        earth = new planet(  "Earth", new planetData(6356.75, rotationType.earth,   $"CSVS/JPL/{header}/PLANETS/earth", oneHour, planetType.planet), new representationData("planet", "earthTex"));
        moon =  new planet(   "Luna", new planetData(1738.1,  rotationType.moon,    $"CSVS/JPL/{header}/PLANETS/Luna",  oneHour,   planetType.moon), new representationData("planet", "moonTex"));
        planet mercury = new planet("Mercury", new planetData(2439.7,  rotationType.none, $"CSVS/JPL/{header}/PLANETS/mercury", oneHour, planetType.planet), new representationData("planet", "mercuryTex"));
        planet venus = new planet(  "Venus", new planetData(6051.8,  rotationType.none,   $"CSVS/JPL/{header}/PLANETS/venus", oneHour, planetType.planet), new representationData("planet", "venusTex"));
        planet jupiter = new planet("Jupiter", new planetData( 71492,  rotationType.none, $"CSVS/JPL/{header}/PLANETS/jupiter", oneHour, planetType.planet), new representationData("planet", "jupiterTex"));
        planet saturn = new planet( "Saturn", new planetData( 60268,  rotationType.none,  $"CSVS/JPL/{header}/PLANETS/saturn", oneHour, planetType.planet), new representationData("planet", "saturnTex"));
        planet uranus = new planet( "Uranus", new planetData( 25559,  rotationType.none,  $"CSVS/JPL/{header}/PLANETS/uranus", oneHour, planetType.planet), new representationData("planet", "uranusTex"));
        planet neptune = new planet("Neptune", new planetData( 24764,  rotationType.none, $"CSVS/JPL/{header}/PLANETS/neptune", oneHour, planetType.planet), new representationData("planet", "neptuneTex"));
        planet mars = new planet(   "Mars", new planetData(3389.92,  rotationType.none,    $"CSVS/JPL/{header}/PLANETS/mars", oneHour, planetType.planet), new representationData("planet", "marsTex"));

        planet.addFamilyNode(earth, moon);

        yield return new WaitForSeconds(0.1f);
        loadingController.addPercent(0.11f);

        facility svalbard = new facility("Svalbard", earth, new facilityData("Svalbard", new geographic(77.875, 20.9752), .001, new List<antennaData>()), new representationData("facility", "defaultMat"));
        facility ASF = new facility("ASF", earth, new facilityData("ASF", new geographic(64.8401, -147.72), .001, new List<antennaData>()), new representationData("facility", "defaultMat"));
        sat1 = new satellite("Sat1", new satelliteData(new Timeline(6378.1 + 900, 0, 98, 0, 0, 0, 0, 2461021.5, EarthMu)), rd);

        facility np = new facility("North Pole", earth, new facilityData("North Pole", new geographic(90, 0), 0, new List<antennaData>()), new representationData("facility", "defaultMat"));
        facility eq = new facility("Equator", earth, new facilityData("Equator", new geographic(0, 0), .001, new List<antennaData>()), new representationData("facility", "defaultMat"));


        body.addFamilyNode(earth, sat1);
        body.addFamilyNode(earth, moon);

        master.relationshipSatellite[earth] = new List<satellite>() { sat1 };

        master.relationshipPlanet[earth] = new List<planet>() {moon};
        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/PLANETS/moon", oneMin));
        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/SATS/v", 0.0006944444));


        linkBudgeting.users.Add("Sat1", (false, 2461021.5, 2461051.5));
        linkBudgeting.providers.Add("ASF", (true, 2461021.5, 2461051.5));
        linkBudgeting.providers.Add("Svalbard", (true, 2461021.5, 2461051.5));
        linkBudgeting.providers.Add("Equator", (true, 2461021.5, 2461051.5));

        loadingController.addPercent(1);

        //master.time.addJulianTime(new Time(new DateTime(2026, 7, 12)).julian - master.time.julian);
        //master.time.addJulianTime(new Time(new DateTime(2027, 9, 1)).julian - master.time.julian);
    }

    public void OnApplicationQuit() {
        IMesh.clearCache();
    }
}
