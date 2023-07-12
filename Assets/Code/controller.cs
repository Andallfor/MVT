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
        master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/JPL/sep2027/PLANETS/sun", 0.0416666665, planetType.planet),
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
        general.pt = loadTerrain();
        general.plt = loadPoles();

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

            general.pt.updateTerrain();

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

        if (Input.GetKeyDown("p")) {
            int sy = 450;
            int sx = 900;
            meshDistributor<universalTerrainMesh> m = new meshDistributor<universalTerrainMesh>(new Vector2Int(sx, sy), Vector2Int.zero, Vector2Int.zero);
            for (int r = 0; r < sy; r++) {
                for (int c = 0; c < sx; c++) {
                    geographic g = new geographic(180.0 * (double) r / (double) sy - 90.0, 360.0 * (double) c / (double) sx - 180.0);
                    m.addPoint(c, r, g.toCartesianWGS(0).swapAxis() / (master.scale));
                }
            }
            m.drawAll(earth.representation.gameObject.transform);
            foreach (IMesh child in m.allMeshes) child.addCollider();
        }

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
            f.overrideToCart(geographic.toCartesianWGS);

            Material m = Resources.Load<Material>("Materials/vis/juanVis");
            f.load(Vector2.zero, Vector2.one, 0, 0, default(position)).drawAll(m, resLoader.load<GameObject>("planetMesh"), new string[0], earth.representation.gameObject.transform);
        }

        if (Input.GetKeyDown("g")) {
            string src = Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth");
            string p = Directory.GetDirectories(src)[stationIndex];
            universalTerrainJp2File f = new universalTerrainJp2File(Path.Combine(p, "data.jp2"), Path.Combine(p, "metadata.txt"), true);
            f.overrideToCart(geographic.toCartesianWGS);
            Debug.Log(p);

            if (prevDist != null) prevDist.clear();
            geographic offset = new geographic(1, 1);
            prevDist = f.load(f.center, 0, f.getBestResolution(f.center - offset, f.center + offset, 5_000_000), offset: 1);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            prevDist.drawAll(earth.representation.gameObject.transform);
            Debug.Log($"Time to draw mesh: {sw.ElapsedMilliseconds}");

            stationIndex++;

            if (stationIndex >= 20) stationIndex = 0;
        }

        if (Input.GetKeyDown("y")) {
            accessCallGeneratorWGS access = new accessCallGeneratorWGS(earth, new geographic(-35.398522, 148.981904), sat1);
            access.initialize(Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth/canberra"), 2);
            //var output = access.findTimes(new Time(2461021.77854328), new Time(2461029.93452393), 0.00069444444, 0.00001157407 / 2.0);
            //var output = access.findTimes(new Time(2461021.77854328 + 0.0002), new Time(2461021.77991930), 0.00069444444, 0.00001157407 / 2.0);
            //access.saveResults(output);
            //StartCoroutine(stall(access));
            
            /*
            string p = Path.Combine(Application.streamingAssetsPath, "terrain/facilities/earth/canberra");
            universalTerrainJp2File f = new universalTerrainJp2File(Path.Combine(p, "data.jp2"), Path.Combine(p, "metadata.txt"));
            f.overrideToCart(geographic.toCartesianWGS);

            geographic g = new geographic(-35.398522, 148.981904);
            double alt = f.getHeight(g);

            GameObject go = resLoader.createPrefab("defaultPrefab");
            go.transform.position = earth.representation.gameObject.transform.rotation * (Vector3) (g.toCartesianWGS(alt).swapAxis() / master.scale);

            accessCallGeneratorWGS access = new accessCallGeneratorWGS(earth, g, alt, sat1);

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            meshDistributor<universalTerrainMesh> mesh = f.load(Vector2.zero, Vector2.one, 0, 2);
            long s1 = sw.ElapsedMilliseconds;
            Debug.Log("Time to load: " + s1);

            s1 = sw.ElapsedMilliseconds;
            mesh.drawAll(earth.representation.gameObject.transform);
            Debug.Log("Time to draw mesh: " + (sw.ElapsedMilliseconds - s1));

            s1 = sw.ElapsedMilliseconds;
            
            //foreach (IMesh child in mesh.allMeshes) child.addCollider();
            //var output = access.bruteForce(new Time(2461021.77854328), new Time(2461029.93452393), 0.00001157407);
            //var output = access.bruteForce(new Time(2461021.95), new Time(2461022.05), 0.00001157407);
            //var output = access.bruteForce(new Time(2461026.84968476), new Time(2461026.86275181), 0.00001157407);
            //var output = access.findTimes(new Time(2461026.92073842), new Time(2461026.93171353), 0.00069444444, 0.00001157407 / 2.0);
            var output = access.findTimes(new Time(2461022.84641104), new Time(2461029.93452393), 0.00069444444, 0.00001157407 / 2.0);
            //var output = access.findTimes(new Time(2461026.85107154), new Time(2461029.93452393), 0.00069444444, 0.00001157407 / 2.0);
            Debug.Log("Time to generate access calls: " + (sw.ElapsedMilliseconds - s1));

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < output.Count; i++) {
                accessCallTimeSpan span = output[i];
                sb.AppendLine($"{i},{span.start},{span.end},{span.end-span.start}");
            }

            File.WriteAllText("C:/Users/leozw/Desktop/accessNew.csv", sb.ToString());
            */
        }
    }

    private IEnumerator stall(accessCallGeneratorWGS access) {
        yield return new WaitForSeconds(1);
        var output = access.findTimes(new Time(2461021.77854328 + 0.0002), new Time(2461021.77991930), 0.00069444444, 0.00001157407 / 2.0);
        access.saveResults(output);
    }

    int stationIndex = 0;
    meshDistributor<universalTerrainMesh> prevDist;

    private planetTerrain loadTerrain() {
        planetTerrain pt = new planetTerrain(moon, "Materials/planets/moon/moon", 1737.4, 1);

        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MVT/terrain");
        //string p = Path.Combine(Application.dataPath, "terrain");

        pt.generateFolderInfos(new string[4] {
            Path.Combine(p, "lunaBinary/1"),
            Path.Combine(p, "lunaBinary/2"),
            Path.Combine(p, "lunaBinary/3"),
            Path.Combine(p, "lunaBinary/4")
        });

        //string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MVT");
        //pt.save("C:/Users/leozw/Desktop/terrain/lunaBinary/1", Path.Combine(p, "1"));
        //pt.save("C:/Users/leozw/Desktop/terrain/lunaBinary/2", Path.Combine(p, "2"));
        //pt.save("C:/Users/leozw/Desktop/terrain/lunaBinary/3", Path.Combine(p, "3"));
        //pt.save("C:/Users/leozw/Desktop/terrain/lunaBinary/4", Path.Combine(p, "4"));
        //pt.save("C:/Users/leozw/Desktop/terrain/lunaBinary/5", Path.Combine(p, "5"));

        //terrainProcessor.divideJpeg2000("C:/Users/leozw/Desktop/lunar", "C:/Users/leozw/Desktop/preparedLunar", new List<terrainResolution>() {
        //    new terrainResolution("C:/Users/leozw/Desktop/preparedLunar/1", 1, 96),
        //    new terrainResolution("C:/Users/leozw/Desktop/preparedLunar/2", 1, 48),
        //    new terrainResolution("C:/Users/leozw/Desktop/preparedLunar/3", 1, 24),
        //    new terrainResolution("C:/Users/leozw/Desktop/preparedLunar/4", 1, 12),
        //    new terrainResolution("C:/Users/leozw/Desktop/preparedLunar/5", 1, 6),
        //    new terrainResolution("C:/Users/leozw/Desktop/preparedLunar/6", 4, 3),
        //});

        return pt;
    }

    private poleTerrain loadPoles() {
        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MVT/terrain");
        //string p = Path.Combine(Application.dataPath, "terrain");
        return new poleTerrain(new Dictionary<int, string>() {
            {5,  Path.Combine(p, "polesBinary/25m")},
            {10, Path.Combine(p, "polesBinary/50m")},
            {20, Path.Combine(p, "polesBinary/100m")}
        }, moon.representation.gameObject.transform);
    }

    private IEnumerator JPL() {
        representationData rd = new representationData("planet", "defaultMat");

        representationData frd = new representationData("facility", "defaultMat");

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666667;

        string header = "dec2025";
        //string header = "jul2026";
        //string header = "sep2027";

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

    private IEnumerator Artemis3()
    {
        List<satellite> moonSats =  new List<satellite>();
        List<satellite> earthSats =  new List<satellite>();

        Dictionary<string, string> realmodelPathes = new Dictionary<string, string>() {
            {"LRO", "Prefabs/models/LRO" },
            {"CubeSat", "Prefabs/models/Cubesat"},
            {"Orion", "Prefabs/models/OrionFull"},
            {"HLS", "Prefabs/models/HLS Lander"},
            {"Gateway", "Prefabs/models/OCO" }
        };

        /*Dictionary<string, sting> realmodelPathes = new Dictionary<string, string>() {
            {"LRO", "Prefabs/models/LRO" },
            {"CubeSat", "Prefabs/models/Cubesat"},
            {"Orion", "Prefabs/models/OrionFull"},
            {"HLS", "Prefabs/models/HLS Lander"},
            {"Gateway", "Prefabs/models/OCO" }
        };
        all else default to solar-b

            "Prefabs/models/AIM",
            "Prefabs/models/Aura",
            "Prefabs/models/GOES",
            "Prefabs/models/GRACE",
            "Prefabs/models/ICESAT",
            "Prefabs/models/ICON",
            "Prefabs/models/LDCM",
            "Prefabs/models/MMS",
            "Prefabs/models/OCO",
            "Prefabs/models/SDO",
            "Prefabs/models/Solar-B",
            "Prefabs/models/TDRS",
            "Prefabs/models/TRIANA"};*/


        representationData rd = new representationData(
            "Prefabs/Planet",
            "Materials/default");

        representationData frd = new representationData(
            "Prefabs/Facility",
            "Materials/default");

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666667;
        double oneSec = 0.00001157;

        double MoonMu = 4902.800066;
        double MarsMu = 4.2828375815756095E+04;
        double UranusMu = 5.7939556417959081E+06;
        double NeptuneMu = 6.8351025518691950E+06;
        double SunMu = 1.3271244091061847E+11;
        double JupMu = 1.2668973461247002E+08;
        double SatMu = 3.7940184296380058E+07;

        earth = new planet(  "Earth", new planetData(  6371, rotationType.earth,   "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/earth/earthEquirectangular"));
        moon =  new planet(   "Luna", new planetData(1738.1,  rotationType.moon,    "CSVS/ARTEMIS 3/PLANETS/moon",  oneMin,   planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/moon/moon"));
                new planet("Mercury", new planetData(2439.7,  rotationType.none, "CSVS/ARTEMIS 3/PLANETS/mercury", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/mercury"));
                new planet(  "Venus", new planetData(6051.8,  rotationType.none,   "CSVS/ARTEMIS 3/PLANETS/venus", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/venus"));

        planet pluto = new planet("Pluto", new planetData(1188.3, rotationType.none, new Timeline(5.946851918231975E+09, 2.503377465169019E-01, 2.347223218001061E+01, 1.844505615088283E+02, 4.441083109363277E+01, 5.036690090541509E+01, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), SunMu), 1, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/pluto"));
        planet charon = new planet("Charon", new planetData(603.6, rotationType.none, new Timeline(1.959426743163140E+04, 1.295552542515501E-04, 9.623268195064630E+01, 1.463964186819785E+02, 2.230282305685080E+02, 1.592839892403547E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), 9.7559000499039507E+02), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/pluto/charon"));

        planet mars = new planet(   "Mars", new planetData(3389.92,  rotationType.none,    "CSVS/ARTEMIS 3/PLANETS/mars", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/mars"));
        planet deimos = new planet("Deimos", new planetData(6.9, rotationType.none, new Timeline(23458.30390813599, 2.130593815196214E-04, 2.458935818421859E+01, 3.539530138717170E+02, 7.976620457709659E+01, 2.361661169839224E+02, 1, Time.strDateToJulian("2022 May 11 00:00:00.0000"), MarsMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/deimos"));
        planet phobos = new planet("Phobos", new planetData(13.1, rotationType.none, new Timeline(9.378107274617230E+03, 3.639882214816549E-01, 1.011880520567134E+02, 1.315971874726009E+02, 9.280370816213794E+01, 2.712109828748827E+02, 1, Time.strDateToJulian("2022 May 11 00:00:00.0000"), MarsMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));

        //semiMajorAxis, eccentricity, inclination, argOfPerigee, longOfAscNode, meanAnom, mass, startingEpoch, mu)
        planet jupiter = new planet("Jupiter", new planetData( 71492,  rotationType.none, "CSVS/ARTEMIS 3/PLANETS/jupiter", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/jupiter"));
        planet europa = new planet("Europa", new planetData(1560.8, rotationType.none, new Timeline(6.712324897297744E+05, 9.756905445059905E-03, 2.558300580281146E+01, 3.130131294888411E+02, 3.570102271715173E+02, 3.771874902292626E+01, 1, Time.strDateToJulian("2022 May 11 00:00:00.0000"), JupMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/deimos"));
        planet io = new planet("Io", new planetData(1821.49, rotationType.none, new Timeline(4.220430357408057E+05, 4.821098954882220E-03, 2.548577417307722E+01, 3.848739106075252E+01, 3.581155318564605E+0, 3.543937511177574E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), JupMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));
        planet ganymede = new planet("Ganymede", new planetData(2631.2, rotationType.none, new Timeline(1.070738103475128E+06, 2.517784289295191E-03, 2.563701513408245E+01, 3.167666466728974E-01, 3.581006093585235E+02, 3.796286236463462E+01, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), JupMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/deimos"));
        planet callisto = new planet("Callisto", new planetData(2410.3, rotationType.none, new Timeline(1.883803333437284E+06, 7.596813727389104E-03, 2.524194247789023E+01, 1.371302989181364E+01, 3.581925397030662E+02, 4.758217360835086E+01, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), JupMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));

        planet saturn = new planet( "Saturn", new planetData( 60268,  rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/saturn", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/saturn"));
        planet titan = new planet("Titan", new planetData(2575.5, rotationType.none, new Timeline(1.221912832749956E+06, 2.875883966971324E-02, 6.347680885407659E+00, 2.206736537274180E+02, 1.271359206097018E+02, 8.699496231782938E+01, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), SatMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));
        planet hyperion = new planet("Hyperion", new planetData(2575.5, rotationType.none, new Timeline(1.221912832749956E+06, 1.042783332287711E-01, 5.759125870436669E+00, 1.417386351404256E+02, 1.238025423593874E+02, 2.491657442861929E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), SatMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));
        planet iapetus = new planet("Iapetus", new planetData(734.5, rotationType.none, new Timeline(3.563644468397091E+06, 2.857578350158145E-02, 1.519294491839505E+01, 3.271589910409460E+02, 4.720769743987517E+01, 1.134990096632687E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), SatMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));
        planet mimas = new planet("Mimas", new planetData(198.8, rotationType.none, new Timeline(1.860003958447655E+05, 1.717384069676054E-02, 6.818744491935989E+00, 6.867092361875247E+01, 1.172616091651329E+02, 1.840503936823107E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), SatMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));
        planet enceladus = new planet("Enceladus", new planetData(252.3, rotationType.none, new Timeline(2.384066089079195E+05, 3.081798856844249E-03, 6.461966878473253E+00, 6.476454936102776E+01, 1.306192136869088E+02, 1.690374658351706E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), SatMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));
        planet tethys = new planet("Tethys", new planetData(536.3, rotationType.none, new Timeline(2.949749029184564E+05, 8.960832414480428E-04, 5.780841108691875E+00, 1.948155842927517E+02, 1.225571840273833E+02, 3.523350802488472E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), SatMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/mars/phobos"));

        planet uranus = new planet( "Uranus", new planetData( 25559,  rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/uranus", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/uranus"));
        planet Miranda = new planet("Miranda", new planetData(234, rotationType.none, new Timeline(1.298785496440501E+05, 1.462399811917616E-03, 7.752457010942014E+01, 4.647842701042226E+01, 1.637233061394332E+02, 1.964047613631384E+01, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), UranusMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/uranus/miranda"));
        planet Ariel = new planet("Ariel", new planetData(13.1, rotationType.none, new Timeline(1.909441966549205E+05, 2.715880741572296E-04, 7.480435400375895E+01, 2.006987238262219E+02, 1.673459380967358E+02, 2.348289370284962E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), UranusMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/uranus/ariel"));
        planet Umbriel = new planet("Umbriel", new planetData(584.7, rotationType.none, new Timeline(2.659991846819316E+05, 3.143845888778475E-03, 7.481041730848607E+01, 6.308114262333238E+01, 1.673669802140787E+02, 1.242312590984253E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), UranusMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/uranus/umbriel"));
        planet Titania = new planet("Titania", new planetData(788.9, rotationType.none, new Timeline(4.362772370020116E+05, 2.035055161547300E-03, 7.487106697860010E+01, 2.344449913034235E+02, 1.673029998988725E+02, 3.433031265687955E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), UranusMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/uranus/titana"));
        planet Oberon = new planet("Oberon", new planetData(761.4, rotationType.none, new Timeline(5.834837422883826E+05, 7.754003500727179E-04, 7.500713668028843E+01, 2.158441350078671E+02, 1.673937473690337E+02, 1.044032711436084E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), UranusMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/uranus/oberon"));

        planet neptune = new planet("Neptune", new planetData( 24764,  rotationType.none, "CSVS/ARTEMIS 3/PLANETS/neptune", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/neptune"));
        planet Proteus = new planet("Proteus", new planetData(208, rotationType.none, new Timeline(1.176751084140828E+05, 6.698630624651811E-04, 4.759369671202530E+01, 3.565033778835370E+02, 2.962923214096653E+01, 3.283481977496181E+02, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), NeptuneMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/neptune/proteus"));
        planet Triton = new planet("Triton", new planetData(1352.6, rotationType.none, new Timeline(3.547667476641174E+05, 1.412643162056324E-05, 1.106056038830452E+02, 4.917714675888436, 2.140211313394768E+02, 3.248133065059064E+01, 1, Time.strDateToJulian("2025 May 11 00:00:00.0000"), NeptuneMu), 1, planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/Moons/neptune/triton"));

        yield return new WaitForSeconds(0.1f);
        loadingController.addPercent(0.11f);

        var data = DBReader.getData();
        float percentIncrease = 0.74f / (float) data["Artemis_III"].satellites.Count;
        foreach (KeyValuePair<string, dynamic> x in data["Artemis_III"].satellites) {

            var dict = data["Artemis_III"].satellites[x.Key];

            if (dict["Type"] == "Satellite") {
                if (x.Key.Contains("LowPower")) continue;
                if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (false, 2460806.5 + dict["TimeInterval_start"], 2460806.5 + dict["TimeInterval_stop"]));
                if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (false, 2460806.5 + dict["TimeInterval_start"], 2460806.5 + dict["TimeInterval_stop"]));
                if (dict["user_provider"] == "user/provider")
                {
                    linkBudgeting.providers.Add(x.Key, (false, 2460806.5 + dict["TimeInterval_start"], 2460806.5 + dict["TimeInterval_stop"]));
                    linkBudgeting.users.Add(x.Key, (false, 2460806.5 + dict["TimeInterval_start"], 2460806.5 + dict["TimeInterval_stop"]));
                }

                representationData srd = new representationData("Prefabs/models/Solar-B", "Materials/default");
                foreach (var kvp in realmodelPathes) {
                    if (x.Key.ToLower().Contains(kvp.Key.ToLower())) {
                        srd = new representationData(kvp.Value, "Materials/default");
                        break;
                    }
                }

                satellite sat = null;
                if (dict.ContainsKey("RAAN")) {
                    if (x.Key == "HLS-Ascent" | x.Key == "HLS-Descent") {
                        sat = new satellite(x.Key, new satelliteData(new Timeline(dict["SemimajorAxis"] / 1000, dict["Eccentricity"], dict["Inclination"], dict["Arg_of_Perigee"], dict["RAAN"], dict["MeanAnomaly"], 1, Time.strDateToJulian(dict["OrbitEpoch"]), MoonMu)), srd);
                    } else {
                        sat = new satellite(x.Key, new satelliteData(new Timeline(dict["SemimajorAxis"] / 1000, dict["Eccentricity"], dict["Inclination"], dict["Arg_of_Perigee"], dict["RAAN"], dict["MeanAnomaly"], 1, Time.strDateToJulian(dict["OrbitEpoch"]), MoonMu)), srd);
                    }
                } else if (dict.ContainsKey("FilePath")) {
                    if (x.Key == "HLS-Ascent" | x.Key == "HLS-Descent") {
                        sat = new satellite(x.Key, new satelliteData($"CSVS/ARTEMIS 3/SATS/{x.Key}", oneSec),  srd);
                    } else {
                        sat = new satellite(x.Key, new satelliteData($"CSVS/ARTEMIS 3/SATS/{x.Key}", oneMin), srd);
                    }
                }
                sat.positions.enableExistanceTime(new Time(2460806.5 + dict["TimeInterval_start"]), new Time((2460806.5 + dict["TimeInterval_stop"])));

                if (dict["CentralBody"] == "Moon") {
                    satellite.addFamilyNode(moon, sat);
                    moonSats.Add(sat);
                } else if (dict["CentralBody"] == "Earth") {
                    satellite.addFamilyNode(earth, sat);
                    earthSats.Add(sat);
                }
            }
            else if (dict["Type"] == "Facility") {
                if (dict["CentralBody"] == "Moon")
                {
                    double start = 0, stop = 0;
                    if (dict["TimeInterval_start"] is string) start = Double.Parse(dict["TimeInterval_start"], System.Globalization.NumberStyles.Any);
                    else start = (double) dict["TimeInterval_start"];

                    if (dict["TimeInterval_stop"] is string) stop = Double.Parse(dict["TimeInterval_stop"], System.Globalization.NumberStyles.Any);
                    else stop = (double) dict["TimeInterval_stop"];

                    facility fd = new facility(x.Key, moon, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), 0, new List<antennaData>(), new Time(2460806.5 + start), new Time(2460806.5 + stop)), frd);
                    List<antennaData> antenna = new List<antennaData>() {new antennaData(fd, x.Key, new geographic(dict["Lat"], dict["Long"]), dict["Schedule_Priority"], dict["Service_Level"], dict["Service_Period"])};

                    fd.data.antennas.Concat(antenna);


                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                } else {
                    facility fd = new facility(x.Key, earth, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), 0, new List<antennaData>()), frd);
                    List<antennaData> antenna = new List<antennaData>() {new antennaData(fd, x.Key, new geographic(dict["Lat"], dict["Long"]), dict["Ground_Priority"])};

                    fd.data.antennas.Concat(antenna);

                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                }
            }

            loadingController.addPercent(percentIncrease);
            yield return new WaitForSeconds(0.1f);
        }

        representationData sdrd = new representationData("Prefabs/models/Cubesat", "Materials/default");
        satellite s1 = new satellite("LRO" , new satelliteData(new Timeline(1.830311669462445E+03, 5.563299732641667E-03, 6.287160669332728E+01, 3.117131857299572E+02, 1.736144041385683E+02, 3.029852347030018E+02, 1, Time.strDateToJulian("2022 Oct 31 00:00:00.0000"), MoonMu)), sdrd);
        satellite.addFamilyNode(moon, s1);
        moonSats.Add(s1);
        satellite s2 = new satellite("MAVEN", new satelliteData(new Timeline(5.736328831395154E+03, 3.757983132436339E-01, 9.409525895127517E+01, 3.116978006182828E+02, 3.495978086657416E+02, 1.663479285527612E+02, 1, Time.strDateToJulian("2022 May 11 00:00:00.0000"), MarsMu)), sdrd);
        satellite.addFamilyNode(mars, s2);
        satellite s3 = new satellite("Mars Express", new satelliteData(new Timeline(8.814763859932871E+03, 5.778208721851736E-01, 8.239745770670901E+01, 3.332233703011546E+02, 1.814088356109900E+02, 6.518950359049940E+01, 1, Time.strDateToJulian("2022 May 11 00:00:00.0000"), MarsMu)), sdrd);
        satellite.addFamilyNode(mars, s3);
        satellite s4 = new satellite("Mars Orbiter Mission", new satelliteData(new Timeline(3.976530777818711E+04, 8.972508960261729E-01, 1.325774832084018E+02, 1.724246041330142E+02, 2.883266576204198E+02, 2.929292608939092E+02, 1, Time.strDateToJulian("2022 May 11 00:00:00.0000"), MarsMu)), sdrd);
        satellite.addFamilyNode(mars, s4);

        planet.addFamilyNode(pluto, charon);
        planet.addFamilyNode(master.sun, pluto);

        planet.addFamilyNode(mars, deimos);
        planet.addFamilyNode(mars, phobos);

        planet.addFamilyNode(jupiter, europa);
        planet.addFamilyNode(jupiter, io);
        planet.addFamilyNode(jupiter, ganymede);
        planet.addFamilyNode(jupiter, callisto);

        planet.addFamilyNode(saturn, titan);
        planet.addFamilyNode(saturn, tethys);
        planet.addFamilyNode(saturn, iapetus);
        planet.addFamilyNode(saturn, hyperion);
        planet.addFamilyNode(saturn, enceladus);
        planet.addFamilyNode(saturn, mimas);

        planet.addFamilyNode(uranus, Ariel);
        planet.addFamilyNode(uranus, Miranda);
        planet.addFamilyNode(uranus, Umbriel);
        planet.addFamilyNode(uranus, Titania);
        planet.addFamilyNode(uranus, Oberon);

        planet.addFamilyNode(neptune, Triton);
        planet.addFamilyNode(neptune, Proteus);

        planet.addFamilyNode(earth, moon);

        master.setReferenceFrame(moon);
        master.relationshipPlanet[neptune] = new List<planet>() { Triton, Proteus };
        master.relationshipPlanet[saturn] = new List<planet>() { titan, tethys, iapetus, hyperion, enceladus, mimas };
        master.relationshipPlanet[uranus] = new List<planet>() { Ariel, Miranda, Umbriel, Titania, Oberon };
        master.relationshipPlanet[mars] = new List<planet>() { deimos, phobos };
        master.relationshipPlanet[jupiter] = new List<planet>() { io, europa, ganymede, callisto };
        master.relationshipPlanet[earth] = new List<planet>() {moon};
        master.relationshipPlanet[pluto] = new List<planet>() { charon };
        master.relationshipSatellite[moon] = moonSats;
        master.relationshipSatellite[earth] = earthSats;
        master.relationshipSatellite[mars] = new List<satellite>() { s2,s3,s4};
        foreach (satellite s in master.allSatellites) s.representation.setRelationshipParent();

        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/PLANETS/moon", oneMin));
        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/SATS/v", 0.0006944444));

        loadingController.addPercent(0.1f);
        yield return null;
    }
}
