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
    public float playerSpeed = 100f * (float) master.scale;
    public static planet earth;
    private double speed = 0.0006944444;
    private Vector3 planetFocusMousePosition, planetFocusMousePosition1;
    private Coroutine loop;


    void Start()
    {
        if (sceneController.alreadyStarted) return;
        sceneController.alreadyStarted = true;

        SceneManager.sceneLoaded += sceneController.prepareScene;

        master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/sun", 0.0416666665, planetType.planet),
            new representationData(
                "Prefabs/Planet",
                "Materials/default"));

        //jsonParser.deserialize(Path.Combine(Application.streamingAssetsPath, "sytEarth.syt"), jsonType.system);

        //jsonParser.deserialize(Path.Combine(Application.streamingAssetsPath, "sytAll.syt"), jsonType.system);
        //master.setReferenceFrame(master.allPlanets.First(x => x.name == "Earth"));

        //onlyEarth();
        //kepler();
        Artemis3();
        //starlink();


        //csvParser.loadScheduling("CSVS/SCHEDULING/July 2021 NSN DTE Schedule");
        DBReader.getData();

        //planetTerrain pt = loadTerrain();

        master.pause = false;
        general.camera = Camera.main;

        master.markStartOfSimulation();

        linkBudgeting.accessCalls("C:/Users/akazemni/Desktop/connections.txt");

        startMainLoop();

        //Debug.Log(position.J2000(new position(0, 1, 0), new position(0, 0, -1), new position(0, 1, 0)));
    }

    public void startMainLoop(bool force = false) {
        if (loop != null && force == false) return;

        loop = StartCoroutine(general.internalClock(3600, int.MaxValue, (tick) => {
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

            //pt.updateTerrain();

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
        } else if (planetFocus.usePlanetFocus) {
            if (Input.GetMouseButtonDown(0)) planetFocusMousePosition = Input.mousePosition;
            else if (Input.GetMouseButton(0)) {
                Vector3 difference = Input.mousePosition - planetFocusMousePosition;
                planetFocusMousePosition = Input.mousePosition;

                Vector2 adjustedDifference = new Vector2(-difference.y / Screen.height, difference.x / Screen.width);
                adjustedDifference *= 100f;

                planetFocus.rotation.x = adjustedDifference.x * (planetFocus.zoom / (general.defaultCameraFOV * 1.5f));
                planetFocus.rotation.y = adjustedDifference.y * (planetFocus.zoom / (general.defaultCameraFOV * 1.5f));
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
                planetFocus.zoom -= Input.mouseScrollDelta.y * UnityEngine.Time.deltaTime * 500f;
            }

            planetFocus.update();
        }
        else {
            if (Input.GetMouseButton(1) && !EventSystem.current.IsPointerOverGameObject())
            {
                Transform c = Camera.main.transform;
                c.Rotate(0, Input.GetAxis("Mouse X") * 2, 0);
                c.Rotate(-Input.GetAxis("Mouse Y") * 2, 0, 0);
                c.localEulerAngles = new Vector3(c.localEulerAngles.x, c.localEulerAngles.y, 0);
            }

            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            float t = UnityEngine.Time.deltaTime;
            if (Input.GetKey("w")) master.currentPosition += forward * playerSpeed * t;
            if (Input.GetKey("s")) master.currentPosition -= forward * playerSpeed * t;
            if (Input.GetKey("d")) master.currentPosition += right * playerSpeed * t;
            if (Input.GetKey("a")) master.currentPosition -= right * playerSpeed * t;

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
            master.clearAllLines();

            general.notifyStatusChange();
        }

        if (Input.GetKeyDown("1")) speed = 0.1;
        if (Input.GetKeyDown("2")) speed = 0.01;
        if (Input.GetKeyDown("3")) speed = 0.00005;

        if (Input.GetKeyDown("4")) master.time.addJulianTime(2460806.5 - master.time.julian);

        if (Input.GetKeyDown("z")) {
            foreach (planet p in master.allPlanets) p.tr.toggle();
            foreach (satellite s in master.allSatellites) s.tr.toggle();
        }
    }
    private planetTerrain loadTerrain() {
        /*terrainProcessor.divideAll("C:/Users/leozw/Desktop/GEBCO_30_Dec_2021_7c5d3c80c8ee/", new List<terrainResolution>() {
            //new terrainResolution("C:/Users/leozw/Desktop/divided/ultra", 100, 6),
            //new terrainResolution("C:/Users/leozw/Desktop/divided/extreme", 64, 9),
            //new terrainResolution("C:/Users/leozw/Desktop/divided/high", 9, 20), // if this doesnt work, regen it
            //new terrainResolution("C:/Users/leozw/Desktop/divided/medium", 4, 30),
            //new terrainResolution("C:/Users/leozw/Desktop/divided/low", 1, 60),
            //new terrainResolution("C:/Users/leozw/Desktop/divided/tiny", 4, 100),
            },
            100, "C:/Users/leozw/Desktop/divided/earthNormal.jpg");*/

        //List<nearbyFacilites> nfs = highResTerrain.neededAreas();
        //foreach (nearbyFacilites nf in nfs) Debug.Log(nf);

        planetTerrain pt = new planetTerrain(6371, 35, earth);
        pt.generateFolderInfos(new string[6] {
            "C:/Users/leozw/Desktop/divided/ultra",
            "C:/Users/leozw/Desktop/divided/extreme",
            "C:/Users/leozw/Desktop/divided/high",
            "C:/Users/leozw/Desktop/divided/medium",
            "C:/Users/leozw/Desktop/divided/low",
            "C:/Users/leozw/Desktop/divided/tiny"});

        //dtedImageCombiner.parseSentinelKML("C:/Users/leozw/Desktop/S2A_OPER_GIP_TILPAR_MPC__20151209T095117_V20150622T000000_21000101T000000_B00.kml", "C:/Users/leozw/Desktop/Sentinel2Tiles.csv");
        //sentinelArea.tileKey = csvParser.loadSentinelTiles("C:/Users/leozw/Desktop/dteds/Sentinel2Tiles.csv");
        //dtedImageCombiner.generateImage(new geographic(34.8376, -117.3898), new geographic(35.9259, -116.3749), "C:/Users/leozw/Desktop/dteds/toProcess", "C:/Users/leozw/Desktop/dteds/Goldstone", "Goldstone");

        //dtedInfo di1 = dtedReader.readDted("C:/Users/leozw/Desktop/dteds/Goldstone/a.dt2", "C:/Users/leozw/Desktop/dteds/Goldstone/Goldstone.txt", true);
        //dtedInfo di2 = dtedReader.readDted("C:/Users/leozw/Desktop/dteds/Goldstone/b.dt2", "C:/Users/leozw/Desktop/dteds/Goldstone/Goldstone.txt", true);
        //dtedInfo di3 = dtedReader.readDted("C:/Users/leozw/Desktop/dteds/Goldstone/c.dt2", "C:/Users/leozw/Desktop/dteds/Goldstone/Goldstone.txt", true);
        //dtedInfo di4 = dtedReader.readDted("C:/Users/leozw/Desktop/dteds/Goldstone/d.dt2", "C:/Users/leozw/Desktop/dteds/Goldstone/Goldstone.txt", true);

        //dtedReader.toFile(new List<dtedInfo>() {di1, di2, di3, di4}, new geographic(34.8376, -117.3898), new geographic(35.9259, -116.3749), "C:/Users/leozw/Desktop/dteds/goldstone/goldstone.hrt");

        return pt;
    }
    private void onlyEarth()
    {
        List<string> sats = new List<string>()
        {
            "AIM",
            "AQUA",
            "AURA",
            "FGST",
            "Geotail",
            "GOES 1",
            "GOES 2",
            "GOES 3",
            "GOES 4",
            "GOES 5",
            "GOES 6",
            "GOES 7",
            "GOES 8",
            "GOES 9",
            "GOES 10",
            "GOES 11",
            "GOES 12",
            "GOES 13",
            "GOES 14",
            "GOES 15",
            "GOES 16",
            "GOES 17",
            "GPM_CORE",
            "GRACE FO1",
            "GRACE FO2",
            "HST",
            "ICESAT 2",
            "ICON",
            "IRIS",
            "ISS",
            "LANDSAT 7",
            "LANDSAT 8",
            "METOP B",
            "METOP C",
            "MMS 1",
            "MMS 2",
            "MMS 3",
            "MMS 4",
            "NUSTAR",
            "OCO-2",
            "SCISAT 1",
            "SDO",
            "SEAHAWK 1",
            "SMAP",
            "SOLAR B",
            "STPSat 3",
            "STPSat 4",
            "STPSat 5",
            "SWIFT",
            "TDRS 3",
            "TDRS 5",
            "TDRS 6",
            "TDRS 7",
            "TDRS 8",
            "TDRS 9",
            "TDRS 10",
            "TDRS 11",
            "TDRS 12",
            "TDRS 13",
            "TERRA",
            "THEMIS_A",
            "THEMIS_D",
            "THEMIS_E"
        };

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

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666665;

        earth =       new planet(  "Earth", new planetData(  6371, rotationType.earth , "CSVS/OLD/PLANETS/Earth", oneHour, planetType.planet), erd);
        planet moon = new planet(   "Luna", new planetData(1738.1, rotationType.moon,    "CSVS/OLD/PLANETS/Luna", oneHour, planetType.moon),   rd);
                      new planet("Mercury", new planetData(2439.7, rotationType.none, "CSVS/OLD/PLANETS/Mercury", oneHour, planetType.planet), rd);
                      new planet(  "Venus", new planetData(6051.8, rotationType.none,   "CSVS/OLD/PLANETS/Venus", oneHour, planetType.planet), rd);
                      new planet(   "Mars", new planetData(3396.2, rotationType.none,    "CSVS/OLD/PLANETS/Mars", oneHour, planetType.planet), rd);
                      new planet("Jupiter", new planetData( 71492, rotationType.none, "CSVS/OLD/PLANETS/Jupiter", oneHour, planetType.planet), rd);
                      new planet( "Saturn", new planetData( 60268, rotationType.none,  "CSVS/OLD/PLANETS/Saturn", oneHour, planetType.planet), rd);
                      new planet( "Uranus", new planetData( 25559, rotationType.none,  "CSVS/OLD/PLANETS/Uranus", oneHour, planetType.planet), rd);
                      new planet("Neptune", new planetData( 24764, rotationType.none, "CSVS/OLD/PLANETS/Neptune", oneHour, planetType.planet), rd);

        master.relationshipPlanet.Add(earth, new List<planet>() {moon});
        master.relationshipSatellite.Add(earth, new List<satellite>() {});
        master.relationshipSatellite.Add(moon, new List<satellite>() {});

        foreach (string sat in sats) {
            satellite s = new satellite(sat, new satelliteData($"CSVS/OLD/SATS/{sat}", oneMin), srd);
            master.relationshipSatellite[earth].Add(s);
            master.relationshipSatellite[moon].Add(s);
        }

        foreach (facilityData fd in csvParser.loadFacilites("CSVS/OLD/FACILITIES/stationList")) new facility(fd.name, earth, fd, frd);

        master.setReferenceFrame(earth);
    }
    private void kepler()
    {
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

        double oneHour = 0.0416666665;

        earth = new planet("Earth", new planetData(  6371,  rotationType.earth, "CSVS/MOONBASED/earth", oneHour, planetType.planet), erd);
        planet moon =        new planet( "Luna", new planetData(1738.1, rotationType.moon,  "CSVS/MOONBASED/moon", oneHour,   planetType.moon),  rd);

        foreach (facilityData fd in csvParser.loadFacilites("CSVS/FACILITIES/stationList")) {
            new facility(fd.name, earth, new facilityData(fd.name, fd.geo, fd.antennas, new Time(2460857.5), new Time(2460859.5)), frd);
        }

        master.setReferenceFrame(moon);

        /*foreach (KeyValuePair<string, Timeline> kvp in csvParser.loadRpt(Path.Combine(Application.streamingAssetsPath, "CM_ORB.rpt")))
        {
            satellite s = new satellite(kvp.Key, new satelliteData(kvp.Value), srd);
            if (kvp.Key == "LRO") satellite.addFamilyNode(moon, s);
            else satellite.addFamilyNode(earth, s);
        }*/
        //0.0012803056323726458 42166.394716755305 0.07603377880226884 5.8448456857898 1.9281280538370242 86170.91187060841 3.693310484029208 3.691970164460813

        //satellite s1 = new satellite("GATEWAY", new satelliteData("CSVS/MOONBASED/Gateway", 0.0006944444), srd);
        satellite s2 = new satellite("LCN", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 0, 1, 2460628.5283449073, 4902.800066, new Time(2460857.5), new Time(2460859.5))), srd);
        //satellite s3 = new satellite("Aqua 2", new satelliteData("CSVS/EARTHBASED/AQUA", 0.0006944444), srd);

        //satellite s2 = new satellite("TDRS-KEPLER", new satelliteData(new Timeline())")
        /*satellite s2 = new satellite("Oco-2 2", new satelliteData("CSVS/EARTHBASED/OCO-2", 0.0006944444), srd);
        satellite s3 = new satellite("Aqua 2", new satelliteData("CSVS/EARTHBASED/AQUA", 0.0006944444), srd);
        satellite s4 = new satellite("Aim 2", new satelliteData("CSVS/EARTHBASED/AIM", 0.0006944444), srd);
        satellite s5 = new satellite("LRO 2", new satelliteData("CSVS/EARTHBASED/LRO", 0.0006944444 * 5.0), srd);

        new satellite("M1", new satelliteData($"CSVS/SATS/MMS 1", 0.0006944444), srd);
        new satellite("M2", new satelliteData($"CSVS/SATS/MMS 2", 0.0006944444), srd);
        new satellite("M3", new satelliteData($"CSVS/SATS/MMS 3", 0.0006944444), srd);
        new satellite("M4", new satelliteData($"CSVS/SATS/MMS 4", 0.0006944444), srd);*/

        //satellite.addFamilyNode(earth, s1);
        satellite.addFamilyNode(moon, s2);
        /*satellite.addFamilyNode(earth, s3);
        satellite.addFamilyNode(earth, s4);
        satellite.addFamilyNode(moon, s5);*/
    }
    private void Artemis3()
    {
        List<satellite> moonSats =  new List<satellite>();
        List<satellite> earthSats =  new List<satellite>();

      representationData rd = new representationData(
          "Prefabs/Planet",
          "Materials/default");

      representationData lrd = new representationData(
          "Prefabs/Planet",
          "Materials/default",
          new Vector3(0f,0f,0f));

      representationData srd = new representationData(
          "Prefabs/Satellite",
          "Materials/default");

      representationData frd = new representationData(
          "Prefabs/Facility",
          "Materials/default");

      representationData erd = new representationData(
          "Prefabs/Planet",
          "Materials/planets/earth/earthEquirectangular");

      double oneMin = 0.0006944444;
      double oneHour = 0.0416666667;

      double MoonMu = 4902.800066;
      double EarthMu = 398600.435436;

      earth =       new planet(  "Earth", new planetData(  6371, rotationType.earth, "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), erd);
      planet moon = new planet(   "Luna", new planetData(1738.1, rotationType.moon,    "CSVS/ARTEMIS 3/PLANETS/moon", oneMin, planetType.moon),   lrd);
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
                    facility fd = new facility(x.Key, moon, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), antenna, new Time(2460806.5 + start), new Time(2460806.5 + stop)), frd);

                    //facility fd = new facility(x.Key, moon, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), antenna), frd);


                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                } else {
                    List<antennaData> antenna = new List<antennaData>() {new antennaData(x.Key, x.Key, new geographic(dict["Lat"], dict["Long"]), dict["Ground_Priority"])};
                    facility fd = new facility(x.Key, earth, new facilityData(x.Key, new geographic(dict["Lat"], dict["Long"]), antenna), frd);

                    if (dict["user_provider"] == "user") linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "provider") linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    if (dict["user_provider"] == "user/provider") {
                        linkBudgeting.users.Add(x.Key, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(x.Key, (true, 2460806.5, 2460836.5));
                    }
                }
            }
        }

         satellite sat1 = new satellite("cubeSat", new satelliteData(new Timeline(1837.1,0, 90, 0, 0, 0, 1, 2460806.5, MoonMu)), srd);
         satellite.addFamilyNode(moon, sat1);
         sat1.positions.enableExistanceTime(new Time(2460806.5), new Time((2460836.5)));
         moonSats.Add(sat1);

      planet.addFamilyNode(earth, moon);


      master.setReferenceFrame(earth);
      master.relationshipPlanet[earth] = new List<planet>() {moon};
      master.relationshipSatellite[moon] = moonSats;
      master.relationshipSatellite[earth] = earthSats;

      master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/PLANETS/moon", oneMin));
      master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/SATS/v", 0.0006944444));

      //windows.jsonWindows();
    }

    private void starlink()
    {

      representationData rd = new representationData(
          "Prefabs/Planet",
          "Materials/default");

      representationData lrd = new representationData(
          "Prefabs/Planet",
          "Materials/default",
          new Vector3(0f,0f,0f));

      representationData srd = new representationData(
          "Prefabs/Satellite",
          "Materials/default");

      representationData frd = new representationData(
          "Prefabs/Facility",
          "Materials/default");

      representationData erd = new representationData(
          "Prefabs/Planet",
          "Materials/planets/earth/earthEquirectangular");

      double oneMin = 0.0006944444;
      double oneHour = 0.0416666667;

      double MoonMu = 4902.800066;
      double EarthMu = 398600.435436;

      earth =       new planet(  "Earth", new planetData(  6371, rotationType.earth, "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), erd);
      planet moon = new planet(   "Luna", new planetData(1738.1, rotationType.moon,    "CSVS/ARTEMIS 3/PLANETS/moon", oneMin, planetType.moon),   lrd);
                    new planet("Mercury", new planetData(2439.7, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/mercury", oneHour, planetType.planet), rd);
                    new planet(  "Venus", new planetData(6051.8, rotationType.none,   "CSVS/ARTEMIS 3/PLANETS/venus", oneHour, planetType.planet), rd);
                    new planet(   "Mars", new planetData(3396.2, rotationType.none,    "CSVS/ARTEMIS 3/PLANETS/mars", oneHour, planetType.planet), rd);
                    new planet("Jupiter", new planetData( 71492, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/jupiter", oneHour, planetType.planet), rd);
                    new planet( "Saturn", new planetData( 60268, rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/saturn", oneHour, planetType.planet), rd);
                    new planet( "Uranus", new planetData( 25559, rotationType.none,  "CSVS/ARTEMIS 3/PLANETS/uranus", oneHour, planetType.planet), rd);
                    new planet("Neptune", new planetData( 24764, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/neptune", oneHour, planetType.planet), rd);

      List<string> sats = new List<string>()
      {
          "STARLINK-24",
          "STARLINK-61",
          "STARLINK-71",
          "STARLINK-43",
          "STARLINK-70",
          "STARLINK-80",
          "STARLINK-76",
          "STARLINK-1007",
          "STARLINK-1008",
          "STARLINK-1009",
          "STARLINK-1010",
          "STARLINK-1011",
          "STARLINK-1012",
          "STARLINK-1013",
          "STARLINK-1014",
          "STARLINK-1015",
          "STARLINK-1016",
          "STARLINK-1017",
          "STARLINK-1019",
          "STARLINK-1020",
          "STARLINK-1021",
          "STARLINK-1022",
          "STARLINK-1023",
          "STARLINK-1024",
          "STARLINK-1025",
          "STARLINK-1026",
          "STARLINK-1027",
          "STARLINK-1028",
          "STARLINK-1029",
          "STARLINK-1030",
          "STARLINK-1031",
          "STARLINK-1032",
          "STARLINK-1033",
          "STARLINK-1052",
          "STARLINK-1035",
          "STARLINK-1036",
          "STARLINK-1037",
          "STARLINK-1038",
          "STARLINK-1039",
          "STARLINK-1041",
          "STARLINK-1042",
          "STARLINK-1043",
          "STARLINK-1044",
          "STARLINK-1046",
          "STARLINK-1047",
          "STARLINK-1048",
          "STARLINK-1049",
          "STARLINK-1050",
          "STARLINK-1051",
          "STARLINK-1034",
          "STARLINK-1053",
          "STARLINK-1054",
          "STARLINK-1055",
          "STARLINK-1056",
          "STARLINK-1057",
          "STARLINK-1058",
          "STARLINK-1059",
          "STARLINK-1060",
          "STARLINK-1061",
          "STARLINK-1062",
          "STARLINK-1063",
          "STARLINK-1064",
          "STARLINK-1065",
          "STARLINK-1067",
          "STARLINK-1068",
          "STARLINK-1073",
          "STARLINK-1084",
          "STARLINK-1097",
          "STARLINK-1098",
          "STARLINK-1099",
          "STARLINK-1101",
          "STARLINK-1102",
          "STARLINK-1103",
          "STARLINK-1104",
          "STARLINK-1106",
          "STARLINK-1111",
          "STARLINK-1112",
          "STARLINK-1113",
          "STARLINK-1114",
          "STARLINK-1119",
          "STARLINK-1121",
          "STARLINK-1123",
          "STARLINK-1128",
          "STARLINK-1130 (DARKSAT)",
          "STARLINK-1144",
          "STARLINK-1071",
          "STARLINK-1072",
          "STARLINK-1078",
          "STARLINK-1079",
          "STARLINK-1082",
          "STARLINK-1083",
          "STARLINK-1091",
          "STARLINK-1094",
          "STARLINK-1096",
          "STARLINK-1100",
          "STARLINK-1108",
          "STARLINK-1109",
          "STARLINK-1110",
          "STARLINK-1116",
          "STARLINK-1122",
          "STARLINK-1125",
          "STARLINK-1126",
          "STARLINK-1117",
          "STARLINK-1124",
          "STARLINK-1066",
          "STARLINK-1069",
          "STARLINK-1070",
          "STARLINK-1074",
          "STARLINK-1076",
          "STARLINK-1080",
          "STARLINK-1081",
          "STARLINK-1085",
          "STARLINK-1086",
          "STARLINK-1088",
          "STARLINK-1089",
          "STARLINK-1090",
          "STARLINK-1092",
          "STARLINK-1093",
          "STARLINK-1095",
          "STARLINK-1107",
          "STARLINK-1115",
          "STARLINK-1132",
          "STARLINK-1120",
          "STARLINK-1129",
          "STARLINK-1131",
          "STARLINK-1134",
          "STARLINK-1135",
          "STARLINK-1140",
          "STARLINK-1141",
          "STARLINK-1148",
          "STARLINK-1155",
          "STARLINK-1156",
          "STARLINK-1159",
          "STARLINK-1162",
          "STARLINK-1165",
          "STARLINK-1166",
          "STARLINK-1169",
          "STARLINK-1171",
          "STARLINK-1178",
          "STARLINK-1133",
          "STARLINK-1139",
          "STARLINK-1145",
          "STARLINK-1150",
          "STARLINK-1161",
          "STARLINK-1163",
          "STARLINK-1167",
          "STARLINK-1168",
          "STARLINK-1170",
          "STARLINK-1172",
          "STARLINK-1174",
          "STARLINK-1180",
          "STARLINK-1182",
          "STARLINK-1177",
          "STARLINK-1149",
          "STARLINK-1153",
          "STARLINK-1151",
          "STARLINK-1160",
          "STARLINK-1190",
          "STARLINK-1173",
          "STARLINK-1179",
          "STARLINK-1181",
          "STARLINK-1185",
          "STARLINK-1183",
          "STARLINK-1136",
          "STARLINK-1176",
          "STARLINK-1127",
          "STARLINK-1137",
          "STARLINK-1142",
          "STARLINK-1146",
          "STARLINK-1147",
          "STARLINK-1152",
          "STARLINK-1184",
          "STARLINK-1186",
          "STARLINK-1193",
          "STARLINK-1194",
          "STARLINK-1195",
          "STARLINK-1196",
          "STARLINK-1138",
          "STARLINK-1143",
          "STARLINK-1192",
          "STARLINK-1200",
          "STARLINK-1201",
          "STARLINK-1202",
          "STARLINK-1205",
          "STARLINK-1216",
          "STARLINK-1224",
          "STARLINK-1225",
          "STARLINK-1228",
          "STARLINK-1230",
          "STARLINK-1234",
          "STARLINK-1236",
          "STARLINK-1237",
          "STARLINK-1239",
          "STARLINK-1240",
          "STARLINK-1241",
          "STARLINK-1244",
          "STARLINK-1269",
          "STARLINK-1154",
          "STARLINK-1197",
          "STARLINK-1199",
          "STARLINK-1203",
          "STARLINK-1204",
          "STARLINK-1206",
          "STARLINK-1208",
          "STARLINK-1209",
          "STARLINK-1210",
          "STARLINK-1211",
          "STARLINK-1218",
          "STARLINK-1219",
          "STARLINK-1231",
          "STARLINK-1232",
          "STARLINK-1233",
          "STARLINK-1245",
          "STARLINK-1254",
          "STARLINK-1271",
          "STARLINK-1187",
          "STARLINK-1188",
          "STARLINK-1189",
          "STARLINK-1191",
          "STARLINK-1212",
          "STARLINK-1214",
          "STARLINK-1215",
          "STARLINK-1217",
          "STARLINK-1221",
          "STARLINK-1222",
          "STARLINK-1226",
          "STARLINK-1227",
          "STARLINK-1229",
          "STARLINK-1235",
          "STARLINK-1238",
          "STARLINK-1243",
          "STARLINK-1246",
          "STARLINK-1247",
          "STARLINK-1270",
          "STARLINK-1279",
          "STARLINK-1301",
          "STARLINK-1306",
          "STARLINK-1313",
          "STARLINK-1317",
          "STARLINK-1262",
          "STARLINK-1273",
          "STARLINK-1276",
          "STARLINK-1277",
          "STARLINK-1281",
          "STARLINK-1287",
          "STARLINK-1288",
          "STARLINK-1295",
          "STARLINK-1300",
          "STARLINK-1302",
          "STARLINK-1304",
          "STARLINK-1305",
          "STARLINK-1310",
          "STARLINK-1319",
          "STARLINK-1207",
          "STARLINK-1258",
          "STARLINK-1264",
          "STARLINK-1266",
          "STARLINK-1267",
          "STARLINK-1272",
          "STARLINK-1274",
          "STARLINK-1280",
          "STARLINK-1283",
          "STARLINK-1284",
          "STARLINK-1289",
          "STARLINK-1290",
          "STARLINK-1291",
          "STARLINK-1292",
          "STARLINK-1297",
          "STARLINK-1303",
          "STARLINK-1307",
          "STARLINK-1312",
          "STARLINK-1255",
          "STARLINK-1213",
          "STARLINK-1256",
          "STARLINK-1257",
          "STARLINK-1259",
          "STARLINK-1260",
          "STARLINK-1263",
          "STARLINK-1265",
          "STARLINK-1275",
          "STARLINK-1278",
          "STARLINK-1282",
          "STARLINK-1285",
          "STARLINK-1293",
          "STARLINK-1296",
          "STARLINK-1298",
          "STARLINK-1309",
          "STARLINK-1316",
          "STARLINK-1318",
          "STARLINK-1286",
          "STARLINK-1299",
          "STARLINK-1308",
          "STARLINK-1329",
          "STARLINK-1338",
          "STARLINK-1339",
          "STARLINK-1341",
          "STARLINK-1350",
          "STARLINK-1352",
          "STARLINK-1353",
          "STARLINK-1362",
          "STARLINK-1367",
          "STARLINK-1368",
          "STARLINK-1369",
          "STARLINK-1371",
          "STARLINK-1372",
          "STARLINK-1373",
          "STARLINK-1374",
          "STARLINK-1375",
          "STARLINK-1377",
          "STARLINK-1378",
          "STARLINK-1379",
          "STARLINK-1390"
          /*"STARLINK-1294",
          "STARLINK-1322",
          "STARLINK-1323",
          "STARLINK-1325",
          "STARLINK-1327",
          "STARLINK-1334",
          "STARLINK-1336",
          "STARLINK-1342",
          "STARLINK-1344",
          "STARLINK-1346",
          "STARLINK-1348",
          "STARLINK-1354",
          "STARLINK-1355",
          "STARLINK-1356",
          "STARLINK-1357",
          "STARLINK-1358",
          "STARLINK-1361",
          "STARLINK-1363",
          "STARLINK-1366",
          "STARLINK-1376",
          "STARLINK-1261",
          "STARLINK-1320",
          "STARLINK-1321",
          "STARLINK-1324",
          "STARLINK-1326",
          "STARLINK-1328",
          "STARLINK-1330",
          "STARLINK-1331",
          "STARLINK-1332",
          "STARLINK-1333",
          "STARLINK-1335",
          "STARLINK-1337",
          "STARLINK-1340",
          "STARLINK-1343",
          "STARLINK-1345",
          "STARLINK-1347",
          "STARLINK-1349",
          "STARLINK-1360",
          "STARLINK-1364",
          "STARLINK-1365",
          "STARLINK-1441",
          "STARLINK-1442",
          "STARLINK-1443",
          "STARLINK-1444",
          "STARLINK-1445",
          "STARLINK-1446",
          "STARLINK-1448",
          "STARLINK-1449",
          "STARLINK-1450",
          "STARLINK-1451",
          "STARLINK-1452",
          "STARLINK-1453",
          "STARLINK-1454",
          "STARLINK-1455",
          "STARLINK-1456",
          "STARLINK-1457",
          "STARLINK-1458",
          "STARLINK-1460",
          "STARLINK-1392",
          "STARLINK-1393",
          "STARLINK-1394",
          "STARLINK-1395",
          "STARLINK-1396",
          "STARLINK-1397",
          "STARLINK-1399",
          "STARLINK-1401",
          "STARLINK-1402",
          "STARLINK-1404",
          "STARLINK-1406",
          "STARLINK-1408",
          "STARLINK-1413",
          "STARLINK-1414",
          "STARLINK-1415",
          "STARLINK-1416",
          "STARLINK-1417",
          "STARLINK-1419",
          "STARLINK-1420",
          "STARLINK-1422",
          "STARLINK-1351",
          "STARLINK-1370",
          "STARLINK-1398",
          "STARLINK-1400",
          "STARLINK-1403",
          "STARLINK-1405",
          "STARLINK-1407",
          "STARLINK-1409",
          "STARLINK-1410",
          "STARLINK-1411",
          "STARLINK-1412",
          "STARLINK-1418",
          "STARLINK-1421",
          "STARLINK-1423",
          "STARLINK-1433",
          "STARLINK-1434",
          "STARLINK-1436",
          "STARLINK-1437",
          "STARLINK-1439",
          "STARLINK-1461",
          "STARLINK-1465",
          "STARLINK-1466",
          "STARLINK-1467",
          "STARLINK-1468",
          "STARLINK-1471",
          "STARLINK-1472",
          "STARLINK-1474",
          "STARLINK-1475",
          "STARLINK-1479",
          "STARLINK-1480",
          "STARLINK-1481",
          "STARLINK-1483",
          "STARLINK-1500",
          "STARLINK-1503",
          "STARLINK-1504",
          "STARLINK-1506",
          "STARLINK-1507",
          "STARLINK-1391",
          "STARLINK-1464",
          "STARLINK-1469",
          "STARLINK-1476",
          "STARLINK-1477",
          "STARLINK-1478",
          "STARLINK-1484",
          "STARLINK-1486",
          "STARLINK-1487",
          "STARLINK-1493",
          "STARLINK-1494",
          "STARLINK-1495",
          "STARLINK-1499",
          "STARLINK-1501",
          "STARLINK-1502",
          "STARLINK-1508",
          "STARLINK-1509",
          "STARLINK-1511",
          "STARLINK-1521",
          "STARLINK-1459",
          "STARLINK-1462",
          "STARLINK-1463",
          "STARLINK-1470",
          "STARLINK-1482",
          "STARLINK-1485",
          "STARLINK-1488",
          "STARLINK-1489",
          "STARLINK-1490",
          "STARLINK-1491",
          "STARLINK-1492",
          "STARLINK-1496",
          "STARLINK-1497",
          "STARLINK-1498",
          "STARLINK-1505",
          "STARLINK-1510",
          "STARLINK-1512",
          "STARLINK-1513",
          "STARLINK-1517",
          "STARLINK-1522",
          "STARLINK-1523",
          "STARLINK-1526",
          "STARLINK-1534",
          "STARLINK-1544",
          "STARLINK-1555",
          "STARLINK-1556",
          "STARLINK-1557",
          "STARLINK-1558",
          "STARLINK-1560",
          "STARLINK-1565",
          "STARLINK-1567",
          "STARLINK-1569",
          "STARLINK-1576",
          "STARLINK-1580",
          "STARLINK-1581",
          "STARLINK-1582",
          "STARLINK-1584",
          "STARLINK-1591",
          "STARLINK-1514",
          "STARLINK-1524",
          "STARLINK-1527",
          "STARLINK-1530",
          "STARLINK-1535",
          "STARLINK-1540",
          "STARLINK-1541",
          "STARLINK-1543",
          "STARLINK-1548",
          "STARLINK-1554",
          "STARLINK-1561",
          "STARLINK-1562",
          "STARLINK-1564",
          "STARLINK-1570",
          "STARLINK-1572",
          "STARLINK-1573",
          "STARLINK-1574",
          "STARLINK-1577",
          "STARLINK-1583",
          "STARLINK-1515",
          "STARLINK-1525",
          "STARLINK-1529",
          "STARLINK-1532",
          "STARLINK-1533",
          "STARLINK-1536",
          "STARLINK-1538",
          "STARLINK-1539",
          "STARLINK-1542",
          "STARLINK-1549",
          "STARLINK-1551",
          "STARLINK-1552",
          "STARLINK-1559",
          "STARLINK-1563",
          "STARLINK-1566",
          "STARLINK-1568",
          "STARLINK-1571",
          "STARLINK-1578",
          "STARLINK-1579",
          "STARLINK-1585",
          "STARLINK-1588",
          "STARLINK-1593",
          "STARLINK-1601",
          "STARLINK-1602",
          "STARLINK-1604",
          "STARLINK-1605",
          "STARLINK-1614",
          "STARLINK-1618",
          "STARLINK-1619",
          "STARLINK-1621",
          "STARLINK-1622",
          "STARLINK-1623",
          "STARLINK-1624",
          "STARLINK-1625",
          "STARLINK-1630",
          "STARLINK-1637",
          "STARLINK-1638",
          "STARLINK-1639",
          "STARLINK-1643",
          "STARLINK-1586",
          "STARLINK-1590",
          "STARLINK-1594",
          "STARLINK-1596",
          "STARLINK-1597",
          "STARLINK-1599",
          "STARLINK-1606",
          "STARLINK-1607",
          "STARLINK-1608",
          "STARLINK-1611",
          "STARLINK-1616",
          "STARLINK-1620",
          "STARLINK-1629",
          "STARLINK-1631",
          "STARLINK-1634",
          "STARLINK-1636",
          "STARLINK-1642",
          "STARLINK-1667",
          "STARLINK-1545",
          "STARLINK-1587",
          "STARLINK-1589",
          "STARLINK-1595",
          "STARLINK-1598",
          "STARLINK-1600",
          "STARLINK-1603",
          "STARLINK-1610",
          "STARLINK-1612",
          "STARLINK-1613",
          "STARLINK-1615",
          "STARLINK-1626",
          "STARLINK-1627",
          "STARLINK-1628",
          "STARLINK-1632",
          "STARLINK-1633",
          "STARLINK-1635",
          "STARLINK-1640",
          "STARLINK-1641",
          "STARLINK-1734",
          "STARLINK-1654",
          "STARLINK-1673",
          "STARLINK-1686",
          "STARLINK-1695",
          "STARLINK-1710",
          "STARLINK-1719",
          "STARLINK-1721",
          "STARLINK-1723",
          "STARLINK-1725",
          "STARLINK-1727",
          "STARLINK-1738",
          "STARLINK-1750",
          "STARLINK-1752",
          "STARLINK-1757",
          "STARLINK-1759",
          "STARLINK-1760",
          "STARLINK-1762",
          "STARLINK-1764",
          "STARLINK-1765",
          "STARLINK-1767",
          "STARLINK-1546",
          "STARLINK-1547",
          "STARLINK-1553",
          "STARLINK-1575",
          "STARLINK-1617",
          "STARLINK-1646",
          "STARLINK-1653",
          "STARLINK-1656",
          "STARLINK-1657",
          "STARLINK-1661",
          "STARLINK-1665",
          "STARLINK-1666",
          "STARLINK-1690",
          "STARLINK-1707",
          "STARLINK-1713",
          "STARLINK-1722",
          "STARLINK-1726",
          "STARLINK-1739",
          "STARLINK-1763",
          "STARLINK-1550",
          "STARLINK-1651",
          "STARLINK-1658",
          "STARLINK-1662",
          "STARLINK-1670",
          "STARLINK-1688",
          "STARLINK-1689",
          "STARLINK-1691",
          "STARLINK-1711",
          "STARLINK-1724",
          "STARLINK-1742",
          "STARLINK-1745",
          "STARLINK-1751",
          "STARLINK-1756",
          "STARLINK-1758",
          "STARLINK-1768",
          "STARLINK-1769",
          "STARLINK-1770",
          "STARLINK-1771",
          "STARLINK-1644",
          "STARLINK-1648",
          "STARLINK-1659",
          "STARLINK-1663",
          "STARLINK-1668",
          "STARLINK-1672",
          "STARLINK-1678",
          "STARLINK-1684",
          "STARLINK-1685",
          "STARLINK-1687",
          "STARLINK-1692",
          "STARLINK-1693",
          "STARLINK-1694",
          "STARLINK-1696",
          "STARLINK-1697",
          "STARLINK-1698",
          "STARLINK-1699",
          "STARLINK-1700",
          "STARLINK-1701",
          "STARLINK-1649",
          "STARLINK-1664",
          "STARLINK-1671",
          "STARLINK-1674",
          "STARLINK-1676",
          "STARLINK-1679",
          "STARLINK-1680",
          "STARLINK-1681",
          "STARLINK-1706",
          "STARLINK-1709",
          "STARLINK-1714",
          "STARLINK-1730",
          "STARLINK-1733",
          "STARLINK-1735",
          "STARLINK-1740",
          "STARLINK-1741",
          "STARLINK-1743",
          "STARLINK-1747",
          "STARLINK-1748",
          "STARLINK-1753",
          "STARLINK-1531",
          "STARLINK-1650",
          "STARLINK-1660",
          "STARLINK-1675",
          "STARLINK-1677",
          "STARLINK-1682",
          "STARLINK-1683",
          "STARLINK-1705",
          "STARLINK-1708",
          "STARLINK-1712",
          "STARLINK-1728",
          "STARLINK-1729",
          "STARLINK-1732",
          "STARLINK-1736",
          "STARLINK-1737",
          "STARLINK-1746",
          "STARLINK-1749",
          "STARLINK-1754",
          "STARLINK-1755",
          "STARLINK-1715",
          "STARLINK-1716",
          "STARLINK-1717",
          "STARLINK-1718",
          "STARLINK-1720",
          "STARLINK-1731",
          "STARLINK-1766",
          "STARLINK-1773",
          "STARLINK-1774",
          "STARLINK-1775",
          "STARLINK-1776",
          "STARLINK-1778",
          "STARLINK-1780",
          "STARLINK-1781",
          "STARLINK-1783",
          "STARLINK-1784",
          "STARLINK-1786",
          "STARLINK-1788",
          "STARLINK-1789",
          "STARLINK-1790",
          "STARLINK-1791",
          "STARLINK-1792",
          "STARLINK-1793",
          "STARLINK-1794",
          "STARLINK-1795",
          "STARLINK-1796",
          "STARLINK-1797",
          "STARLINK-1799",
          "STARLINK-1800",
          "STARLINK-1801",
          "STARLINK-1802",
          "STARLINK-1803",
          "STARLINK-1804",
          "STARLINK-1805",
          "STARLINK-1807",
          "STARLINK-1808",
          "STARLINK-1809",
          "STARLINK-1810",
          "STARLINK-1811",
          "STARLINK-1813",
          "STARLINK-1814",
          "STARLINK-1815",
          "STARLINK-1816",
          "STARLINK-1817",
          "STARLINK-1818",
          "STARLINK-1820",
          "STARLINK-1821",
          "STARLINK-1822",
          "STARLINK-1823",
          "STARLINK-1824",
          "STARLINK-1825",
          "STARLINK-1826",
          "STARLINK-1827",
          "STARLINK-1828",
          "STARLINK-1829",
          "STARLINK-1830",
          "STARLINK-1831",
          "STARLINK-1848",
          "STARLINK-1865",
          "STARLINK-1872",
          "STARLINK-1892",
          "STARLINK-1894",
          "STARLINK-1898",
          "STARLINK-1905",
          "STARLINK-1908",
          "STARLINK-1910",
          "STARLINK-1911",
          "STARLINK-1920",
          "STARLINK-1921",
          "STARLINK-1922",
          "STARLINK-1923",
          "STARLINK-1924",
          "STARLINK-1925",
          "STARLINK-1926",
          "STARLINK-1928",
          "STARLINK-1833",
          "STARLINK-1896",
          "STARLINK-1897",
          "STARLINK-1901",
          "STARLINK-1902",
          "STARLINK-1903",
          "STARLINK-1906",
          "STARLINK-1916",
          "STARLINK-1917",
          "STARLINK-1918",
          "STARLINK-1919",
          "STARLINK-1932",
          "STARLINK-1935",
          "STARLINK-1936",
          "STARLINK-1937",
          "STARLINK-1939",
          "STARLINK-1945",
          "STARLINK-1946",
          "STARLINK-1949",
          "STARLINK-1798",
          "STARLINK-1832",
          "STARLINK-1834",
          "STARLINK-1835",
          "STARLINK-1851",
          "STARLINK-1882",
          "STARLINK-1883",
          "STARLINK-1893",
          "STARLINK-1899",
          "STARLINK-1929",
          "STARLINK-1930",
          "STARLINK-1931",
          "STARLINK-1933",
          "STARLINK-1934",
          "STARLINK-1941",
          "STARLINK-1942",
          "STARLINK-1943",
          "STARLINK-1944",
          "STARLINK-1947",
          "STARLINK-1948",
          "STARLINK-1777",
          "STARLINK-1779",
          "STARLINK-1785",
          "STARLINK-1787",
          "STARLINK-1812",
          "STARLINK-1836",
          "STARLINK-1837",
          "STARLINK-1838",
          "STARLINK-1839",
          "STARLINK-1840",
          "STARLINK-1843",
          "STARLINK-1844",
          "STARLINK-1845",
          "STARLINK-1846",
          "STARLINK-1849",
          "STARLINK-1850",
          "STARLINK-1852",
          "STARLINK-1853",
          "STARLINK-1854",
          "STARLINK-1855",
          "STARLINK-1856",
          "STARLINK-1857",
          "STARLINK-1858",
          "STARLINK-1859",
          "STARLINK-1860",
          "STARLINK-1861",
          "STARLINK-1862",
          "STARLINK-1863",
          "STARLINK-1864",
          "STARLINK-1866",
          "STARLINK-1867",
          "STARLINK-1868",
          "STARLINK-1869",
          "STARLINK-1870",
          "STARLINK-1871",
          "STARLINK-1873",
          "STARLINK-1874",
          "STARLINK-1875",
          "STARLINK-1876",
          "STARLINK-1877",
          "STARLINK-1878",
          "STARLINK-1879",
          "STARLINK-1880",
          "STARLINK-1881",
          "STARLINK-1884",
          "STARLINK-1885",
          "STARLINK-1886",
          "STARLINK-1887",
          "STARLINK-1888",
          "STARLINK-1889",
          "STARLINK-1890",
          "STARLINK-1891",
          "STARLINK-1895",
          "STARLINK-1907",
          "STARLINK-1912",
          "STARLINK-1913",
          "STARLINK-1914",
          "STARLINK-1927",
          "STARLINK-1952",
          "STARLINK-2011",
          "STARLINK-2017",
          "STARLINK-2034",
          "STARLINK-2045",
          "STARLINK-2046",
          "STARLINK-2047",
          "STARLINK-2049",
          "STARLINK-2050",
          "STARLINK-2055",
          "STARLINK-2069",
          "STARLINK-2070",
          "STARLINK-2071",
          "STARLINK-2076",
          "STARLINK-2077",
          "STARLINK-2079",
          "STARLINK-2080",
          "STARLINK-2081",
          "STARLINK-2082",
          "STARLINK-2084",
          "STARLINK-2085",
          "STARLINK-2086",
          "STARLINK-2088",
          "STARLINK-2089",
          "STARLINK-2092",
          "STARLINK-2093",
          "STARLINK-2094",
          "STARLINK-2096",
          "STARLINK-2097",
          "STARLINK-2098",
          "STARLINK-2099",
          "STARLINK-2100",
          "STARLINK-2101",
          "STARLINK-2102",
          "STARLINK-2103",
          "STARLINK-2104",
          "STARLINK-2105",
          "STARLINK-2106",
          "STARLINK-2108",
          "STARLINK-2109",
          "STARLINK-2110",
          "STARLINK-2111",
          "STARLINK-2112",
          "STARLINK-2113",
          "STARLINK-2114",
          "STARLINK-2115",
          "STARLINK-2117",
          "STARLINK-2118",
          "STARLINK-2119",
          "STARLINK-2120",
          "STARLINK-2121",
          "STARLINK-2122",
          "STARLINK-2123",
          "STARLINK-2124",
          "STARLINK-2127",
          "STARLINK-2128",
          "STARLINK-2130",
          "STARLINK-2133",
          "STARLINK-2134",
          "STARLINK-2135",
          "STARLINK-2199",
          "STARLINK-2200",
          "STARLINK-2201",
          "STARLINK-2202",
          "STARLINK-2203",
          "STARLINK-2204",
          "STARLINK-2205",
          "STARLINK-2206",
          "STARLINK-2207",
          "STARLINK-2208",
          "STARLINK-1782",
          "STARLINK-1806",
          "STARLINK-1909",
          "STARLINK-1938",
          "STARLINK-1940",
          "STARLINK-1951",
          "STARLINK-1953",
          "STARLINK-1954",
          "STARLINK-1955",
          "STARLINK-1956",
          "STARLINK-1957",
          "STARLINK-1958",
          "STARLINK-1959",
          "STARLINK-1960",
          "STARLINK-1961",
          "STARLINK-1962",
          "STARLINK-1963",
          "STARLINK-1964",
          "STARLINK-1965",
          "STARLINK-1966",
          "STARLINK-1967",
          "STARLINK-1968",
          "STARLINK-1969",
          "STARLINK-1970",
          "STARLINK-1971",
          "STARLINK-1975",
          "STARLINK-1976",
          "STARLINK-1977",
          "STARLINK-1978",
          "STARLINK-1979",
          "STARLINK-1980",
          "STARLINK-1981",
          "STARLINK-1982",
          "STARLINK-1984",
          "STARLINK-1986",
          "STARLINK-1987",
          "STARLINK-1988",
          "STARLINK-1989",
          "STARLINK-1990",
          "STARLINK-1991",
          "STARLINK-1993",
          "STARLINK-1994",
          "STARLINK-1995",
          "STARLINK-1996",
          "STARLINK-1997",
          "STARLINK-1998",
          "STARLINK-1999",
          "STARLINK-2000",
          "STARLINK-2001",
          "STARLINK-2002",
          "STARLINK-2003",
          "STARLINK-2004",
          "STARLINK-2005",
          "STARLINK-2006",
          "STARLINK-2007",
          "STARLINK-2008",
          "STARLINK-2021",
          "STARLINK-2023",
          "STARLINK-2024",
          "STARLINK-2025",
          "STARLINK-1528",
          "STARLINK-1609",
          "STARLINK-1645",
          "STARLINK-1655",
          "STARLINK-1669",
          "STARLINK-1704",
          "STARLINK-1761",
          "STARLINK-1972",
          "STARLINK-1973",
          "STARLINK-1974",
          "STARLINK-1983",
          "STARLINK-1985",
          "STARLINK-1992",
          "STARLINK-2009",
          "STARLINK-2010",
          "STARLINK-2012",
          "STARLINK-2013",
          "STARLINK-2014",
          "STARLINK-2015",
          "STARLINK-2016",
          "STARLINK-2018",
          "STARLINK-2019",
          "STARLINK-2020",
          "STARLINK-2022",
          "STARLINK-2026",
          "STARLINK-2027",
          "STARLINK-2028",
          "STARLINK-2030",
          "STARLINK-2031",
          "STARLINK-2032",
          "STARLINK-2033",
          "STARLINK-2035",
          "STARLINK-2036",
          "STARLINK-2037",
          "STARLINK-2038",
          "STARLINK-2039",
          "STARLINK-2040",
          "STARLINK-2041",
          "STARLINK-2042",
          "STARLINK-2043",
          "STARLINK-2044",
          "STARLINK-2051",
          "STARLINK-2052",
          "STARLINK-2053",
          "STARLINK-2054",
          "STARLINK-2056",
          "STARLINK-2057",
          "STARLINK-2058",
          "STARLINK-2059",
          "STARLINK-2060",
          "STARLINK-2062",
          "STARLINK-2064",
          "STARLINK-2065",
          "STARLINK-2066",
          "STARLINK-2067",
          "STARLINK-2078",
          "STARLINK-2083",
          "STARLINK-2090",
          "STARLINK-2091",
          "STARLINK-2095",
          "STARLINK-2068",
          "STARLINK-2107",
          "STARLINK-2116",
          "STARLINK-2125",
          "STARLINK-2126",
          "STARLINK-2129",
          "STARLINK-2131",
          "STARLINK-2132",
          "STARLINK-2140",
          "STARLINK-2141",
          "STARLINK-2142",
          "STARLINK-2143",
          "STARLINK-2144",
          "STARLINK-2146",
          "STARLINK-2147",
          "STARLINK-2148",
          "STARLINK-2149",
          "STARLINK-2150",
          "STARLINK-2152",
          "STARLINK-2154",
          "STARLINK-2156",
          "STARLINK-2157",
          "STARLINK-2158",
          "STARLINK-2159",
          "STARLINK-2160",
          "STARLINK-2161",
          "STARLINK-2162",
          "STARLINK-2163",
          "STARLINK-2164",
          "STARLINK-2168",
          "STARLINK-2169",
          "STARLINK-2170",
          "STARLINK-2171",
          "STARLINK-2172",
          "STARLINK-2174",
          "STARLINK-2175",
          "STARLINK-2176",
          "STARLINK-2177",
          "STARLINK-2178",
          "STARLINK-2179",
          "STARLINK-2180",
          "STARLINK-2181",
          "STARLINK-2182",
          "STARLINK-2183",
          "STARLINK-2184",
          "STARLINK-2185",
          "STARLINK-2189",
          "STARLINK-2192",
          "STARLINK-2193",
          "STARLINK-2194",
          "STARLINK-2195",
          "STARLINK-2196",
          "STARLINK-2197",
          "STARLINK-2198",
          "STARLINK-2209",
          "STARLINK-2210",
          "STARLINK-2211",
          "STARLINK-2212",
          "STARLINK-2213",
          "STARLINK-2223",
          "STARLINK-2257",
          "STARLINK-2314",
          "STARLINK-2315",
          "STARLINK-2319",
          "STARLINK-2322",
          "STARLINK-2334",
          "STARLINK-2338",
          "STARLINK-2341",
          "STARLINK-2347",
          "STARLINK-2373",
          "STARLINK-2377",
          "STARLINK-2379",
          "STARLINK-2380",
          "STARLINK-2381",
          "STARLINK-2382",
          "STARLINK-2383",
          "STARLINK-2384",
          "STARLINK-2385",
          "STARLINK-2386",
          "STARLINK-2387",
          "STARLINK-2388",
          "STARLINK-2389",
          "STARLINK-2390",
          "STARLINK-2391",
          "STARLINK-2392",
          "STARLINK-2393",
          "STARLINK-2394",
          "STARLINK-2395",
          "STARLINK-2396",
          "STARLINK-2399",
          "STARLINK-2400",
          "STARLINK-2401",
          "STARLINK-2402",
          "STARLINK-2403",
          "STARLINK-2406",
          "STARLINK-2407",
          "STARLINK-2408",
          "STARLINK-2409",
          "STARLINK-2410",
          "STARLINK-2411",
          "STARLINK-2413",
          "STARLINK-2415",
          "STARLINK-2416",
          "STARLINK-2419",
          "STARLINK-2420",
          "STARLINK-2422",
          "STARLINK-2423",
          "STARLINK-2424",
          "STARLINK-2425",
          "STARLINK-2426",
          "STARLINK-2427",
          "STARLINK-2429",
          "STARLINK-2431",
          "STARLINK-2432",
          "STARLINK-2433",
          "STARLINK-2434",
          "STARLINK-2435",
          "STARLINK-2446",
          "STARLINK-2453",
          "STARLINK-2456",
          "STARLINK-2258",
          "STARLINK-2280",
          "STARLINK-2291",
          "STARLINK-2293",
          "STARLINK-2304",
          "STARLINK-2310",
          "STARLINK-2320",
          "STARLINK-2321",
          "STARLINK-2323",
          "STARLINK-2324",
          "STARLINK-2326",
          "STARLINK-2327",
          "STARLINK-2328",
          "STARLINK-2329",
          "STARLINK-2330",
          "STARLINK-2331",
          "STARLINK-2332",
          "STARLINK-2333",
          "STARLINK-2335",
          "STARLINK-2336",
          "STARLINK-2337",
          "STARLINK-2339",
          "STARLINK-2340",
          "STARLINK-2342",
          "STARLINK-2343",
          "STARLINK-2344",
          "STARLINK-2345",
          "STARLINK-2346",
          "STARLINK-2348",
          "STARLINK-2349",
          "STARLINK-2350",
          "STARLINK-2351",
          "STARLINK-2352",
          "STARLINK-2354",
          "STARLINK-2355",
          "STARLINK-2356",
          "STARLINK-2357",
          "STARLINK-2358",
          "STARLINK-2359",
          "STARLINK-2360",
          "STARLINK-2361",
          "STARLINK-2362",
          "STARLINK-2363",
          "STARLINK-2364",
          "STARLINK-2365",
          "STARLINK-2366",
          "STARLINK-2367",
          "STARLINK-2368",
          "STARLINK-2369",
          "STARLINK-2370",
          "STARLINK-2371",
          "STARLINK-2372",
          "STARLINK-2374",
          "STARLINK-2375",
          "STARLINK-2376",
          "STARLINK-2378",
          "STARLINK-2397",
          "STARLINK-2398",
          "STARLINK-2405",
          "STARLINK-2087",
          "STARLINK-1647",
          "STARLINK-2325",
          "STARLINK-2312",
          "STARLINK-2303",
          "STARLINK-2317",
          "STARLINK-2289",
          "STARLINK-2316",
          "STARLINK-2308",
          "STARLINK-2313",
          "STARLINK-2311",
          "STARLINK-2306",
          "STARLINK-2305",
          "STARLINK-2307",
          "STARLINK-2279",
          "STARLINK-2229",
          "STARLINK-2273",
          "STARLINK-2290",
          "STARLINK-2309",
          "STARLINK-2260",
          "STARLINK-2266",
          "STARLINK-2296",
          "STARLINK-2218",
          "STARLINK-2270",
          "STARLINK-2262",
          "STARLINK-2265",
          "STARLINK-2263",
          "STARLINK-2261",
          "STARLINK-2254",
          "STARLINK-2277",
          "STARLINK-2259",
          "STARLINK-2271",
          "STARLINK-2153",
          "STARLINK-2226",
          "STARLINK-2272",
          "STARLINK-2216",
          "STARLINK-2243",
          "STARLINK-2283",
          "STARLINK-2281",
          "STARLINK-2284",
          "STARLINK-2239",
          "STARLINK-2282",
          "STARLINK-2285",
          "STARLINK-2294",
          "STARLINK-2301",
          "STARLINK-2298",
          "STARLINK-2292",
          "STARLINK-2302",
          "STARLINK-2318",
          "STARLINK-2278",
          "STARLINK-2300",
          "STARLINK-2264",
          "STARLINK-2299",
          "STARLINK-2268",
          "STARLINK-2267",
          "STARLINK-2297",
          "STARLINK-2286",
          "STARLINK-2288",
          "STARLINK-2287",
          "STARLINK-2295",
          "STARLINK-2048",
          "STARLINK-2404",
          "STARLINK-2412",
          "STARLINK-2414",
          "STARLINK-2417",
          "STARLINK-2418",
          "STARLINK-2421",
          "STARLINK-2428",
          "STARLINK-2430",
          "STARLINK-2436",
          "STARLINK-2437",
          "STARLINK-2438",
          "STARLINK-2439",
          "STARLINK-2440",
          "STARLINK-2442",
          "STARLINK-2443",
          "STARLINK-2444",
          "STARLINK-2445",
          "STARLINK-2447",
          "STARLINK-2448",
          "STARLINK-2449",
          "STARLINK-2450",
          "STARLINK-2451",
          "STARLINK-2452",
          "STARLINK-2454",
          "STARLINK-2455",
          "STARLINK-2458",
          "STARLINK-2459",
          "STARLINK-2460",
          "STARLINK-2462",
          "STARLINK-2463",
          "STARLINK-2464",
          "STARLINK-2465",
          "STARLINK-2466",
          "STARLINK-2467",
          "STARLINK-2468",
          "STARLINK-2469",
          "STARLINK-2471",
          "STARLINK-2472",
          "STARLINK-2473",
          "STARLINK-2474",
          "STARLINK-2475",
          "STARLINK-2476",
          "STARLINK-2478",
          "STARLINK-2479",
          "STARLINK-2480",
          "STARLINK-2481",
          "STARLINK-2482",
          "STARLINK-2483",
          "STARLINK-2484",
          "STARLINK-2485",
          "STARLINK-2486",
          "STARLINK-2487",
          "STARLINK-2488",
          "STARLINK-2489",
          "STARLINK-2490",
          "STARLINK-2491",
          "STARLINK-2492",
          "STARLINK-2493",
          "STARLINK-2503",
          "STARLINK-2567",
          "STARLINK-2569",
          "STARLINK-2543",
          "STARLINK-2580",
          "STARLINK-2565",
          "STARLINK-2520",
          "STARLINK-2558",
          "STARLINK-2516",
          "STARLINK-2564",
          "STARLINK-2548",
          "STARLINK-2547",
          "STARLINK-2566",
          "STARLINK-2562",
          "STARLINK-2545",
          "STARLINK-2540",
          "STARLINK-2555",
          "STARLINK-2542",
          "STARLINK-2550",
          "STARLINK-2533",
          "STARLINK-2535",
          "STARLINK-2546",
          "STARLINK-2544",
          "STARLINK-2559",
          "STARLINK-2557",
          "STARLINK-2538",
          "STARLINK-2537",
          "STARLINK-2556",
          "STARLINK-2530",
          "STARLINK-2524",
          "STARLINK-2519",
          "STARLINK-2523",
          "STARLINK-2528",
          "STARLINK-2532",
          "STARLINK-2517",
          "STARLINK-2536",
          "STARLINK-2534",
          "STARLINK-2061",
          "STARLINK-2541",
          "STARLINK-2549",
          "STARLINK-2506",
          "STARLINK-2507",
          "STARLINK-2513",
          "STARLINK-2509",
          "STARLINK-2512",
          "STARLINK-2029",
          "STARLINK-2457",
          "STARLINK-2477",
          "STARLINK-2515",
          "STARLINK-2527",
          "STARLINK-2495",
          "STARLINK-2498",
          "STARLINK-2502",
          "STARLINK-2504",
          "STARLINK-2510",
          "STARLINK-2501",
          "STARLINK-2514",
          "STARLINK-2511",
          "STARLINK-2518",
          "STARLINK-2470",
          "STARLINK-2441",
          "STARLINK-2613",
          "STARLINK-2674",
          "STARLINK-2635",
          "STARLINK-2637",
          "STARLINK-2636",
          "STARLINK-2624",
          "STARLINK-2628",
          "STARLINK-2622",
          "STARLINK-2591",
          "STARLINK-2578",
          "STARLINK-2626",
          "STARLINK-2611",
          "STARLINK-2608",
          "STARLINK-2631",
          "STARLINK-2643",
          "STARLINK-2623",
          "STARLINK-2641",
          "STARLINK-2621",
          "STARLINK-2589",
          "STARLINK-2572",
          "STARLINK-2609",
          "STARLINK-2604",
          "STARLINK-2603",
          "STARLINK-2610",
          "STARLINK-2499",
          "STARLINK-2526",
          "STARLINK-2612",
          "STARLINK-2614",
          "STARLINK-2630",
          "STARLINK-2585",
          "STARLINK-2599",
          "STARLINK-2601",
          "STARLINK-2598",
          "STARLINK-2606",
          "STARLINK-2600",
          "STARLINK-2594",
          "STARLINK-1904",
          "STARLINK-2586",
          "STARLINK-2607",
          "STARLINK-2605",
          "STARLINK-2602",
          "STARLINK-2573",
          "STARLINK-2574",
          "STARLINK-2575",
          "STARLINK-2590",
          "STARLINK-2588",
          "STARLINK-2587",
          "STARLINK-2576",
          "STARLINK-2571",
          "STARLINK-2560",
          "STARLINK-2561",
          "STARLINK-2593",
          "STARLINK-2570",
          "STARLINK-2568",
          "STARLINK-2595",
          "STARLINK-2592",
          "STARLINK-2596",
          "STARLINK-2563",
          "STARLINK-2505",
          "STARLINK-2581",
          "STARLINK-2461",
          "STARLINK-2749",
          "STARLINK-2729",
          "STARLINK-2700",
          "STARLINK-2680",
          "STARLINK-2699",
          "STARLINK-2692",
          "STARLINK-2633",
          "STARLINK-2639",
          "STARLINK-2642",
          "STARLINK-2640",
          "STARLINK-2583",
          "STARLINK-2682",
          "STARLINK-2702",
          "STARLINK-2644",
          "STARLINK-2663",
          "STARLINK-2645",
          "STARLINK-2634",
          "STARLINK-2247",
          "STARLINK-2269",
          "STARLINK-2632",
          "STARLINK-2655",
          "STARLINK-2660",
          "STARLINK-2652",
          "STARLINK-2703",
          "STARLINK-2620",
          "STARLINK-2654",
          "STARLINK-2497",
          "STARLINK-2508",
          "STARLINK-2698",
          "STARLINK-2579",
          "STARLINK-2582",
          "STARLINK-2693",
          "STARLINK-2683",
          "STARLINK-2689",
          "STARLINK-2686",
          "STARLINK-2681",
          "STARLINK-2687",
          "STARLINK-2659",
          "STARLINK-2685",
          "STARLINK-2661",
          "STARLINK-2675",
          "STARLINK-2684",
          "STARLINK-2722",
          "STARLINK-2658",
          "STARLINK-2697",
          "STARLINK-2619",
          "STARLINK-2723",
          "STARLINK-2728",
          "STARLINK-2755",
          "STARLINK-2690",
          "STARLINK-2706",
          "STARLINK-2525",
          "STARLINK-2531",
          "STARLINK-2696",
          "STARLINK-2500",
          "STARLINK-2496",
          "STARLINK-2494",
          "STARLINK-2063",
          "STARLINK-2139",
          "STARLINK-2145",
          "STARLINK-2151",
          "STARLINK-2155",
          "STARLINK-2166",
          "STARLINK-2167",
          "STARLINK-2173",
          "STARLINK-2186",
          "STARLINK-2187",
          "STARLINK-2188",
          "STARLINK-2190",
          "STARLINK-2191",
          "STARLINK-2214",
          "STARLINK-2215",
          "STARLINK-2217",
          "STARLINK-2219",
          "STARLINK-2220",
          "STARLINK-2221",
          "STARLINK-2222",
          "STARLINK-2224",
          "STARLINK-2225",
          "STARLINK-2227",
          "STARLINK-2228",
          "STARLINK-2231",
          "STARLINK-2232",
          "STARLINK-2233",
          "STARLINK-2234",
          "STARLINK-2235",
          "STARLINK-2236",
          "STARLINK-2237",
          "STARLINK-2238",
          "STARLINK-2240",
          "STARLINK-2241",
          "STARLINK-2242",
          "STARLINK-2244",
          "STARLINK-2245",
          "STARLINK-2246",
          "STARLINK-2248",
          "STARLINK-2249",
          "STARLINK-2250",
          "STARLINK-2251",
          "STARLINK-2252",
          "STARLINK-2253",
          "STARLINK-2255",
          "STARLINK-2256",
          "STARLINK-2274",
          "STARLINK-2275",
          "STARLINK-2276",
          "STARLINK-2713",
          "STARLINK-2714",
          "STARLINK-2757",
          "CAPELLA-6 (WHITNEY)",
          "TYVAK-0130",
          "STARLINK-2758",
          "STARLINK-2739",
          "STARLINK-2736",
          "STARLINK-2754",
          "STARLINK-2646",
          "STARLINK-2704",
          "STARLINK-2695",
          "STARLINK-2733",
          "STARLINK-2732",
          "STARLINK-2691",
          "STARLINK-2521",
          "STARLINK-2673",
          "STARLINK-2672",
          "STARLINK-2731",
          "STARLINK-2727",
          "STARLINK-2720",
          "STARLINK-2651",
          "STARLINK-2657",
          "STARLINK-2701",
          "STARLINK-2734",
          "STARLINK-2647",
          "STARLINK-2717",
          "STARLINK-2688",
          "STARLINK-2708",
          "STARLINK-2726",
          "STARLINK-2667",
          "STARLINK-2709",
          "STARLINK-2653",
          "STARLINK-2666",
          "STARLINK-2735",
          "STARLINK-2738",
          "STARLINK-2707",
          "STARLINK-2763",
          "STARLINK-2745",
          "STARLINK-2705",
          "STARLINK-2711",
          "STARLINK-2712",
          "STARLINK-2737",
          "STARLINK-2746",
          "STARLINK-2719",
          "STARLINK-2615",
          "STARLINK-2648",
          "STARLINK-2649",
          "STARLINK-2725",
          "STARLINK-2743",
          "STARLINK-2756",
          "STARLINK-2741",
          "STARLINK-2751",
          "STARLINK-2629",
          "STARLINK-2627",
          "STARLINK-2742",
          "STARLINK-2617",
          "STARLINK-2740",
          "STARLINK-2750",
          "STARLINK-2752",
          "STARLINK-2618",
          "STARLINK-2748",
          "STARLINK-2616",
          "STARLINK-2753",
          "STARLINK-2715",
          "STARLINK-3003",
          "STARLINK-3004",
          "STARLINK-3005",*/
      };

      master.relationshipPlanet.Add(earth, new List<planet>() {moon});
      master.relationshipSatellite.Add(earth, new List<satellite>() {});
      master.relationshipSatellite.Add(moon, new List<satellite>() {});

      foreach (string sat in sats) 
      {
          Timeline t = csvParser.loadPlanetCsv($"CSVS/STARLINK/{sat}", oneMin);
          if (t.find(new Time(2460806.5)) == t.find(new Time(2460806.5)))
          {
            satellite s = new satellite(sat, new satelliteData(t), srd);
            master.relationshipSatellite[earth].Add(s);
          }
      }

      master.setReferenceFrame(earth);

    }
}