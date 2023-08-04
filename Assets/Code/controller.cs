using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using Newtonsoft.Json;

public class controller : MonoBehaviour
{
    public static planet earth, moon;
    public static planet defaultReferenceFrame;
    public static double speed = 0.0000116;
    public static int tickrate = 7200;
    private Coroutine loop;
    public static bool useTerrainVisibility = false;
    public static controller self;
    public static double scenarioStart;
    public static bool accessRunning = false;
    public static bool schedRunning = false;
    public static string assets = "AssetsDFSTest.xlsx";

    private void Awake() { self = this; }

    private void Start() {
        general.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.planetParent = GameObject.FindGameObjectWithTag("planet/parent");
        uiHelper.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.camera = Camera.main;

        resLoader.initialize();

        loadingController.start(new Dictionary<float, string>() {
            {0, "Connecting to server"},
            {0.1f, "Generating Planets"},
            {0.25f, "Parsing Database"},
            {0.75f, "Finalizing"}
        });

        StartCoroutine(start());
    }

    IEnumerator start() {
        yield return null;
        master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/sun", 0.0416666665, planetType.planet), new representationData("planet", "sunTex"));
        yield return StartCoroutine(web.initialize());

        loadingController.addPercent(0.1f);
        yield return new WaitForSeconds(0.1f);

        IScenario scenario = null;
        UnityEngine.Debug.Log("passed connection");
        if (web.isClient) scenario = new serverScenario(); // download scenario from server
        else scenario = new jplScenario();

        yield return StartCoroutine(scenario.generate());

        moon = (planet) scenario.metadata.importantBodies["Luna"];
        earth = (planet) scenario.metadata.importantBodies["Earth"];
        scenarioStart = scenario.metadata.timeStart;

        defaultReferenceFrame = earth;

        master.time.addJulianTime(scenarioStart - master.time.julian);

        yield return null;

        master.setReferenceFrame(master.allPlanets.First(x => x.name == "Earth"));

        master.markStartOfSimulation();
        modeController.initialize();

        if (planetFocus.instance.lunarTerrainFilesExist) {
            general.pt = terrainStartup.loadTerrain(moon);
            general.plt = terrainStartup.loadLunarPoles(moon);
        }

        startMainLoop();
        master.pause = false;

        yield return null;
        loadingController.addPercent(1);

        defaultMode.instance.runIntroAnimation();
    }

#if (UNITY_EDITOR || UNITY_STANDALONE) && !UNITY_WEBGL
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
#endif

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

#if (UNITY_EDITOR || UNITY_STANDALONE) && !UNITY_WEBGL
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
        if (Input.GetKeyDown("l"))
        {
            StartCoroutine(ScheduleStructGenerator.doScheduleWithAccess());
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
        
        if (Input.GetKeyDown("u"))
        {
            string date = DateTime.Now.ToString("MM-dd_hhmm");
            if (!File.Exists(DBReader.mainDBPath))
            {
                UnityEngine.Debug.Log("Generating main.db");
                System.Diagnostics.Process.Start(DBReader.apps.excelParser, $"{DBReader.data.get(assets)} {DBReader.mainDBPath}").WaitForExit();
            }
            
            var missionStructure = DBReader.getData();
            UnityEngine.Debug.Log("epoch: " + missionStructure["EarthTest"].epoch);
            DBReader.output.setOutputFolder(Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), date));
            string json = JsonConvert.SerializeObject(missionStructure, Formatting.Indented);
            DBReader.output.write("MissionStructure_2023.txt", json);
            UnityEngine.Debug.Log("Generating windows.....");
            ScheduleStructGenerator.genDB(missionStructure, "EarthTest", "DFSTestwindows.json", date, "PreconWindows");
            UnityEngine.Debug.Log("Generating conflict list.....");
            //ScheduleStructGenerator.createConflictList(date);
            ScheduleStructGenerator.genDBNoJSON(missionStructure, date, "cut1Windows");
            //ScheduleStructGenerator.createConflictList(date);
            //UnityEngine.Debug.Log("Doing DFS.....");
            ScheduleStructGenerator.doDFS(date);
            UnityEngine.Debug.Log(DBReader.output.getClean("PostDFSUsers.txt"));
            //System.Diagnostics.Process.Start(DBReader.apps.heatmap, $"{DBReader.output.getClean("PostDFSUsers.txt")} {DBReader.output.get("PostDFSUsers", "png")} 0 1 6");
            //System.Diagnostics.Process.Start(DBReader.apps.heatmap, $"{DBReader.output.getClean("PreDFSUsers.txt")} {DBReader.output.get("PreDFSUsers", "png")} 0 1 6");
            System.Diagnostics.Process.Start(DBReader.apps.schedGen, $"{DBReader.output.get("ScheduleCSV", "csv")} source destination 0 1 {DBReader.output.get("sched", "png")} 0");
        }
#endif
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

    public void OnApplicationQuit() {IMesh.clearCache();}
}
