using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.SceneManagement;

public class controller : MonoBehaviour
{
    public static planet earth, moon;
    public static planet defaultReferenceFrame;
    public static double speed = 0.00005;
    public static int tickrate = 7200;
    private Vector3 planetFocusMousePosition, planetFocusMousePosition1;
    private Coroutine loop;
    public static bool useTerrainVisibility = false;

    void Start()
    {
        master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/sun", 0.0416666665, planetType.planet),
            new representationData(
                "Prefabs/Planet",
                "Materials/planets/sun"));

        Artemis3();
        defaultReferenceFrame = moon;
        //onlyEarth();

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

        master.pause = false;
        general.camera = Camera.main;

        master.markStartOfSimulation();

        runDynamicLink();
        //linkBudgeting.accessCalls("C:/Users/akazemni/Desktop/connections.txt");

        initModes();

        startMainLoop();

        //Debug.Log(position.J2000(new position(0, 1, 0), new position(0, 0, -1), new position(0, 1, 0)));
    }

    private void initModes() { // i dont wanna hear it
        planetFocus.enable(true);
        planetFocus.enable(false);
        planetOverview.enable(true);
        planetOverview.enable(false);
        uiMap.map.toggle(true);
        uiMap.map.toggle(false);
    }

    private void runDynamicLink() {
        dynamicLinkOptions options = new dynamicLinkOptions();
        options.callback = (data) => {
            int c = 0;
            foreach (var v in data.Values) {
                c += v.time.Count;
            }
            Debug.Log($"Recieved data that contains {data.Count} entries, totaling {c} possible connections");

            useTerrainVisibility = false;
        };
        options.debug = true;
        options.outputPath = Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), "data.txt");

        useTerrainVisibility = true;

        List<object> users = new List<object>();
        List<object> providers = new List<object>();

        foreach (var u in linkBudgeting.users)
        {
            if (u.Value.Item1)
            {
                users.Add(master.allFacilites.Find(x => x.name == u.Key));
                Debug.Log("user: " + u.Key);
            }
            else
            {
                users.Add(master.allSatellites.Find(x => x.name == u.Key));
                Debug.Log("user: " + u.Key);
            }
        }

        foreach (var p in linkBudgeting.providers)
        {
            if (p.Value.Item1)
            {
                providers.Add(master.allFacilites.Find(x => x.name == p.Key));
                Debug.Log("provider: " + p.Key);
            }
            else
            {
                providers.Add(master.allSatellites.Find(x => x.name == p.Key));
                Debug.Log("provider: " + p.Key);
            }
        }

        StartCoroutine(visibility.raycastTerrain(
            providers, users, master.time.julian, master.time.julian + 30, speed, options));
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

            if (!planetOverview.usePlanetOverview) master.requestSchedulingUpdate();
            master.currentTick = tick;

            master.markTickFinished();
        }, null));
    }

    public void Update()
    {
        if (planetOverview.usePlanetOverview)
        {
            if (Input.GetKey("d")) planetOverview.rotationalOffset -= 90f * UnityEngine.Time.deltaTime * Mathf.Deg2Rad;
            if (Input.GetKey("a")) planetOverview.rotationalOffset += 90f * UnityEngine.Time.deltaTime * Mathf.Deg2Rad;

            if (Input.mouseScrollDelta.y != 0) {
                general.camera.orthographicSize -= Input.mouseScrollDelta.y * UnityEngine.Time.deltaTime * 500f;
                general.camera.orthographicSize = Math.Max(2, Math.Min(20, general.camera.orthographicSize));
            }

            planetOverview.updateAxes();
        } 
        else if (planetFocus.usePlanetFocus) {
            if (Input.GetMouseButtonDown(0)) planetFocusMousePosition = Input.mousePosition;
            else if (Input.GetMouseButton(0)) {
                Vector3 difference = Input.mousePosition - planetFocusMousePosition;
                planetFocusMousePosition = Input.mousePosition;

                Vector2 adjustedDifference = new Vector2(-difference.y / Screen.height, difference.x / Screen.width);
                adjustedDifference *= 100f;
                
                planetFocus.rotation.x = adjustedDifference.x * planetFocus.zoom / 125f;
                planetFocus.rotation.y = adjustedDifference.y * planetFocus.zoom / 125f;
                planetFocus.rotation.z = 0;
            }

            if (Input.GetMouseButtonDown(1)) planetFocusMousePosition1 = Input.mousePosition;
            if (Input.GetMouseButton(1)) {
                Vector3 difference = Input.mousePosition - planetFocusMousePosition1;
                planetFocusMousePosition1 = Input.mousePosition;

                float adjustedDifference = (difference.x / Screen.width) * 100;
                planetFocus.rotation.x = 0;
                planetFocus.rotation.y = 0;
                planetFocus.rotation.z = adjustedDifference;
            }

            if (Input.mouseScrollDelta.y != 0) {
                // hi!
                // i know you probably have questions about y tf the code below here exists
                // well too bad
                // if u want to fix it go ahead, otherwise its staying here
                if (planetFocus.usePoleFocus) {
                    float change = (float) (0.1 * master.scale) * Mathf.Sign(Input.mouseScrollDelta.y);
                    master.scale -= change;
                    planetFocus.update();
                    master.requestPositionUpdate();
                } else {
                    planetFocus.zoom -= Input.mouseScrollDelta.y * planetFocus.zoom / 10f;
                    planetFocus.zoom = Mathf.Max(Mathf.Min(planetFocus.zoom, 90), 7f);
                }
            }

            float t = UnityEngine.Time.deltaTime;
            float r = planetFocus.zoom / 40f;
            if (Input.GetKey("w")) planetFocus.movementOffset += (float) master.scale * 0.75f * general.camera.transform.up * r * t;
            if (Input.GetKey("s")) planetFocus.movementOffset -= (float) master.scale * 0.75f * general.camera.transform.up * r * t;
            if (Input.GetKey("d")) planetFocus.movementOffset += (float) master.scale * 0.75f * general.camera.transform.right * r * t;
            if (Input.GetKey("a")) planetFocus.movementOffset -= (float) master.scale * 0.75f * general.camera.transform.right * r * t;

            if (Input.GetKeyDown("t") && !general.plt.currentlyDrawing) {
                planetFocus.togglePoleFocus(!planetFocus.usePoleFocus);
                if (planetFocus.usePoleFocus) general.plt.genMinScale();
                else general.plt.clear();
            }

            if (planetFocus.usePoleFocus) {
                if (Input.GetKeyDown("=")) general.plt.increaseScale();
                if (Input.GetKeyDown("-")) general.plt.decreaseScale();

                planetFocus.focus.representation.forceHide = true;
            }

            planetFocus.update();
        } 
        else if (uiMap.useUiMap) {

        }
        else 
        {
            if (Input.GetMouseButton(1) && !EventSystem.current.IsPointerOverGameObject())
            {
                Transform c = general.camera.transform;
                c.Rotate(0, Input.GetAxis("Mouse X") * 2, 0);
                c.Rotate(-Input.GetAxis("Mouse Y") * 2, 0, 0);
                c.localEulerAngles = new Vector3(c.localEulerAngles.x, c.localEulerAngles.y, 0);
            }

            Vector3 forward = general.camera.transform.forward;
            Vector3 right = general.camera.transform.right;
            float t = UnityEngine.Time.deltaTime;
            if (Input.GetKey("w")) master.currentPosition += forward * 5f * (float) master.scale * t;
            if (Input.GetKey("s")) master.currentPosition -= forward * 5f * (float) master.scale * t;
            if (Input.GetKey("d")) master.currentPosition += right * 5f * (float) master.scale * t;
            if (Input.GetKey("a")) master.currentPosition -= right * 5f * (float) master.scale * t;
        }

        if (Input.GetKeyDown("q"))
        {
            master.requestScaleUpdate();
            planetFocus.enable(false);
            uiMap.map.toggle(false);
            planetOverview.enable(!planetOverview.usePlanetOverview);
            master.clearAllLines();

            general.notifyStatusChange();
        }

        if (Input.GetKeyDown("e")) {
            master.requestScaleUpdate();
            uiMap.map.toggle(false);
            planetOverview.enable(false);
            planetFocus.enable(!planetFocus.usePlanetFocus);
            general.pt.unload();
            general.plt.clear();
            master.clearAllLines();

            general.notifyStatusChange();
        }

        if (Input.GetKeyDown("m")) {
            master.requestScaleUpdate();
            planetFocus.enable(false);
            planetOverview.enable(false);
            uiMap.map.toggle(!uiMap.useUiMap);
            master.clearAllLines();

            general.notifyStatusChange();
        }

        if (Input.GetKeyDown("z")) {
            foreach (planet p in master.allPlanets) p.tr.enable(!general.showingTrails);
            if (!general.showingTrails) trailRenderer.drawAllSatelliteTrails(master.allSatellites);
            else foreach (satellite s in master.allSatellites) s.tr.disable();

            general.showingTrails = !general.showingTrails;
            general.notifyTrailsChange();
        }
    }
    private planetTerrain loadTerrain() {
        planetTerrain pt = new planetTerrain(moon, "Materials/planets/moon/moon", 1737.4, 1);

        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MVT/terrain");

        pt.generateFolderInfos(new string[5] {
            Path.Combine(p, "lunaBinary/1"),
            Path.Combine(p, "lunaBinary/2"),
            Path.Combine(p, "lunaBinary/3"),
            Path.Combine(p, "lunaBinary/4"),
            Path.Combine(p, "lunaBinary/5")
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
        return new poleTerrain(new Dictionary<int, string>() {
            {5,  Path.Combine(p, "polesBinary/25m")},
            {10, Path.Combine(p, "polesBinary/50m")},
            {20, Path.Combine(p, "polesBinary/100m")}
        }, moon.representation.gameObject.transform);
    }

    private void Artemis3()
    {
        List<satellite> moonSats =  new List<satellite>();
        List<satellite> earthSats =  new List<satellite>();

        representationData rd = new representationData(
            "Prefabs/Planet",
            "Materials/default");

        representationData srd = new representationData(
            "Prefabs/Satellite",
            "Materials/default");

        representationData frd = new representationData(
            "Prefabs/Facility",
            "Materials/default");

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666667;
        double oneSec = 0.00001157;

        double MoonMu = 4902.800066;

        earth = new planet(  "Earth", new planetData(  6371, rotationType.earth,   "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/earth/earthEquirectangular"));
        moon =  new planet(   "Luna", new planetData(1738.1,  rotationType.moon,    "CSVS/ARTEMIS 3/PLANETS/moon",  oneMin,   planetType.moon), new representationData("Prefabs/Planet", "Materials/planets/moon/moon"));
                new planet("Mercury", new planetData(2439.7,  rotationType.none, "CSVS/ARTEMIS 3/PLANETS/mercury", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/mercury"));
                new planet(  "Venus", new planetData(6051.8,  rotationType.none,   "CSVS/ARTEMIS 3/PLANETS/venus", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/venus"));
                new planet(   "Mars", new planetData(3396.2,  rotationType.none,    "CSVS/ARTEMIS 3/PLANETS/mars", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/mars"));
                new planet("Jupiter", new planetData( 71492,  rotationType.none, "CSVS/ARTEMIS 3/PLANETS/jupiter", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/jupiter"));
                new planet( "Saturn", new planetData( 60268,  rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/saturn", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/saturn"));
                new planet( "Uranus", new planetData( 25559,  rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/uranus", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/uranus"));
                new planet("Neptune", new planetData( 24764,  rotationType.none, "CSVS/ARTEMIS 3/PLANETS/neptune", oneHour, planetType.planet), new representationData("Prefabs/Planet", "Materials/planets/neptune"));


        var data = DBReader.getData();
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

                satellite sat = null;
                if (dict.ContainsKey("RAAN")) {
                    sat = new satellite(x.Key, new satelliteData(new Timeline(dict["SemimajorAxis"] / 1000, dict["Eccentricity"], dict["Inclination"], dict["Arg_of_Perigee"], dict["RAAN"], dict["MeanAnomaly"], 1, Time.strDateToJulian(dict["OrbitEpoch"]), MoonMu)), srd);
                } else if (dict.ContainsKey("FilePath")) {
                    if (x.Key == "HLS-Ascent" | x.Key == "HLS-Descent")
                    {
                        sat = new satellite(x.Key, new satelliteData($"CSVS/ARTEMIS 3/SATS/{x.Key}", oneSec), srd);
                    }
                    else
                    {
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
                    Debug.Log("worked: " + x.Key);
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

                    List<antennaData> antenna = new List<antennaData>() {new antennaData(x.Key, x.Key, new geographic(dict["Lat"], dict["Long"]), dict["Schedule_Priority"], dict["Service_Level"], dict["Service_Period"])};
                    facility fd = new facility(x.Key, moon, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), 0, antenna, new Time(2460806.5 + start), new Time(2460806.5 + stop)), frd);
                    Debug.Log("Facility created");

                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                } else {
                    List<antennaData> antenna = new List<antennaData>() {new antennaData(x.Key, x.Key, new geographic(dict["Lat"], dict["Long"]), dict["Ground_Priority"])};
                    facility fd = new facility(x.Key, earth, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), 0, antenna), frd);

                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                    Debug.Log("Facility created");
                }
            }
        }

        //List<antennaData> antenna1 = new List<antennaData>() {new antennaData("(0,0)", "(0, 0)", new geographic(0, 0), 1)};
        //facility fd1 = new facility("(0, 0)", moon, new facilityData("(0, 0)", new geographic(0, 0), 0, antenna1), frd);

        //List<antennaData> antenna2 = new List<antennaData>() {new antennaData("(90, 0)", "(90, 0)", new geographic(90, 0), 1)};
        //facility fd2 = new facility("(90, 0)", moon, new facilityData("(90, 0)", new geographic(90, 0), 0, antenna2), frd);

        //List<antennaData> antenna3 = new List<antennaData>() {new antennaData("(-90, 0)", "(-90, 0)", new geographic(-90, 0), 1)};
        //facility fd3 = new facility("(-90, 0)", moon, new facilityData("(-90, 0)", new geographic(-90, 0), 0, antenna3), frd);

        //List<antennaData> antenna4 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, 90), 1)};
        //facility fd4 = new facility("(0, 90)", moon, new facilityData("(0, 90)", new geographic(0, 90), 0, antenna4), frd);

        //List<antennaData> antenna7 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, 60), 1)};
        //facility fd7 = new facility("(0, 60)", moon, new facilityData("(0, 90)", new geographic(0, 60), 0, antenna7), frd);

        //List<antennaData> antenna8 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, 30), 1)};
        //facility fd8 = new facility("(0, 30)", moon, new facilityData("(0, 90)", new geographic(0, 30), 0, antenna8), frd);

        //List<antennaData> antenna5 = new List<antennaData>() {new antennaData("(0, 180)", "(0, 180)", new geographic(0, 180), 1)};
        //facility fd5 = new facility("(0, 180)", moon, new facilityData("(0, 180)", new geographic(0, 180), 0, antenna5), frd);

        //List<antennaData> antenna6 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, -90), 1)};
        //facility fd6 = new facility("(0, -90)", moon, new facilityData("(0, 90)", new geographic(0, -90), 0, antenna6), frd);

        // List<antennaData> antenna9 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(60, 0), 1)};
        //facility fd9 = new facility("(60, 0)", moon, new facilityData("(0, 90)", new geographic(60, 0), 0, antenna9), frd);

        //List<antennaData> antenna10 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(30, 0), 1)};
        //facility fd10 = new facility("(30, 0)", moon, new facilityData("(0, 90)", new geographic(30, 0), 0, antenna10), frd);

        /*List<antennaData> antenna4 = new List<antennaData>() {new antennaData("(270, 0)", "(270, 0)", new geographic(270, 0), 1)};
        facility fd4 = new facility("(270, 0)", moon, new facilityData("(270, 0)", new geographic(270, 0), antenna4), frd);*/

        planet.addFamilyNode(earth, moon);

        master.setReferenceFrame(moon);
        master.relationshipPlanet[earth] = new List<planet>() {moon};
        master.relationshipSatellite[moon] = moonSats;
        master.relationshipSatellite[earth] = earthSats;

        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/PLANETS/moon", oneMin));
        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/SATS/v", 0.0006944444));

        //windows.jsonWindows();

    }
}
