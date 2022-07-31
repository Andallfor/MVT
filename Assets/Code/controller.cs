using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.EventSystems;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class controller : MonoBehaviour
{
    public float playerSpeed = 100f * (float) master.scale;
    public static planet earth, moon;
    private double speed = 0.00005;
    private Vector3 planetFocusMousePosition, planetFocusMousePosition1;
    private Coroutine loop;
    private planetTerrain pt;
    private poleTerrain plt;

    void Start()
    {
        general.main = this;
        master.sun = new planet("Sun", new planetData(695700, false, "CSVS/ARTEMIS 3/PLANETS/sun", 0.0416666665, planetType.planet),
            new representationData(
                "Prefabs/Planet",
                "Materials/default"));

        
        webRequest wr = new webRequest();
        wr.download("Artemis_III_May_11_2025.json", lateStart);
    }

    private void lateStart(string data) {
        Artemis4(JsonConvert.DeserializeObject<Dictionary<string, object>[]>(data));

        loadTerrain();
        //plt = loadPoles();

        
    }

    public void startMainLoop(bool force = false) {
        if (loop != null && force == false) return;

        loop = StartCoroutine(general.internalClock(7200, int.MaxValue, (tick) => {
            if (master.pause) {
                master.tickStart(master.time);
                master.time.addJulianTime(0);
            } else {
                Time t = new Time(master.time.julian);
                t.addJulianTime(speed);
                master.tickStart(t);
                master.time.addJulianTime(speed);
            }

            pt.updateTerrain();

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

            if (Input.GetKeyDown("t") && !plt.currentlyDrawing) {
                planetFocus.togglePoleFocus(!planetFocus.usePoleFocus);
                if (planetFocus.usePoleFocus) plt.genMinScale();
                else plt.clear();
            }

            if (planetFocus.usePoleFocus) {
                if (Input.GetKeyDown("=")) plt.increaseScale();
                if (Input.GetKeyDown("-")) plt.decreaseScale();

                planetFocus.focus.representation.forceHide = true;
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
            pt.unload();
            //plt.clear();
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
    private void loadTerrain() {
        // read folder info here
        // meshes are saved into resources, so can just load them without having to use unitywebrequest

        webRequest wr = new webRequest();
        wr.download("resInfo.txt", _loadTerrain);
        

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
    }

    private void _loadTerrain(string data) {
        pt = new planetTerrain(moon, "Materials/planets/moon/moon", 1737.4, 1);
        pt.generateFolderInfos(data);

        master.pause = false;
        general.camera = Camera.main;

        master.markStartOfSimulation();
        
        startMainLoop();
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

        double oneHour = 0.0416666665;

        earth = new planet("Earth", new planetData(6371, true, "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), erd);
        moon = new planet("Luna", new planetData(1738.1, false, "CSVS/ARTEMIS 3/PLANETS/moon", oneHour, planetType.moon), mrd);
        new planet("Mercury", new planetData(2439.7, false, "CSVS/ARTEMIS 3/PLANETS/mercury", oneHour, planetType.planet), rd);
        new planet("Venus", new planetData(6051.8, false, "CSVS/ARTEMIS 3/PLANETS/venus", oneHour, planetType.planet), rd);
        new planet("Mars", new planetData(3396.2, false, "CSVS/ARTEMIS 3/PLANETS/mars", oneHour, planetType.planet), rd);
        new planet("Jupiter", new planetData(71492, false, "CSVS/ARTEMIS 3/PLANETS/jupiter", oneHour, planetType.planet), rd);
        new planet("Saturn", new planetData(60268, false, "CSVS/ARTEMIS 3/PLANETS/saturn", oneHour, planetType.planet), rd);
        new planet("Uranus", new planetData(25559, false, "CSVS/ARTEMIS 3/PLANETS/uranus", oneHour, planetType.planet), rd);
        new planet("Neptune", new planetData(24764, false, "CSVS/ARTEMIS 3/PLANETS/neptune", oneHour, planetType.planet), rd);

        //satellite s1 = new satellite("LCN-1", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 0, 1, 2460628.5283449073, 4902.800066)), srd);
        //satellite s2 = new satellite("LCN-2", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 180, 1, 2460628.5283449073, 4902.800066)), srd);
        //satellite s3 = new satellite("LCN-3", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 360, 1, 2460628.5283449073, 4902.800066)), srd);
//
        //satellite s4 = new satellite("Moonlight-1", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 0, 1, 2460628.5283449073, 4902.800066)), srd);
        //satellite s5 = new satellite("Moonlight-2", new satelliteData(new Timeline(6142.58, 0.6, 51.7, 90, 165, 180, 1, 2460628.5283449073, 4902.800066)), srd);
//
        //satellite s6 = new satellite("CubeSat-1", new satelliteData(new Timeline(5000, 0.51, 74.3589, 90, 356.858, 311.274, 1, 2460615.5, 4902.800066)), srd);
        //satellite s7 = new satellite("CubeSat-2", new satelliteData(new Timeline(1837.4, 0.000000000000000195, 114.359, 0, 356.858, 360, 1, 2460615.5, 4902.800066)), srd);
//
        //satellite s8 = new satellite("HLS-NRHO", new satelliteData("CSVS/ARTEMIS 3/SATS/HLS/HLS-NRHO", oneMin), srd);
        //satellite s9 = new satellite("HLS-Docked", new satelliteData("CSVS/ARTEMIS 3/SATS/HLS/HLS-Docked", oneMin), srd);
        //satellite s10 = new satellite("HLS-Disposal", new satelliteData("CSVS/ARTEMIS 3/SATS/HLS/HLS-Disposal", oneMin), srd);
//
        //satellite s11 = new satellite("Orion-Transit-O", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-Transit-O", oneMin), srd);
        //satellite s12 = new satellite("Orion-Docked", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-Docked", oneMin), srd);
        //satellite s13 = new satellite("Orion-NRHO", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-NRHO", oneMin), srd);
        //satellite s14 = new satellite("Orion-Transit-R", new satelliteData("CSVS/ARTEMIS 3/SATS/ORION/Orion-Transit-R", oneMin), srd);

        //s8.positions.enableExistanceTime(new Time(2460806.5), new Time((2460806.5 + 9.0)));
        //s9.positions.enableExistanceTime(new Time((2460806.5 + 9.0)), new Time((2460806.5 + 13.0)));
        //s10.positions.enableExistanceTime(new Time((2460806.5 + 13.0)), new Time((2460806.5 + 20.29504301)));
//
        //s11.positions.enableExistanceTime(new Time(2460806.5), new Time((2460806.5 + 9.0)));
        //s12.positions.enableExistanceTime(new Time((2460806.5 + 9.0)), new Time((2460806.5 + 13.0)));
        //s13.positions.enableExistanceTime(new Time((2460806.5 + 13.0)), new Time((2460806.5 + 20.29504301)));
        //s14.positions.enableExistanceTime(new Time((2460806.5 + 20.29504301)), new Time((2460806.5 + 30.0)));
//
        //satellite.addFamilyNode(moon, s1);
        //satellite.addFamilyNode(moon, s2);
        //satellite.addFamilyNode(moon, s3);
//
        //satellite.addFamilyNode(moon, s4);
        //satellite.addFamilyNode(moon, s5);
//
        //satellite.addFamilyNode(moon, s6);
        //satellite.addFamilyNode(moon, s7);
//
        //satellite.addFamilyNode(moon, s8);
        //satellite.addFamilyNode(moon, s9);
        //satellite.addFamilyNode(moon, s10);
//
        //satellite.addFamilyNode(moon, s11);
        //satellite.addFamilyNode(moon, s12);
        //satellite.addFamilyNode(moon, s13);
        //satellite.addFamilyNode(moon, s14);
//
        //master.relationshipPlanet.Add(earth, new List<planet>() { moon });
        //master.relationshipSatellite.Add(moon, new List<satellite>() { s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14 });

        new facility("Schickard", moon, new facilityData("Schickard", new geographic(-44.4, -55.1), new List<antennaData>()), frd);
        new facility("Longomontanus", moon, new facilityData("Longomontanus", new geographic(-49.5, -21.7), new List<antennaData>()), frd);
        new facility("Maginus", moon, new facilityData("Maginus", new geographic(-50, -6.2), new List<antennaData>()), frd);
        new facility("Apollo", moon, new facilityData("Apollo", new geographic(-36.1, -151.8), new List<antennaData>()), frd);
        new facility("Mare Crisium", moon, new facilityData("Mare Crisium", new geographic(17, 59.1), new List<antennaData>()), frd);

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

    private void Artemis4(Dictionary<string, object>[] data)
    {
        List<satellite> moonSats = new List<satellite>();
        List<satellite> earthSats = new List<satellite>();

        representationData rd = new representationData(
            "Prefabs/Planet",
            "Materials/default");

        representationData lrd = new representationData(
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
        //double EarthMu = 398600.435436;

        earth = new planet("Earth", new planetData(6371, true, "CSVS/ARTEMIS 3/PLANETS/earth", oneHour, planetType.planet), erd);
        moon = new planet("Luna", new planetData(1738.1, false, "CSVS/ARTEMIS 3/PLANETS/moon", oneMin, planetType.moon), mrd);
        new planet("Mercury", new planetData(2439.7, false, "CSVS/ARTEMIS 3/PLANETS/mercury", oneHour, planetType.planet), rd);
        new planet("Venus", new planetData(6051.8, false, "CSVS/ARTEMIS 3/PLANETS/venus", oneHour, planetType.planet), rd);
        new planet("Mars", new planetData(3396.2, false, "CSVS/ARTEMIS 3/PLANETS/mars", oneHour, planetType.planet), rd);
        new planet("Jupiter", new planetData(71492, false, "CSVS/ARTEMIS 3/PLANETS/jupiter", oneHour, planetType.planet), rd);
        new planet("Saturn", new planetData(60268, false, "CSVS/ARTEMIS 3/PLANETS/saturn", oneHour, planetType.planet), rd);
        new planet("Uranus", new planetData(25559, false, "CSVS/ARTEMIS 3/PLANETS/uranus", oneHour, planetType.planet), rd);
        new planet("Neptune", new planetData(24764, false, "CSVS/ARTEMIS 3/PLANETS/neptune", oneHour, planetType.planet), rd);

        for (int i = 0; i < data.Length; i++)
        {
            Dictionary<string, object> dict = data[i];
            string Name = (string) dict["Name"];
            string Type = (string) dict["Type"];
            string user_provider = (string) dict["user_provider"];
            string CentralBody = (string) dict["CentralBody"];            
            double TimeInterval_start = dict["TimeInterval_start"] is string ? Double.Parse((string) dict["TimeInterval_start"], System.Globalization.NumberStyles.Any) : Convert.ToDouble(dict["TimeInterval_start"]);
            double TimeInterval_stop = dict["TimeInterval_stop"] is string ? Double.Parse((string) dict["TimeInterval_stop"], System.Globalization.NumberStyles.Any) : Convert.ToDouble(dict["TimeInterval_stop"]);

            if (Type == "Satellite")
            {
                if (user_provider == "user/provider" || user_provider == "user") linkBudgeting.users.Add(Name, (false, 2460806.5 + TimeInterval_start, 2460806.5 + TimeInterval_stop));
                if (user_provider == "provider") linkBudgeting.providers.Add(Name, (false, 2460806.5 + TimeInterval_start, 2460806.5 + TimeInterval_stop));

                satellite sat = null;
                if (!ReferenceEquals(dict["RAAN"], null))
                {
                    double RAAN = Convert.ToDouble(dict["RAAN"]);
                    string OrbitEpoch = (string) dict["OrbitEpoch"];
                    double SemimajorAxis = Convert.ToDouble(dict["SemimajorAxis"]);
                    double Eccentricity = Convert.ToDouble(dict["Eccentricity"]);
                    double Inclination = Convert.ToDouble(dict["Inclination"]);
                    double Arg_of_Perigee = Convert.ToDouble(dict["Arg_of_Perigee"]);
                    double MeanAnomaly = Convert.ToDouble(dict["MeanAnomaly"]);
                    sat = new satellite(Name, new satelliteData(new Timeline(SemimajorAxis / 1000, Eccentricity, Inclination, Arg_of_Perigee, RAAN, MeanAnomaly, 1, Time.strDateToJulian(OrbitEpoch), MoonMu)), srd);
                }
                else if (!ReferenceEquals(dict["FilePath"], null))
                {
                    sat = new satellite(Name, new satelliteData($"CSVS/ARTEMIS 3/SATS/{Name}", oneMin), srd);
                }
                sat.positions.enableExistanceTime(new Time(2460806.5 + TimeInterval_start), new Time((2460806.5 + TimeInterval_stop)));

                if (CentralBody == "Moon")
                {
                    satellite.addFamilyNode(moon, sat);
                    moonSats.Add(sat);
                }
                else if (CentralBody == "Earth")
                {
                    satellite.addFamilyNode(earth, sat);
                    earthSats.Add(sat);
                }
            }
            else if (Type == "Facility")
            {
                double Lat = Convert.ToDouble(dict["Lat"]);
                double Long = Convert.ToDouble(dict["Long"]);

                if (CentralBody == "Moon")
                {
                    string Service_Period = (string) dict["Service_Period"];
                    double Schedule_Priority = Convert.ToDouble(dict["Schedule_Priority"]);
                    double Service_Level = Convert.ToDouble(dict["Service_Level"]);
                    List<antennaData> antenna = new List<antennaData>() { new antennaData(Name, Name, new geographic(Lat, Long), Schedule_Priority, Service_Level, Service_Period) };
                    facility fd = new facility(Name, moon, new facilityData(Name, new geographic(Lat, Long), antenna, new Time(2460806.5 + TimeInterval_start), new Time(2460806.5 + TimeInterval_stop)), frd);

                    if (user_provider == "user") linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "provider") linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "user/provider")
                    {
                        linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    }
                }
                else
                {
                    double Ground_Priority = Convert.ToDouble(dict["Ground_Priority"]);
                    List<antennaData> antenna = new List<antennaData>() { new antennaData(Name, Name, new geographic(Lat, Long), Ground_Priority) };
                    facility fd = new facility(Name, earth, new facilityData(Name, new geographic(Lat, Long), antenna), frd);

                    if (user_provider == "user") linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "provider") linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "user/provider")
                    {
                        linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    }
                }
            }
        }

        master.setReferenceFrame(moon);
        master.relationshipPlanet[earth] = new List<planet>() { moon };
        master.relationshipSatellite[moon] = moonSats;
        master.relationshipSatellite[earth] = earthSats;

        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/PLANETS/moon", oneMin));
        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/SATS/v", 0.0006944444));
    }
}

//hello, it's me
// wow, it's you
