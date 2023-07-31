using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;

public sealed class planetOverview : IMode {
    public float rotationalOffset = 0, displayScale = 12;
    private float lastRotationalOffset = 0;
    private double maxDist = 0;
    public planet focus = master.sun;
    public List<planet> obeyingPlanets = new List<planet>();
    public List<satellite> obeyingSatellites = new List<satellite>();
    private Button back;
    private Toggle toggleSat, toggleMoon, toggleLines;
    private GameObject disclaimer;

    private Dictionary<string, lineController> axes = new Dictionary<string, lineController>() {
        {"+x", null},
        {"-x", null},
        {"+y", null},
        {"-y", null},
        {"+z", null},
        {"-z", null}};
    private Dictionary<body, lineController> bodies = new Dictionary<body, lineController>();

    private Dictionary<string, Vector3> directions = new Dictionary<string, Vector3>() {
        {"+x", new Vector3(8, 0, 0)},
        {"-x", new Vector3(-8, 0, 0)},
        {"+y", new Vector3(0, 0, 8)},
        {"-y", new Vector3(0, 0, -8)},
        {"+z", new Vector3(0, 4, 0)},
        {"-z", new Vector3(0, -4, 0)}};
    private Dictionary<string, Color> colors = new Dictionary<string, Color>() {
        {"+x", new Color(1, 0, 0, 0.25f)},
        {"-x", new Color(1, 0, 0, 0.25f)},
        {"+y", new Color(0, 1, 0, 0.25f)},
        {"-y", new Color(0, 1, 0, 0.25f)},
        {"+z", new Color(0, 0, 1, 0.25f)},
        {"-z", new Color(0, 0, 1, 0.25f)}};

    private void calculateDefaultMaxDist() {
        foreach (planet p in master.allPlanets) {
            if (p.pType != planetType.planet) continue;
            
            double dist = p.requestPosition(master.time).length();
            if (maxDist < dist) maxDist = dist;
        }
    }

    protected override IModeParameters modePara => new planetOverviewParameters();

    protected override bool enable() {
        calculateDefaultMaxDist();
        general.camera.transform.LookAt(Vector3.zero);
        rotationalOffset = 0;

        planetOverviewUI parent = GameObject.FindGameObjectWithTag("ui/planetOverview/parent").GetComponent<planetOverviewUI>();

        back = parent.back;
        toggleSat = parent.toggleSat;
        toggleMoon = parent.toggleMoon;
        toggleLines = parent.toggleLine;
        disclaimer = parent.disclaimer;

        toggleSat.onValueChanged.AddListener(satCallback);
        toggleMoon.onValueChanged.AddListener(moonCallback);
        toggleLines.onValueChanged.AddListener(lineCallback);
        back.onClick.AddListener(backCallback);

        disclaimer.SetActive(true);

        addDefaultObey();
        drawAxes();

        toggleLines.gameObject.SetActive(true);

        return true;
    }

    protected override bool disable() {
        modeParameters.load(new defaultParameters());
        maxDist = 0;
        focus = master.sun;
        obeyingPlanets = new List<planet>();

        disclaimer.SetActive(false);

        clearAxes();

        foreach (facility f in master.allFacilities) f.representation.setActive(true);
        toggleLines.gameObject.SetActive(false);

        return true;
    }

    protected override void callback() {
        obeyingSatellites = new List<satellite>();
        back.gameObject.SetActive(false);
        toggleMoon.gameObject.SetActive(false);
        toggleSat.gameObject.SetActive(false);

        rotationalOffset = 0;
    }

    private planetOverview() {}
    private static readonly Lazy<planetOverview> lazy = new Lazy<planetOverview>(() => new planetOverview());
    public static planetOverview instance => lazy.Value;

    private void drawAxes()
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

        lastRotationalOffset = 0;
        updateAxes(true);
    }

    private void createLineController(body b) {
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

    private void clearAxes() {
        foreach (lineController lc in axes.Values) {if (!ReferenceEquals(lc, null)) lc.clearLine();};
        foreach (lineController lc in bodies.Values) {if (!ReferenceEquals(lc, null)) lc.destroy();};
        bodies = new Dictionary<body, lineController>();

        updateAxes(true);
    }

    public void updateAxes(bool force = false)
    {
        // update planetary lines in accordance to the new planets positions
        foreach (KeyValuePair<body, lineController> kvp in bodies) {
            if (kvp.Key is satellite) {
                satellite s = (satellite) kvp.Key;
                if (!s.positions.exists(master.time)) continue;
            }
            Vector3 start = (Vector3) (planetOverviewPosition(kvp.Key.pos - focus.pos) / master.scale);
            Vector3 end = new Vector3(start.x, 0, start.z);

            List<Vector3> p = null;
            if (kvp.Key is planet && ((planet) kvp.Key).name == "Luna") p = new List<Vector3>() {end, start, start * 100f, start, Vector3.zero};
            else p = new List<Vector3>() {end, start};

            if (toggleLines.isOn) p.Add(Vector3.zero);
            kvp.Value.drawLine(p, kvp.Value.color);
            kvp.Value.rotateAround(general.planetParent.transform.rotation.eulerAngles.y * Mathf.Deg2Rad, 0, 0, Vector3.zero);

            kvp.Value.setWidth(0.05f * general.camera.orthographicSize * 0.2f);
            kvp.Value.gameObject.transform.localScale = new Vector3(
                0.25f * general.camera.orthographicSize * 0.2f,
                0.001f * general.camera.orthographicSize * 0.2f,
                0.25f * general.camera.orthographicSize * 0.2f
            );

            kvp.Value.gameObject.transform.position = kvp.Value.requestPosition(1);
        }

        // rotate
        if (lastRotationalOffset != rotationalOffset || force)
        {
            float rotationalDifference = rotationalOffset - lastRotationalOffset;
            lastRotationalOffset = rotationalOffset;

            // rotate axes
            foreach (lineController lc in axes.Values) {
                lc.rotateAround(rotationalDifference, 0, 0, Vector3.zero);
                lc.setWidth(0.05f * general.camera.orthographicSize * 0.2f);
            }

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
            rotationalOffset = 0;
            focus = target;

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

    public position planetOverviewPosition(position pos)
    {
        if (pos == new position(0, 0, 0)) return new position(0, 0, 0);
        position p = pos.normalize();
        double ratio = pos.length() / ((maxDist + 1) * 1.1);

        double adjustedRatio = 0.25 * Math.Log(ratio + 0.01, controller._logBase) + 0.5;

        return p * adjustedRatio * master.scale * displayScale;     
    }

    private void backCallback() {
        calculateDefaultMaxDist();
        clearAxes();
        focus = master.sun;
        back.gameObject.SetActive(false);
        toggleMoon.gameObject.SetActive(false);
        toggleSat.gameObject.SetActive(false);
        obeyingSatellites = new List<satellite>();

        addDefaultObey();
        drawAxes();

        general.notifyStatusChange();
    }

    private void satCallback(bool value) {
        clearAxes();
        if (!value) {
            foreach (satellite s in obeyingSatellites) s.tr.disable();
            obeyingSatellites = new List<satellite>();
            general.notifyTrailsChange();
        } else {
            obeyingSatellites = master.relationshipSatellite.FirstOrDefault(x => x.Key == focus).Value;
            if (ReferenceEquals(obeyingSatellites, null)) obeyingSatellites = new List<satellite>();
        }

        general.notifyTrailsChange();

        drawAxes();
    }

    private void moonCallback(bool value) {
        clearAxes();
        if (!value) {
            foreach (planet p in obeyingPlanets) p.tr.disable();
            obeyingPlanets = new List<planet>();
            general.notifyTrailsChange();
        } else {
            obeyingPlanets = master.relationshipPlanet.FirstOrDefault(x => x.Key == focus).Value;
            if (ReferenceEquals(obeyingPlanets, null)) obeyingPlanets = new List<planet>();
        }

        obeyingPlanets.Add(focus);

        drawAxes();
    }

    private void lineCallback(bool value) {
        updateAxes(true);
    }

    private void addDefaultObey() {
        obeyingSatellites = new List<satellite>();
        obeyingPlanets = new List<planet>();
        foreach (planet p in master.allPlanets) {
            if (p.pType == planetType.planet) obeyingPlanets.Add(p);
        }
    }

    protected override void loadControls() {
        List<IMode> w = new List<IMode>() {this};

        playerControls.addKey("d", conTrig.held, () => {
            rotationalOffset -= 90f * UnityEngine.Time.deltaTime * Mathf.Deg2Rad;
        }, whitelist: w);

        playerControls.addKey("a", conTrig.held, () => {
            rotationalOffset += 90f * UnityEngine.Time.deltaTime * Mathf.Deg2Rad;
        }, whitelist: w);

        playerControls.addKey("", conTrig.none, () => {
            updateAxes();
        }, whitelist: w);

        playerControls.addKey("", conTrig.none, () => {
            general.camera.orthographicSize -= Input.mouseScrollDelta.y * UnityEngine.Time.deltaTime * 100f * general.camera.orthographicSize;
            general.camera.orthographicSize = Math.Max(0.01f, Math.Min(20, general.camera.orthographicSize));
            updateAxes(true); // force update for changing zoom
        }, precondition: () => Input.mouseScrollDelta.y != 0, whitelist: w);

        playerControls.addKey("", conTrig.none, () => {
            general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, 20f * UnityEngine.Time.deltaTime);
        }, precondition: () => Input.GetKeyDown(KeyCode.UpArrow), whitelist: w);

        playerControls.addKey("", conTrig.none, () => {
            general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, -20f * UnityEngine.Time.deltaTime);
        }, precondition: () => Input.GetKeyDown(KeyCode.DownArrow), whitelist: w);
        
        playerControls.addKey("", conTrig.none, () => {
            general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, 90f - general.camera.transform.eulerAngles.x);
            rotationalOffset = -45f * Mathf.Deg2Rad;
            displayScale = 13.65f;
        }, precondition: () => Input.GetKeyDown(KeyCode.RightArrow), whitelist: w);

        playerControls.addKey("", conTrig.none, () => {
            general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, -general.camera.transform.eulerAngles.x);
            rotationalOffset = Mathf.Deg2Rad * 90f * (float) (rotationalOffset * Mathf.Rad2Deg / 90);
        }, precondition: () => Input.GetKeyDown(KeyCode.LeftArrow), whitelist: w);

        playerControls.addKey("w", conTrig.held, () => {
            displayScale += 5f * UnityEngine.Time.deltaTime;
        }, whitelist: w);

        playerControls.addKey("s", conTrig.held, () => {
            displayScale -= 5f * UnityEngine.Time.deltaTime;
        }, whitelist: w);

        playerControls.addKey("q", conTrig.down, () => {
            modeController.toggle(this);
        });
    }
}
