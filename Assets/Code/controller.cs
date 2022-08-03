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
                "Materials/default"));

        Artemis3();
        defaultReferenceFrame = moon;
        //onlyEarth();

        general.pt = loadTerrain();
        general.plt = loadPoles();

        //runScheduling();
        //csvParser.loadScheduling("CSVS/SCHEDULING/July 2021 NSN DTE Schedule");

        master.pause = false;
        general.camera = Camera.main;

        master.markStartOfSimulation();

        //runDynamicLink();
        //linkBudgeting.accessCalls("C:/Users/akazemni/Desktop/connections.txt");

        startMainLoop();

        //Debug.Log(position.J2000(new position(0, 1, 0), new position(0, 0, -1), new position(0, 1, 0)));
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

        StartCoroutine(visibility.raycastTerrain(
            new List<object>() {master.allSatellites.Find(x => x.name == "LCN-1"), master.allSatellites.Find(x => x.name == "LCN-2"), master.allSatellites.Find(x => x.name == "LCN-3"), master.allFacilites.Find(x => x.name == "Mare Crisium"), master.allFacilites.Find(x => x.name == "South Pole")}, 
            new List<object>() {master.allSatellites.Find(x => x.name == "CubeSat-1")}, master.time.julian, master.time.julian + 1, speed, options));
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
                
                planetFocus.rotation.x = adjustedDifference.x * ((6371f / (float) planetFocus.focus.radius) *  planetFocus.zoom / (general.defaultCameraFOV * 1.5f));
                planetFocus.rotation.y = adjustedDifference.y * ((6371f / (float) planetFocus.focus.radius) *  planetFocus.zoom / (general.defaultCameraFOV * 1.5f));
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
                    planetFocus.zoom = Mathf.Max(Mathf.Min(planetFocus.zoom, 75), 4f);
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
            planetOverview.enable(!planetOverview.usePlanetOverview);
            master.clearAllLines();

            general.notifyStatusChange();
        }

        if (Input.GetKeyDown("e")) {
            master.requestScaleUpdate();
            planetFocus.enable(!planetFocus.usePlanetFocus);
            general.pt.unload();
            general.plt.clear();
            master.clearAllLines();

            general.notifyStatusChange();
        }

        if (Input.GetKeyDown("m")) {
            master.requestScaleUpdate();
            uiMap.map.toggle(!uiMap.useUiMap);
            master.clearAllLines();

            general.notifyStatusChange();
        }

        if (Input.GetKeyDown("z")) {
            foreach (planet p in master.allPlanets) p.tr.enable(!general.showingTrails);
            foreach (satellite s in master.allSatellites) s.tr.enable(!general.showingTrails);

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

        representationData erd = new representationData(
            "Prefabs/Planet",
            "Materials/planets/earth/earthEquirectangular");
        
        representationData mrd = new representationData(
            "Prefabs/Planet",
            "Materials/planets/moon/moon");

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666667;

        double MoonMu = 4902.800066;

        earth = new planet(  "Earth", new planetData(  6371, rotationType.earth, "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), erd);
        moon =  new planet(   "Luna", new planetData(1738.1, rotationType.moon,    "CSVS/ARTEMIS 3/PLANETS/moon", oneMin, planetType.moon),   mrd);
                new planet("Mercury", new planetData(2439.7, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/mercury", oneHour, planetType.planet), rd);
                new planet(  "Venus", new planetData(6051.8, rotationType.none,   "CSVS/ARTEMIS 3/PLANETS/venus", oneHour, planetType.planet), rd);
                new planet(   "Mars", new planetData(3396.2, rotationType.none,    "CSVS/ARTEMIS 3/PLANETS/mars", oneHour, planetType.planet), rd);
                new planet("Jupiter", new planetData( 71492, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/jupiter", oneHour, planetType.planet), rd);
                new planet( "Saturn", new planetData( 60268, rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/saturn", oneHour, planetType.planet), rd);
                new planet( "Uranus", new planetData( 25559, rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/uranus", oneHour, planetType.planet), rd);
                new planet("Neptune", new planetData( 24764, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/neptune", oneHour, planetType.planet), rd);

        var data = DBReader.getData();
        foreach (KeyValuePair<string, dynamic> x in data["Artemis_III"].satellites) {
            var dict = data["Artemis_III"].satellites[x.Key];

            if (dict["Type"] == "Satellite") {
                if (dict["user_provider"] == "user/provider" || dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (false, 2460806.5 + dict["TimeInterval_start"], 2460806.5 + dict["TimeInterval_stop"]));
                if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (false, 2460806.5 + dict["TimeInterval_start"], 2460806.5 + dict["TimeInterval_stop"]));

                satellite sat = null;
                if (dict.ContainsKey("RAAN")) {
                    sat = new satellite(x.Key, new satelliteData(new Timeline(dict["SemimajorAxis"] / 1000, dict["Eccentricity"], dict["Inclination"], dict["Arg_of_Perigee"], dict["RAAN"], dict["MeanAnomaly"], 1, Time.strDateToJulian(dict["OrbitEpoch"]), MoonMu)), srd);
                } else if (dict.ContainsKey("FilePath")) {
                    sat = new satellite(x.Key, new satelliteData($"CSVS/ARTEMIS 3/SATS/{x.Key}", oneMin), srd);
                }
                sat.positions.enableExistanceTime(new Time(2460806.5 + dict["TimeInterval_start"]), new Time((2460806.5 + dict["TimeInterval_stop"])));

                if (dict["CentralBody"] == "Moon") {
                    satellite.addFamilyNode(moon, sat);
                    moonSats.Add(sat);
                } else if (dict["CentralBody"] == "Earth") {
                    satellite.addFamilyNode(earth, sat);
                    earthSats.Add(sat);
                }
            } else if (dict["Type"] == "Facility") {
                if (dict["CentralBody"] == "Moon")
                {
                    double start = 0, stop = 0;
                    if (dict["TimeInterval_start"] is string) start = Double.Parse(dict["TimeInterval_start"], System.Globalization.NumberStyles.Any);
                    else start = (double) dict["TimeInterval_start"];

                    if (dict["TimeInterval_stop"] is string) stop = Double.Parse(dict["TimeInterval_stop"], System.Globalization.NumberStyles.Any);
                    else stop = (double) dict["TimeInterval_stop"];

                    List<antennaData> antenna = new List<antennaData>() {new antennaData(x.Key, x.Key, new geographic(dict["Lat"], dict["Long"]), dict["Schedule_Priority"], dict["Service_Level"], dict["Service_Period"])};
                    facility fd = new facility(x.Key, moon, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), 0, antenna, new Time(2460806.5 + start), new Time(2460806.5 + stop)), frd);

                    //facility fd = new facility(x.Key, moon, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), antenna), frd);


                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                } /*else {
                    List<antennaData> antenna = new List<antennaData>() {new antennaData(x.Key, x.Key, new geographic(dict["Lat"], dict["Long"]), dict["Ground_Priority"])};
                    facility fd = new facility(x.Key, earth, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), antenna), frd);

                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                }*/
            }
        }

        satellite sat1 = new satellite("cubeSat", new satelliteData(new Timeline(1837.1,0, 90, 0, 0, 0, 1, 2460806.5, MoonMu)), srd);
        satellite.addFamilyNode(moon, sat1);
        sat1.positions.enableExistanceTime(new Time(2460806.5), new Time((2460836.5)));
        moonSats.Add(sat1);

        List<antennaData> antenna1 = new List<antennaData>() {new antennaData("(0,0)", "(0, 0)", new geographic(0, 0), 1)};
        facility fd1 = new facility("(0, 0)", moon, new facilityData("(0, 0)", new geographic(0, 0), 0, antenna1), frd);

        List<antennaData> antenna2 = new List<antennaData>() {new antennaData("(90, 0)", "(90, 0)", new geographic(90, 0), 1)};
        facility fd2 = new facility("(90, 0)", moon, new facilityData("(90, 0)", new geographic(90, 0), 0, antenna2), frd);

        List<antennaData> antenna3 = new List<antennaData>() {new antennaData("(-90, 0)", "(-90, 0)", new geographic(-90, 0), 1)};
        facility fd3 = new facility("(-90, 0)", moon, new facilityData("(-90, 0)", new geographic(-90, 0), 0, antenna3), frd);

        List<antennaData> antenna4 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, 90), 1)};
        facility fd4 = new facility("(0, 90)", moon, new facilityData("(0, 90)", new geographic(0, 90), 0, antenna4), frd);

        List<antennaData> antenna7 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, 60), 1)};
        facility fd7 = new facility("(0, 60)", moon, new facilityData("(0, 90)", new geographic(0, 60), 0, antenna7), frd);

        List<antennaData> antenna8 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, 30), 1)};
        facility fd8 = new facility("(0, 30)", moon, new facilityData("(0, 90)", new geographic(0, 30), 0, antenna8), frd);

        List<antennaData> antenna5 = new List<antennaData>() {new antennaData("(0, 180)", "(0, 180)", new geographic(0, 180), 1)};
        facility fd5 = new facility("(0, 180)", moon, new facilityData("(0, 180)", new geographic(0, 180), 0, antenna5), frd);

        List<antennaData> antenna6 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(0, -90), 1)};
        facility fd6 = new facility("(0, -90)", moon, new facilityData("(0, 90)", new geographic(0, -90), 0, antenna6), frd);

         List<antennaData> antenna9 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(60, 0), 1)};
        facility fd9 = new facility("(60, 0)", moon, new facilityData("(0, 90)", new geographic(60, 0), 0, antenna9), frd);

        List<antennaData> antenna10 = new List<antennaData>() {new antennaData("(0, 90)", "(0, 90)", new geographic(30, 0), 1)};
        facility fd10 = new facility("(30, 0)", moon, new facilityData("(0, 90)", new geographic(30, 0), 0, antenna10), frd);

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
    
    private void Artemis3ButAgain() {
        representationData rd = new representationData(
            "Prefabs/Planet",
            "Materials/default");

        representationData srd = new representationData(
            "Prefabs/Satellite",
            "Materials/default");

        representationData frd = new representationData(
            "Prefabs/Facility",
            "Materials/default");

        representationData erd = new representationData(
            "Prefabs/Planet",
            "Materials/planets/earth/earthEquirectangular");

        representationData mrd = new representationData(
            "Prefabs/Planet",
            "Materials/planets/moon/moon");

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666665;

        earth = new planet("Earth", new planetData(6371, rotationType.earth, "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), erd);
        moon = new planet("Luna", new planetData(1738.1, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/moon", oneHour, planetType.moon), mrd);
        new planet("Mercury", new planetData(2439.7, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/mercury", oneHour, planetType.planet), rd);
        new planet("Venus", new planetData(6051.8, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/venus", oneHour, planetType.planet), rd);
        new planet("Mars", new planetData(3396.2, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/mars", oneHour, planetType.planet), rd);
        new planet("Jupiter", new planetData(71492, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/jupiter", oneHour, planetType.planet), rd);
        new planet("Saturn", new planetData(60268, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/saturn", oneHour, planetType.planet), rd);
        new planet("Uranus", new planetData(25559, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/uranus", oneHour, planetType.planet), rd);
        new planet("Neptune", new planetData(24764, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/neptune", oneHour, planetType.planet), rd);

        satellite s1 = new satellite("LCN-1", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 0, 1, 2460628.5283449073, 4902.800066)), srd);
        satellite s2 = new satellite("LCN-2", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 180, 1, 2460628.5283449073, 4902.800066)), srd);
        satellite s3 = new satellite("LCN-3", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 360, 1, 2460628.5283449073, 4902.800066)), srd);

        //satellite s4 = new satellite("Moonlight-1", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 0, 1, 2460628.5283449073, 4902.800066)), srd);
        //satellite s5 = new satellite("Moonlight-2", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 180, 1, 2460628.5283449073, 4902.800066)), srd);

        satellite s6 = new satellite("CubeSat-1", new satelliteData(new Timeline(5000, 0.51, 74.3589, 90, 356.858, 311.274, 1, 2460615.5, 4902.800066)), srd);
        satellite s7 = new satellite("CubeSat-2", new satelliteData(new Timeline(1837.4, 0.000000000000000195, 114.359, 0, 356.858, 360, 1, 2460615.5, 4902.800066)), srd);

        satellite s8 = new satellite("HLS-NRHO", new satelliteData("CSVS/ARTEMIS 3/SATS/HLS/HLS-NRHO", oneMin), srd);
        satellite s9 = new satellite("HLS-Docked", new satelliteData("CSVS/ARTEMIS 3/SATS/HLS/HLS-Docked", oneMin), srd);
        satellite s10 = new satellite("HLS-Disposal", new satelliteData("CSVS/ARTEMIS 3/SATS/HLS/HLS-Disposal", oneMin), srd);

        satellite s11 = new satellite("Orion-Transit-O", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-Transit-O", oneMin), srd);
        satellite s12 = new satellite("Orion-Docked", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-Docked", oneMin), srd);
        satellite s13 = new satellite("Orion-NRHO", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-NRHO", oneMin), srd);
        satellite s14 = new satellite("Orion-Transit-R", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-Transit-R", oneMin), srd);

        s8.positions.enableExistanceTime(new Time(2460806.5), new Time((2460806.5 + 9.0)));
        s9.positions.enableExistanceTime(new Time((2460806.5 + 9.0)), new Time((2460806.5 + 13.0)));
        s10.positions.enableExistanceTime(new Time((2460806.5 + 13.0)), new Time((2460806.5 + 20.29504301)));

        s11.positions.enableExistanceTime(new Time(2460806.5), new Time((2460806.5 + 9.0)));
        s12.positions.enableExistanceTime(new Time((2460806.5 + 9.0)), new Time((2460806.5 + 13.0)));
        s13.positions.enableExistanceTime(new Time((2460806.5 + 13.0)), new Time((2460806.5 + 20.29504301)));
        s14.positions.enableExistanceTime(new Time((2460806.5 + 20.29504301)), new Time((2460806.5 + 30.0)));

        satellite.addFamilyNode(moon, s1);
        satellite.addFamilyNode(moon, s2);
        satellite.addFamilyNode(moon, s3);

        //satellite.addFamilyNode(moon, s4);
        //satellite.addFamilyNode(moon, s5);

        satellite.addFamilyNode(moon, s6);
        satellite.addFamilyNode(moon, s7);

        satellite.addFamilyNode(moon, s8);
        satellite.addFamilyNode(moon, s9);
        satellite.addFamilyNode(moon, s10);

        satellite.addFamilyNode(moon, s11);
        satellite.addFamilyNode(moon, s12);
        satellite.addFamilyNode(moon, s13);
        satellite.addFamilyNode(moon, s14);

        facility schickard = new facility("Schickard", moon, new facilityData("Schickard", new geographic(-44.4, -55.1), 0, new List<antennaData>()), frd);
        //facility longomontanus = new facility("Longomontanus", moon, new facilityData("Longomontanus", new geographic(-49.5, -21.7), 0, new List<antennaData>()), frd);
        facility maginus = new facility("Maginus", moon, new facilityData("Maginus", new geographic(-50, -6.2), 0, new List<antennaData>()), frd);
        facility apollo = new facility("Apollo", moon, new facilityData("Apollo", new geographic(-36.1, -151.8), 0, new List<antennaData>()), frd);
        facility mare_crisium = new facility("Mare Crisium", moon, new facilityData("Mare Crisium", new geographic(17, 59.1), 0, new List<antennaData>()), frd);

        new facility("South Pole", moon, new facilityData("South Pole", new geographic(-90, 0), 0, new List<antennaData>()), frd);

        master.relationshipPlanet.Add(earth, new List<planet>() { moon });
        master.relationshipSatellite.Add(moon, new List<satellite>() { s1, s2, s3, /*s4, s5,*/ s6, s7, s8, s9, s10, s11, s12, s13, s14});
        master.relationshipFacility.Add(moon, new List<facility>() {schickard, maginus, apollo, mare_crisium});

        /*facility f1 = new facility("HLS-Surface", moon, new facilityData("HLS-Surface", new geographic(-89.45, -137.31), null, new Time((2460806.5 + 13.0)), new Time((2460806.5 + 20.0))), frd);
        facility f2 = new facility("CLPS9", moon, new facilityData("CLPS9", new geographic(-75.0, 113), new Time(2460806.5), new Time((2460806.5 + 30.0))), frd);

        facility f3 = new facility("DSS-14", earth, new facilityData("DSS-14", new geographic(35.4295, -116.889), null), frd);
        facility f4 = new facility("DSS-23", earth, new facilityData("DSS-23", new geographic(35.3399, -116.87), null), frd);
        facility f5 = new facility("DSS-24", earth, new facilityData("DSS-24", new geographic(35.3399, -116.875), null), frd);
        facility f6 = new facility("DSS-25", earth, new facilityData("DSS-25", new geographic(35.3376, -116.875), null), frd);
        facility f7 = new facility("DSS-26", earth, new facilityData("DSS-26", new geographic(35.3357, -116.873), null), frd);
        facility f8 = new facility("DSS-33", earth, new facilityData("DSS-33", new geographic(-35.3985, 148.982), null), frd);
        facility f9 = new facility("DSS-35", earth, new facilityData("DSS-34", new geographic(-35.3985, 148.982), null), frd);
        facility f10 = new facility("DSS-36", earth, new facilityData("DSS-36", new geographic(-35.3951, 148.979), null), frd);*/

        master.setReferenceFrame(moon);
    }
}
