using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.Networking;
using B83.MeshTools;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

public class controller : MonoBehaviour
{
    public static planet earth, moon;
    public static planet defaultReferenceFrame;
    public static double speed = 0.00005;
    public static int tickrate = 7200;
    private Vector3 planetFocusMousePosition, planetFocusMousePosition1;
    private Coroutine loop;
    public static bool useTerrainVisibility = false;
    public static controller self;

    private void Awake() {self = this;}

    private void Start() {
        general.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.planetParent = GameObject.FindGameObjectWithTag("planet/parent");
        uiHelper.canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
        general.camera = Camera.main;

        master.sun = new planet("Sun", new planetData(695700, rotationType.none, "CSVS/ARTEMIS 3/PLANETS/sun", 0.0416666665, planetType.planet),
            new representationData(
                "Prefabs/Planet",
                "Materials/planets/sun"));

        loadingController.start(new Dictionary<float, string>() {
            {0, "Generating Planets"},
            {0.10f, "Generating Satellites"},
            {0.75f, "Generating Terrain"}
        });

        StartCoroutine(start());
    }

    private IEnumerator start()
    {
        yield return StartCoroutine(Artemis3());
        defaultReferenceFrame = moon;
        //onlyEarth();

        yield return StartCoroutine(generateTerrain());
        yield return new WaitForSeconds(0.1f);

        master.onScaleChange += (s, e) => {
            if (general.showingTrails) {
                foreach (planet p in master.allPlanets) p.tr.enable();
                trailRenderer.drawAllSatelliteTrails(master.allSatellites);
            }
        };

        //runScheduling();
        //csvParser.loadScheduling("CSVS/SCHEDULING/July 2021 NSN DTE Schedule");

        master.setReferenceFrame(master.allPlanets.First(x => x.name == "Luna"));
        master.pause = false;
        general.camera = Camera.main;

        //runDynamicLink();
        //linkBudgeting.accessCalls("C:/Users/akazemni/Desktop/connections.txt");

        initModes();

        master.markStartOfSimulation();

        loadingController.addPercent(0.26f);
        startMainLoop();

        //Debug.Log(position.J2000(new position(0, 1, 0), new position(0, 0, -1), new position(0, 1, 0)));
    }

    private int lunaRunCount, lunaFinishCount;
    private int poleRunCount, poleFinishCount;
    private IEnumerator generateTerrain() {
        // look i dont want to talk about it ok
        List<string> lunaFiles = new List<string>() {
            "terrainMeshes/luna/5.33333333333333_-30x-135.trn",
            "terrainMeshes/luna/5.33333333333333_-30x-180.trn",
            "terrainMeshes/luna/5.33333333333333_-30x-45.trn",
            "terrainMeshes/luna/5.33333333333333_-30x-90.trn",
            "terrainMeshes/luna/5.33333333333333_-30x0.trn",
            "terrainMeshes/luna/5.33333333333333_-30x135.trn",
            "terrainMeshes/luna/5.33333333333333_-30x45.trn",
            "terrainMeshes/luna/5.33333333333333_-30x90.trn",
            "terrainMeshes/luna/5.33333333333333_-60x-135.trn",
            "terrainMeshes/luna/5.33333333333333_-60x-180.trn",
            "terrainMeshes/luna/5.33333333333333_-60x-45.trn",
            "terrainMeshes/luna/5.33333333333333_-60x-90.trn",
            "terrainMeshes/luna/5.33333333333333_-60x0.trn",
            "terrainMeshes/luna/5.33333333333333_-60x135.trn",
            "terrainMeshes/luna/5.33333333333333_-60x45.trn",
            "terrainMeshes/luna/5.33333333333333_-60x90.trn",
            "terrainMeshes/luna/5.33333333333333_0x-135.trn",
            "terrainMeshes/luna/5.33333333333333_0x-180.trn",
            "terrainMeshes/luna/5.33333333333333_0x-45.trn",
            "terrainMeshes/luna/5.33333333333333_0x-90.trn",
            "terrainMeshes/luna/5.33333333333333_0x0.trn",
            "terrainMeshes/luna/5.33333333333333_0x135.trn",
            "terrainMeshes/luna/5.33333333333333_0x45.trn",
            "terrainMeshes/luna/5.33333333333333_0x90.trn",
            "terrainMeshes/luna/5.33333333333333_30x-135.trn",
            "terrainMeshes/luna/5.33333333333333_30x-180.trn",
            "terrainMeshes/luna/5.33333333333333_30x-45.trn",
            "terrainMeshes/luna/5.33333333333333_30x-90.trn",
            "terrainMeshes/luna/5.33333333333333_30x0.trn",
            "terrainMeshes/luna/5.33333333333333_30x135.trn",
            "terrainMeshes/luna/5.33333333333333_30x45.trn",
            "terrainMeshes/luna/5.33333333333333_30x90.trn"};

        List<string> poleFiles = new List<string>() {
            "terrainMeshes/pole/10810x10810_3063x3063.trn",
            "terrainMeshes/pole/10810x10_3063x3600.trn",
            "terrainMeshes/pole/10810x13873_3063x3063.trn",
            "terrainMeshes/pole/10810x16936_3063x3063.trn",
            "terrainMeshes/pole/10810x19999_3063x3063.trn",
            "terrainMeshes/pole/10810x23062_3063x3063.trn",
            "terrainMeshes/pole/10810x26125_3063x3064.trn",
            "terrainMeshes/pole/10810x29189_3063x3600.trn",
            "terrainMeshes/pole/10810x32789_3063x3600.trn",
            "terrainMeshes/pole/10810x3610_3063x3600.trn",
            "terrainMeshes/pole/10810x36389_3063x3600.trn",
            "terrainMeshes/pole/10810x7210_3063x3600.trn",
            "terrainMeshes/pole/10x10810_3600x3063.trn",
            "terrainMeshes/pole/10x13873_3600x3063.trn",
            "terrainMeshes/pole/10x16936_3600x3063.trn",
            "terrainMeshes/pole/10x19999_3600x3063.trn",
            "terrainMeshes/pole/10x23062_3600x3063.trn",
            "terrainMeshes/pole/10x26125_3600x3064.trn",
            "terrainMeshes/pole/13873x10810_3063x3063.trn",
            "terrainMeshes/pole/13873x10_3063x3600.trn",
            "terrainMeshes/pole/13873x13873_3063x3063.trn",
            "terrainMeshes/pole/13873x16936_3063x3063.trn",
            "terrainMeshes/pole/13873x19999_3063x3063.trn",
            "terrainMeshes/pole/13873x23062_3063x3063.trn",
            "terrainMeshes/pole/13873x26125_3063x3064.trn",
            "terrainMeshes/pole/13873x29189_3063x3600.trn",
            "terrainMeshes/pole/13873x32789_3063x3600.trn",
            "terrainMeshes/pole/13873x3610_3063x3600.trn",
            "terrainMeshes/pole/13873x36389_3063x3600.trn",
            "terrainMeshes/pole/13873x7210_3063x3600.trn",
            "terrainMeshes/pole/16936x10810_3063x3063.trn",
            "terrainMeshes/pole/16936x10_3063x3600.trn",
            "terrainMeshes/pole/16936x13873_3063x3063.trn",
            "terrainMeshes/pole/16936x16936_3063x3063.trn",
            "terrainMeshes/pole/16936x19999_3063x3063.trn",
            "terrainMeshes/pole/16936x23062_3063x3063.trn",
            "terrainMeshes/pole/16936x26125_3063x3064.trn",
            "terrainMeshes/pole/16936x29189_3063x3600.trn",
            "terrainMeshes/pole/16936x32789_3063x3600.trn",
            "terrainMeshes/pole/16936x3610_3063x3600.trn",
            "terrainMeshes/pole/16936x36389_3063x3600.trn",
            "terrainMeshes/pole/16936x7210_3063x3600.trn",
            "terrainMeshes/pole/19999x10810_3063x3063.trn",
            "terrainMeshes/pole/19999x10_3063x3600.trn",
            "terrainMeshes/pole/19999x13873_3063x3063.trn",
            "terrainMeshes/pole/19999x16936_3063x3063.trn",
            "terrainMeshes/pole/19999x19999_3063x3063.trn",
            "terrainMeshes/pole/19999x23062_3063x3063.trn",
            "terrainMeshes/pole/19999x26125_3063x3064.trn",
            "terrainMeshes/pole/19999x29189_3063x3600.trn",
            "terrainMeshes/pole/19999x32789_3063x3600.trn",
            "terrainMeshes/pole/19999x3610_3063x3600.trn",
            "terrainMeshes/pole/19999x36389_3063x3600.trn",
            "terrainMeshes/pole/19999x7210_3063x3600.trn",
            "terrainMeshes/pole/23062x10810_3063x3063.trn",
            "terrainMeshes/pole/23062x10_3063x3600.trn",
            "terrainMeshes/pole/23062x13873_3063x3063.trn",
            "terrainMeshes/pole/23062x16936_3063x3063.trn",
            "terrainMeshes/pole/23062x19999_3063x3063.trn",
            "terrainMeshes/pole/23062x23062_3063x3063.trn",
            "terrainMeshes/pole/23062x26125_3063x3064.trn",
            "terrainMeshes/pole/23062x29189_3063x3600.trn",
            "terrainMeshes/pole/23062x32789_3063x3600.trn",
            "terrainMeshes/pole/23062x3610_3063x3600.trn",
            "terrainMeshes/pole/23062x36389_3063x3600.trn",
            "terrainMeshes/pole/23062x7210_3063x3600.trn",
            "terrainMeshes/pole/26125x10810_3064x3063.trn",
            "terrainMeshes/pole/26125x10_3064x3600.trn",
            "terrainMeshes/pole/26125x13873_3064x3063.trn",
            "terrainMeshes/pole/26125x16936_3064x3063.trn",
            "terrainMeshes/pole/26125x19999_3064x3063.trn",
            "terrainMeshes/pole/26125x23062_3064x3063.trn",
            "terrainMeshes/pole/26125x26125_3064x3064.trn",
            "terrainMeshes/pole/26125x29189_3064x3600.trn",
            "terrainMeshes/pole/26125x32789_3064x3600.trn",
            "terrainMeshes/pole/26125x3610_3064x3600.trn",
            "terrainMeshes/pole/26125x36389_3064x3600.trn",
            "terrainMeshes/pole/26125x7210_3064x3600.trn",
            "terrainMeshes/pole/29189x10810_3600x3063.trn",
            "terrainMeshes/pole/29189x13873_3600x3063.trn",
            "terrainMeshes/pole/29189x16936_3600x3063.trn",
            "terrainMeshes/pole/29189x19999_3600x3063.trn",
            "terrainMeshes/pole/29189x23062_3600x3063.trn",
            "terrainMeshes/pole/29189x26125_3600x3064.trn",
            "terrainMeshes/pole/29189x29189_3600x3600.trn",
            "terrainMeshes/pole/29189x32789_3600x3600.trn",
            "terrainMeshes/pole/29189x3610_3600x3600.trn",
            "terrainMeshes/pole/29189x7210_3600x3600.trn",
            "terrainMeshes/pole/32789x10810_3600x3063.trn",
            "terrainMeshes/pole/32789x13873_3600x3063.trn",
            "terrainMeshes/pole/32789x16936_3600x3063.trn",
            "terrainMeshes/pole/32789x19999_3600x3063.trn",
            "terrainMeshes/pole/32789x23062_3600x3063.trn",
            "terrainMeshes/pole/32789x26125_3600x3064.trn",
            "terrainMeshes/pole/32789x29189_3600x3600.trn",
            "terrainMeshes/pole/32789x7210_3600x3600.trn",
            "terrainMeshes/pole/3610x10810_3600x3063.trn",
            "terrainMeshes/pole/3610x13873_3600x3063.trn",
            "terrainMeshes/pole/3610x16936_3600x3063.trn",
            "terrainMeshes/pole/3610x19999_3600x3063.trn",
            "terrainMeshes/pole/3610x23062_3600x3063.trn",
            "terrainMeshes/pole/3610x26125_3600x3064.trn",
            "terrainMeshes/pole/3610x29189_3600x3600.trn",
            "terrainMeshes/pole/3610x7210_3600x3600.trn",
            "terrainMeshes/pole/36389x10810_3600x3063.trn",
            "terrainMeshes/pole/36389x13873_3600x3063.trn",
            "terrainMeshes/pole/36389x16936_3600x3063.trn",
            "terrainMeshes/pole/36389x19999_3600x3063.trn",
            "terrainMeshes/pole/36389x23062_3600x3063.trn",
            "terrainMeshes/pole/36389x26125_3600x3064.trn",
            "terrainMeshes/pole/7210x10810_3600x3063.trn",
            "terrainMeshes/pole/7210x13873_3600x3063.trn",
            "terrainMeshes/pole/7210x16936_3600x3063.trn",
            "terrainMeshes/pole/7210x19999_3600x3063.trn",
            "terrainMeshes/pole/7210x23062_3600x3063.trn",
            "terrainMeshes/pole/7210x26125_3600x3064.trn",
            "terrainMeshes/pole/7210x29189_3600x3600.trn",
            "terrainMeshes/pole/7210x32789_3600x3600.trn",
            "terrainMeshes/pole/7210x3610_3600x3600.trn",
            "terrainMeshes/pole/7210x7210_3600x3600.trn",
        };

        using (UnityWebRequest uwr = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, "resInfo.txt"))) {
            yield return uwr.SendWebRequest();
            general.pt = new planetTerrain(moon, "Materials/planets/moon/moon", 1737.4, 1);
            general.pt.generateFolderInfos(uwr.downloadHandler.text);
        }

        loadingController.addPercent(0.15f);

        general.plt = new poleTerrain(moon.representation.gameObject.transform);

        foreach (string file in lunaFiles) {
            lunaRunCount++;
            StartCoroutine(downloadLunaMesh(file));
        }

        foreach (string file in poleFiles) {
            poleRunCount++;
            StartCoroutine(downloadPoleMesh(file));
        }

        yield return new WaitUntil(() => lunaRunCount == lunaFinishCount && poleRunCount == poleFinishCount);
        loadingController.addPercent(0.1f);
    }

    private void initModes() { // i dont wanna hear it
        planetFocus.enable(true);
        planetFocus.enable(false);
        planetOverview.enable(true);
        planetOverview.enable(false);
        uiMap.map.toggle(true);
        uiMap.map.toggle(false);
    }

    public static void runWindows()
    {
        master.time.addJulianTime((double)2460806.5 - (double)master.time.julian);
        master.requestPositionUpdate();
        dynamicLinkOptions options = new dynamicLinkOptions();
        options.callback = (data) => {
            windows.jsonWindows(data);
        };
        options.debug = true;
        options.blocking = false;
        options.outputPath = "data.txt";

        useTerrainVisibility = true;

        List<object> users = new List<object>();
        List<object> providers = new List<object>();

        foreach (var u in linkBudgeting.users)
        {
            if (u.Value.Item1) users.Add(master.allFacilites.Find(x => x.name == u.Key));
            else users.Add(master.allSatellites.Find(x => x.name == u.Key));
        }

        foreach (var p in linkBudgeting.providers)
        {
            if (p.Value.Item1) providers.Add(master.allFacilites.Find(x => x.name == p.Key));
            else providers.Add(master.allSatellites.Find(x => x.name == p.Key));
        }

        visibility.raycastTerrain(users, providers, master.time.julian, master.time.julian + 5, speed, options, false);
    }

    public static void runDynamicLink() {
        master.time.addJulianTime((double)2460806.5 - (double)master.time.julian);
        master.requestPositionUpdate();
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
        options.blocking = true;
        options.outputPath = "data.txt";

        useTerrainVisibility = true;

        List<object> users = new List<object>();
        List<object> providers = new List<object>();

        foreach (var u in linkBudgeting.users)
        {
            if (u.Value.Item1) users.Add(master.allFacilites.Find(x => x.name == u.Key));
            else users.Add(master.allSatellites.Find(x => x.name == u.Key));
        }

        foreach (var p in linkBudgeting.providers)
        {
            if (p.Value.Item1) providers.Add(master.allFacilites.Find(x => x.name == p.Key));
            else providers.Add(master.allSatellites.Find(x => x.name == p.Key));
        }

        visibility.raycastTerrain(providers, users, master.time.julian, master.time.julian + 1, speed, options, true);
    }

    private IEnumerator downloadLunaMesh(string file) {
        using (UnityWebRequest uwr = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, file))) {
            yield return uwr.SendWebRequest();
            Mesh m = MeshSerializer.DeserializeMesh(uwr.downloadHandler.data);
            string[] name = Path.GetFileNameWithoutExtension(file).Split('_').Last().Split('x');
            geographic g = new geographic(Double.Parse(name[0]), Double.Parse(name[1]));

            general.pt.registerMesh(g, m);
        }
        lunaFinishCount++;
    }

    private IEnumerator downloadPoleMesh(string file) {
        using (UnityWebRequest uwr = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, file))) {
            yield return uwr.SendWebRequest();
            Mesh m = MeshSerializer.DeserializeMesh(uwr.downloadHandler.data);

            general.plt.registerMesh(m);
        }
        poleFinishCount++;
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

            if (Input.GetKeyDown("t")) {
                planetFocus.togglePoleFocus(!planetFocus.usePoleFocus);
                if (planetFocus.usePoleFocus) general.plt.generate();
                else general.plt.clear();
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

    private IEnumerator Artemis3()
    {
        List<satellite> moonSats =  new List<satellite>();
        List<satellite> earthSats =  new List<satellite>();
        List<facility> moonFacilities = new List<facility>();
        List<facility> earthFacilities = new List<facility>();

        representationData frd = new representationData(
            "Prefabs/Facility",
            "Materials/default");

        double oneMin = 0.0006944444;
        double oneHour = 0.0416666667;
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

        Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>();
        using (UnityWebRequest uwr = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, "Artemis_III.json"))) {
            yield return uwr.SendWebRequest();
            data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(uwr.downloadHandler.text);
        }

        float percentIncrease = 0.5f / (float) data.Count;

        Dictionary<string, Dictionary<string, object>> dbSatellites = new Dictionary<string, Dictionary<string, object>>();
        foreach (var kvp in data) {
            yield return new WaitForSeconds(0.1f);
            loadingController.addPercent(percentIncrease);

            string Name = (string) kvp.Key;
            Dictionary<string, object> dict = kvp.Value;
            if (!dict.ContainsKey("Type")) continue;
            string Type = (string) dict["Type"];
            string user_provider = (dict.ContainsKey("user_provider")) ? (string) dict["user_provider"] : null;
            string CentralBody = (dict.ContainsKey("CentralBody")) ? (string) dict["CentralBody"] : null;

            dbSatellites[kvp.Key] = kvp.Value;

            representationData srd = new representationData("Prefabs/Satellite", "Materials/default");

            if (Type == "Satellite")
            {
                double TimeInterval_start = dict["TimeInterval_start"] is string ? Double.Parse((string) dict["TimeInterval_start"], System.Globalization.NumberStyles.Any) : Convert.ToDouble(dict["TimeInterval_start"]);
                double TimeInterval_stop = dict["TimeInterval_stop"] is string ? Double.Parse((string) dict["TimeInterval_stop"], System.Globalization.NumberStyles.Any) : Convert.ToDouble(dict["TimeInterval_stop"]);
                if (user_provider == "user/provider" || user_provider == "user") linkBudgeting.users.Add(Name, (false, 2460806.5 + TimeInterval_start, 2460806.5 + TimeInterval_stop));
                if (user_provider == "provider") linkBudgeting.providers.Add(Name, (false, 2460806.5 + TimeInterval_start, 2460806.5 + TimeInterval_stop));

                satellite sat = null;
                if (dict.ContainsKey("RAAN")) {
                    double RAAN = Convert.ToDouble(dict["RAAN"]);
                    string OrbitEpoch = (string) dict["OrbitEpoch"];
                    double SemimajorAxis = Convert.ToDouble(dict["SemimajorAxis"]);
                    double Eccentricity = Convert.ToDouble(dict["Eccentricity"]);
                    double Inclination = Convert.ToDouble(dict["Inclination"]);
                    double Arg_of_Perigee = Convert.ToDouble(dict["Arg_of_Perigee"]);
                    double MeanAnomaly = Convert.ToDouble(dict["MeanAnomaly"]);
                    sat = new satellite(Name, new satelliteData(new Timeline(SemimajorAxis / 1000.0, Eccentricity, Inclination, Arg_of_Perigee, RAAN, MeanAnomaly, 1, Time.strDateToJulian(OrbitEpoch), MoonMu)), srd);
                }
                else if (dict.ContainsKey("FilePath")) sat = new satellite(Name, new satelliteData($"CSVS/ARTEMIS 3/SATS/{Name}", oneMin), srd);
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

                windows.dbInfo[Name] = dict;
            }
            else if (Type == "Facility")
            {
                double Lat = Convert.ToDouble(dict["Lat"]);
                double Long = Convert.ToDouble(dict["Long"]);

                if (CentralBody == "Moon")
                {
                    double TimeInterval_start = dict["TimeInterval_start"] is string ? Double.Parse((string) dict["TimeInterval_start"], System.Globalization.NumberStyles.Any) : Convert.ToDouble(dict["TimeInterval_start"]);
                    double TimeInterval_stop = dict["TimeInterval_stop"] is string ? Double.Parse((string) dict["TimeInterval_stop"], System.Globalization.NumberStyles.Any) : Convert.ToDouble(dict["TimeInterval_stop"]);
                    string Service_Period = (string) dict["Service_Period"];
                    double Schedule_Priority = Convert.ToDouble(dict["Schedule_Priority"]);
                    double Service_Level = Convert.ToDouble(dict["Service_Level"]);
                    List<antennaData> antenna = new List<antennaData>() { new antennaData(Name, Name, new geographic(Lat, Long), Schedule_Priority, Service_Level, Service_Period) };
                    facility fd = new facility(Name, moon, new facilityData(Name, new geographic(Lat, Long), 0, antenna, new Time(2460806.5 + TimeInterval_start), new Time(2460806.5 + TimeInterval_stop)), frd);

                    if (user_provider == "user") linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "provider") linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "user/provider")
                    {
                        linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    }

                    moonFacilities.Add(fd);
                }
                else
                {
                    double Ground_Priority = Convert.ToDouble(dict["Ground_Priority"]);
                    List<antennaData> antenna = new List<antennaData>() { new antennaData(Name, Name, new geographic(Lat, Long), Ground_Priority) };
                    facility fd = new facility(Name, earth, new facilityData(Name, new geographic(Lat, Long), 0, antenna), frd);

                    if (user_provider == "user") linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "provider") linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    if (user_provider == "user/provider")
                    {
                        linkBudgeting.users.Add(Name, (true, 2460806.5, 2460836.5));
                        linkBudgeting.providers.Add(Name, (true, 2460806.5, 2460836.5));
                    }

                    earthFacilities.Add(fd);
                }

                windows.dbInfo[Name] = dict;
            }
        }

        planet.addFamilyNode(earth, moon);

        master.setReferenceFrame(moon);
        master.relationshipPlanet[earth] = new List<planet>() { moon };
        master.relationshipSatellite[moon] = moonSats;
        master.relationshipSatellite[earth] = earthSats;
        master.relationshipFacility[moon] = moonFacilities;
        master.relationshipFacility[earth] = earthFacilities;

        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/PLANETS/moon", oneMin));
        master.rod.Add(csvParser.loadPlanetCsv("CSVS/ARTEMIS 3/SATS/v", 0.0006944444));

        foreach (satellite s in master.allSatellites) s.representation.setRelationshipParent();

        loadingController.addPercent(0.1f);
        yield return null;
    }
}
