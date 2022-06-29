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
    private double speed = 0.00005;
    private Vector3 planetFocusMousePosition, planetFocusMousePosition1;
    private Coroutine loop;

    void Awake() {
        // eventually i want to be able to enable this- the only thing currently preventing this is Physics.raycast
        //Physics.autoSimulation = false;
    }

    void Start()
    {
        if (sceneController.alreadyStarted) return;
        sceneController.alreadyStarted = true;

        SceneManager.sceneLoaded += sceneController.prepareScene;

        master.sun = new planet("Sun", new planetData(695700, false, "CSVS/NEW/PLANETS/Sol", 0.0416666665, planetType.planet), 
            new representationData(
                "Prefabs/Planet",
                "Materials/default"));

        //jsonParser.deserialize(Path.Combine(Application.streamingAssetsPath, "sytEarth.syt"), jsonType.system);

        //jsonParser.deserialize(Path.Combine(Application.streamingAssetsPath, "sytAll.syt"), jsonType.system);
        //master.setReferenceFrame(master.allPlanets.First(x => x.name == "Earth"));

        onlyEarth();
        //kepler();

        csvParser.loadScheduling("CSVS/SCHEDULING/July 2021 NSN DTE Schedule");

        //planetTerrain pt = loadTerrain();

        master.pause = false;
        general.camera = Camera.main;

        startMainLoop();
    }

    public void startMainLoop(bool force = false) {
        if (loop != null && force == false) return;

        loop = StartCoroutine(general.internalClock(7200, int.MaxValue, (tick) => {
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
        }, null));
    }

    public void Update()
    {
        if (planetOverview.usePlanetOverview)
        {
            if (Input.GetKey("d")) planetOverview.rotationalOffset -= 90f * UnityEngine.Time.deltaTime * Mathf.Deg2Rad;
            if (Input.GetKey("a")) planetOverview.rotationalOffset += 90f * UnityEngine.Time.deltaTime * Mathf.Deg2Rad;

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
        } else {
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
        }

        if (Input.GetKeyDown("e")) {
            master.requestScaleUpdate();
            planetFocus.enable(!planetFocus.usePlanetFocus);
            master.clearAllLines();
        }

        if (Input.GetKeyDown("1")) speed = 0.1;
        if (Input.GetKeyDown("2")) speed = 0.01;
        if (Input.GetKeyDown("3")) speed = 0.00005;

        if (Input.GetKeyDown("4")) master.time.addJulianTime(2459396.5 - master.time.julian);
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
        
        earth =       new planet(  "Earth", new planetData(  6371, true, "CSVS/PLANETS/Earth", oneHour, planetType.planet), erd);
        planet moon = new planet(   "Luna", new planetData(1738.1, false,    "CSVS/PLANETS/Luna", oneHour, planetType.moon),   rd);
                      new planet("Mercury", new planetData(2439.7, false, "CSVS/PLANETS/Mercury", oneHour, planetType.planet), rd);
                      new planet(  "Venus", new planetData(6051.8, false,   "CSVS/PLANETS/Venus", oneHour, planetType.planet), rd);
                      new planet(   "Mars", new planetData(3396.2, false,    "CSVS/PLANETS/Mars", oneHour, planetType.planet), rd);
                      new planet("Jupiter", new planetData( 71492, false, "CSVS/PLANETS/Jupiter", oneHour, planetType.planet), rd);
                      new planet( "Saturn", new planetData( 60268, false,  "CSVS/PLANETS/Saturn", oneHour, planetType.planet), rd);
                      new planet( "Uranus", new planetData( 25559, false,  "CSVS/PLANETS/Uranus", oneHour, planetType.planet), rd);
                      new planet("Neptune", new planetData( 24764, false, "CSVS/PLANETS/Neptune", oneHour, planetType.planet), rd);

        foreach (string sat in sats)
        {
            satellite s = new satellite(sat, new satelliteData($"CSVS/SATS/{sat}", oneMin), srd);
            try 
            {
                // desync between planet and satellites
                
                //satellite.addFamilyNode(earth, s);
            }
            catch {UnityEngine.Debug.Log($"Unable to load {sat}");}
        }

        foreach (facilityData fd in csvParser.loadFacilites("CSVS/FACILITIES/stationList")) new facility(fd.name, earth, fd, frd);

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
            "Materials/earthLatLonTest");

        double oneHour = 0.0416666665;
        
        earth = new planet("Earth", new planetData(  6371,  true, "CSVS/PLANETS/Earth", oneHour, planetType.planet), erd);
        planet moon =        new planet( "Luna", new planetData(1738.1, false,  "CSVS/PLANETS/Luna", oneHour,   planetType.moon),  rd);
        
        foreach (facilityData fd in csvParser.loadFacilites("CSVS/FACILITIES/stationList")) new facility(fd.name, earth, fd, frd);

        master.setReferenceFrame(earth);

        foreach (KeyValuePair<string, Timeline> kvp in csvParser.loadRpt(Path.Combine(Application.streamingAssetsPath, "CM_ORB.rpt")))
        {
            satellite s = new satellite(kvp.Key, new satelliteData(kvp.Value), srd);
            if (kvp.Key == "LRO") satellite.addFamilyNode(moon, s);
            else satellite.addFamilyNode(earth, s);
        }

        satellite s1 = new satellite("Aura 2", new satelliteData("CSVS/EARTHBASED/AURA", 0.0006944444), srd);
        satellite s2 = new satellite("Oco-2 2", new satelliteData("CSVS/EARTHBASED/OCO-2", 0.0006944444), srd);
        satellite s3 = new satellite("Aqua 2", new satelliteData("CSVS/EARTHBASED/AQUA", 0.0006944444), srd);
        satellite s4 = new satellite("Aim 2", new satelliteData("CSVS/EARTHBASED/AIM", 0.0006944444), srd);
        satellite s5 = new satellite("LRO 2", new satelliteData("CSVS/EARTHBASED/LRO", 0.0006944444 * 5.0), srd);

        new satellite("M1", new satelliteData($"CSVS/SATS/MMS 1", 0.0006944444), srd);
        new satellite("M2", new satelliteData($"CSVS/SATS/MMS 2", 0.0006944444), srd);
        new satellite("M3", new satelliteData($"CSVS/SATS/MMS 3", 0.0006944444), srd);
        new satellite("M4", new satelliteData($"CSVS/SATS/MMS 4", 0.0006944444), srd);

        satellite.addFamilyNode(earth, s1);
        satellite.addFamilyNode(earth, s2);
        satellite.addFamilyNode(earth, s3);
        satellite.addFamilyNode(earth, s4);
        satellite.addFamilyNode(moon, s5);
    }
}

//hello, it's me