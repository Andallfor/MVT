using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;

public static class planetOverview
{
    public static bool usePlanetOverview {get {return _upo;}}
    private static bool _upo = false;
    public static double maxDist = 0;
    public static float rotationalOffset = 0;
    private static float lastRotationalOffset = 0;
    public static planet focus = master.sun;
    public static List<planet> obeyingPlanets = new List<planet>();
    public static List<satellite> obeyingSatellites = new List<satellite>();
    public static bool zoomed {get; private set;} = false;
    private static Button back;
    private static Toggle toggleSat, toggleMoon;
    private static GameObject disclaimer;

    private static Dictionary<string, lineController> axes = new Dictionary<string, lineController>() {
        {"+x", null},
        {"-x", null},
        {"+y", null},
        {"-y", null},
        {"+z", null},
        {"-z", null}};
    private static Dictionary<body, lineController> bodies = new Dictionary<body, lineController>();

    private static Dictionary<string, Vector3> directions = new Dictionary<string, Vector3>() {
        {"+x", new Vector3(8, 0, 0)},
        {"-x", new Vector3(-8, 0, 0)},
        {"+y", new Vector3(0, 0, 8)},
        {"-y", new Vector3(0, 0, -8)},
        {"+z", new Vector3(0, 4, 0)},
        {"-z", new Vector3(0, -4, 0)}};
    private static Dictionary<string, Color> colors = new Dictionary<string, Color>() {
        {"+x", Color.red},
        {"-x", Color.red},
        {"+y", Color.green},
        {"-y", Color.green},
        {"+z", Color.blue},
        {"-z", Color.blue}};

    private static void calulcateDefaultMaxDist() {
        foreach (planet p in master.allPlanets) {
            if (p.pType != planetType.planet) continue;
            
            double dist = p.pos.length();
            if (maxDist < dist) maxDist = dist;
        }
    }

    public static void enable(bool use)
    {
        if (use)
        {
            calulcateDefaultMaxDist();

            Camera c = Camera.main;
            c.transform.position = new Vector3(-15, 7.5f, -15);
            c.transform.rotation = Quaternion.Euler(20, 45, 0);
            c.orthographic = true;
            c.orthographicSize = 5;
            obeyingSatellites = new List<satellite>();
            zoomed = false;
            rotationalOffset = 0;

            planetOverviewUI parent = GameObject.FindGameObjectWithTag("ui/planetOverview/parent").GetComponent<planetOverviewUI>();

            back = parent.back;
            toggleSat = parent.toggleSat;
            toggleMoon = parent.toggleMoon;
            disclaimer = parent.disclaimer;

            toggleSat.onValueChanged.AddListener(satCallback);
            toggleMoon.onValueChanged.AddListener(moonCallback);
            back.onClick.AddListener(backCallback);

            back.gameObject.SetActive(false);
            toggleMoon.gameObject.SetActive(false);
            toggleSat.gameObject.SetActive(false);
            disclaimer.SetActive(true);


            addDefaultObey();
            drawAxes();
        } else {
            Camera c = Camera.main;
            c.transform.position = new Vector3(0, 0, -10);
            c.transform.rotation = Quaternion.Euler(0, 0, 0);
            c.orthographic = false;
            c.fieldOfView = 60;
            zoomed = false;
            maxDist = 0;
            focus = master.sun;
            obeyingPlanets = new List<planet>();
            obeyingSatellites = new List<satellite>();
            clearAxes();

            back.gameObject.SetActive(false);
            toggleMoon.gameObject.SetActive(false);
            toggleSat.gameObject.SetActive(false);
            disclaimer.SetActive(false);

            foreach (facility f in master.allFacilites) f.representation.setActive(true);
        }

        rotationalOffset = 0;
        _upo = use;
    }

    private static void drawAxes()
    {
        List<string> keys = axes.Keys.ToList();

        foreach (string key in keys)
        {
            lineController lc = null;
            if (ReferenceEquals(axes[key], null))
            {
                GameObject go = GameObject.Instantiate(Resources.Load("Prefabs/line") as GameObject);
                lc = go.GetComponent<lineController>();
                axes[key] = lc;
            } else lc = axes[key];
            
            lc.drawLine(new List<Vector3>() {Vector3.zero, directions[key]}, colors[key]);
            lc.addCaption(1, key, 20);
        }

        foreach (planet p in obeyingPlanets) createLineController(p);
        foreach (satellite s in obeyingSatellites) createLineController(s);

        updateAxes(true);
    }

    private static void createLineController(body b) {
        lineController lc = null;
        if (!bodies.ContainsKey(b))
        {
            GameObject go = GameObject.Instantiate(Resources.Load("Prefabs/planetOverviewLine") as GameObject);
            lc = go.GetComponent<lineController>();
            bodies[b] = lc;
        } else lc = bodies[b];

        Color c = UnityEngine.Random.ColorHSV();
        lc.setColor(c);

        MeshRenderer mr = lc.gameObject.GetComponent<MeshRenderer>();
        mr.material.color = c;
        mr.enabled = true;
    }

    private static void clearAxes()
    {
        rotationalOffset = 0;

        foreach (lineController lc in axes.Values) lc.clearLine();
        foreach (lineController lc in bodies.Values) lc.destroy();
        bodies = new Dictionary<body, lineController>();

        updateAxes(true);
    }

    public static void updateAxes(bool force = false)
    {
        // update planetary lines in accordance to the new planets positions
        foreach (KeyValuePair<body, lineController> kvp in bodies) {
            if (kvp.Key is satellite) {
                satellite s = (satellite) kvp.Key;
                if (!s.positions.exists(master.time)) continue;
            }
            Vector3 start = (Vector3) (planetOverviewPosition(kvp.Key.pos - focus.pos) / master.scale);
            Vector3 end = new Vector3(start.x, 0, start.z);

            kvp.Value.drawLine(new List<Vector3>() {start, end}, kvp.Value.color);
            kvp.Value.rotateAround(general.planetParent.transform.rotation.eulerAngles.y * Mathf.Deg2Rad, 0, 0, Vector3.zero);

            kvp.Value.gameObject.transform.position = kvp.Value.requestPosition(1);
        }

        // rotate
        if (lastRotationalOffset != rotationalOffset || force)
        {
            float rotationalDifference = rotationalOffset - lastRotationalOffset;
            lastRotationalOffset = rotationalOffset;

            // rotate axes
            foreach (lineController lc in axes.Values) lc.rotateAround(rotationalDifference, 0, 0, Vector3.zero);

            // rotate planets
            general.planetParent.transform.rotation = Quaternion.Euler(0, rotationalOffset * Mathf.Rad2Deg, 0);
        }

        planet target = null;
        RaycastHit hit;
        if (Physics.Raycast(general.camera.ScreenPointToRay(Input.mousePosition), out hit)) {
            if (master.allPlanets.Exists(x => x.name == hit.transform.gameObject.name)) {
                target = master.allPlanets.First(x => x.name == hit.transform.gameObject.name);
            }
        }

        if (target == null) return;

        if (Input.GetMouseButtonDown(0) && target is planet && focus != target) {
            focus = target;
            zoomed = true;

            back.gameObject.SetActive(true);
            toggleMoon.gameObject.SetActive(true);
            toggleSat.gameObject.SetActive(true);

            obeyingPlanets = new List<planet>() {focus};
            obeyingSatellites = new List<satellite>();

            satCallback(true);
            moonCallback(true);

            maxDist = 0;
            foreach (planet p in obeyingPlanets) {        
                double dist = (p.pos - focus.pos).length();
                if (maxDist < dist) maxDist = dist;
            }

            foreach (satellite s in obeyingSatellites) {
                double dist = (s.pos - focus.pos).length();
                if (maxDist < dist) maxDist = dist;
            }

            clearAxes();
            drawAxes();

            general.notifyStatusChange();
        }
    }

    public static position planetOverviewPosition(position pos)
    {
        if (pos == new position(0, 0, 0)) return new position(0, 0, 0);
        position p = pos.normalize();
        double ratio = pos.length() / ((planetOverview.maxDist + 1) * 1.1);

        double adjustedRatio = 0.25 * Math.Log(ratio + 0.01, 10) + 0.5;

        return p * adjustedRatio * master.scale * 12.0;
    }

    public static void backCallback() {
        calulcateDefaultMaxDist();
        clearAxes();
        zoomed = false;
        focus = master.sun;
        back.gameObject.SetActive(false);
        toggleMoon.gameObject.SetActive(false);
        toggleSat.gameObject.SetActive(false);
        obeyingSatellites = new List<satellite>();

        addDefaultObey();
        drawAxes();

        general.notifyStatusChange();
    }

    public static void satCallback(bool value) {
        clearAxes();
        if (!value) obeyingSatellites = new List<satellite>();
        else {
            foreach (KeyValuePair<planet, List<satellite>> relation in master.relationshipSatellite) {
                if (relation.Key == focus) {
                    obeyingSatellites = relation.Value;
                    break;
                }
            }
        }

        drawAxes();
    }

    public static void moonCallback(bool value) {
        clearAxes();
        if (!value) obeyingPlanets = new List<planet>();
        else {
            foreach (KeyValuePair<planet, List<planet>> relation in master.relationshipPlanet) {
                if (relation.Key == focus) {
                    obeyingPlanets = relation.Value;
                    break;
                }
            }
        }

        obeyingPlanets.Add(focus);

        drawAxes();
    }

    public static void addDefaultObey() {
        obeyingSatellites = new List<satellite>();
        obeyingPlanets = new List<planet>();
        foreach (planet p in master.allPlanets) {
            if (p.pType == planetType.planet) obeyingPlanets.Add(p);
        }
    }
}
