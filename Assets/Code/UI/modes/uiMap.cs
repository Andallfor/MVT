using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;

public sealed class uiMap : IMode {
    public planet parent;
    private double lastTime;
    private Texture texture;
    private GameObject surface, uilrPrefab, transformParent, satelliteParent;
    private Vector2 size;
    private Dictionary<body, RectTransform> bodies = new Dictionary<body, RectTransform>();
    private List<GameObject> trailsGo = new List<GameObject>();
    private List<RectTransform> Facilities = new List<RectTransform>();
    private List<GameObject> markers = new List<GameObject>();
    private Dictionary<body, Vector2[]> trails = new Dictionary<body, Vector2[]>();
    private List<Color32> colors = new List<Color32>() {
        new Color32(255, 102, 0, 75),
        new Color32(255, 204, 0, 75),
        new Color32(78, 255, 0, 75),
        new Color32(0, 179, 255, 75)
    };

    protected override IModeParameters modePara => new uiModeParameters();

    private Dictionary<string, int> offsetKey = new Dictionary<string, int>() {
        {"LCN-1", 0},
        {"LCN-2", 0},
        {"LCN-3", 0},
        {"CubeSat-1", 2},
        {"CubeSat-2", 2},
    };

    protected override void _initialize() {
        for (int i = 1; i < 9; i++) markers.Add(Resources.Load($"Prefabs/markers/marker{i}") as GameObject);

        uilrPrefab = resLoader.load<GameObject>("uiLine");

        surface = GameObject.FindGameObjectWithTag("ui/map/surface");
        size = new Vector2(surface.GetComponent<RectTransform>().rect.width, surface.GetComponent<RectTransform>().rect.height);
        transformParent = GameObject.FindGameObjectWithTag("ui/map/trails");
        satelliteParent = GameObject.FindGameObjectWithTag("ui/map/surface/satellites");

        base._initialize();
    }

    protected override bool enable() {
        if (!(master.requestReferenceFrame() is planet)) return false;
        parent = master.requestReferenceFrame() as planet;

        surface.GetComponent<RawImage>().enabled = true;

        // NOTE these next 3 lines may need to be in callback idk
        texture = parent.representation.gameObject.GetComponent<MeshRenderer>().material.mainTexture;
        surface.GetComponent<RawImage>().texture = texture;
        lastTime = master.time.julian;

        // get satellite markers
        if (master.relationshipSatellite.ContainsKey(parent)) {
            List<satellite> neededTrails = new List<satellite>();
            List<int> rs = new List<int>();
            foreach (satellite s in master.relationshipSatellite[parent]) {
                if (!s.positions.exists(master.time)) continue;
                int r = offsetKey.ContainsKey(s.name) ? offsetKey[s.name] : UnityEngine.Random.Range(0, 4);
                rs.Add(r);

                bodies[s] = generateMarker(s.name, markerType.satellite, r);

                neededTrails.Add(s);
            }

            generateTrails(neededTrails, rs);
        }
        
        // get facility markers
        if (master.relationshipFacility.ContainsKey(parent)) {
            foreach (facility f in master.relationshipFacility[parent]) {
                if (!f.exists(master.time)) continue;
                int r = offsetKey.ContainsKey(f.name) ? offsetKey[f.name] : UnityEngine.Random.Range(0, 4);
                
                RectTransform rt = generateMarker(f.name, markerType.facility, r);
                rt.anchoredPosition = geoToVec(f.geo);
                Facilities.Add(rt);
            }
        }

        foreach (var kvp in bodies) moveRepresentation(kvp.Key);

        return true;
    }

    protected override bool disable() {
        surface.GetComponent<RawImage>().enabled = false;
        foreach (RectTransform rt in bodies.Values) GameObject.Destroy(rt.gameObject);
        foreach (RectTransform rt in Facilities) GameObject.Destroy(rt.gameObject);
        foreach (GameObject go in trailsGo) GameObject.Destroy(go);

        bodies = new Dictionary<body, RectTransform>();
        Facilities = new List<RectTransform>();
        trailsGo = new List<GameObject>();

        return true;
    }

    private void generateTrails(List<satellite> ss, List<int> r) {
        double totalTime = 1;
        int resolution = 360;
        double increment = totalTime / (double) resolution;

        Dictionary<satellite, trailData> data = new Dictionary<satellite, trailData>();
        foreach (satellite s in ss) data[s] = new trailData() {
            generatedAt = master.time.julian,
            startTime = master.time.julian - totalTime * 0.5,
            endTime = master.time.julian + totalTime * 0.5,
            points = new List<List<Vector2>>() {new List<Vector2>()}};

        double checkpoint = master.time.julian;
        master.time.addJulianTime(-totalTime * 0.5);

        for (int i = 0; i < resolution + 1; i++) {
            foreach (satellite s in ss) {
                bool positionExists = s.positions.exists(master.time);
                trailData td = data[s];
                // update real start and end
                if (!data[s].identifiedStart && positionExists) {
                    td.identifiedStart = true;
                    td.realStartTime = master.time.julian;
                }
                if (!data[s].identifiedEnd && data[s].identifiedStart && !positionExists) {
                    td.identifiedEnd = true;
                    td.realEndTime = master.time.julian;
                }

                if (positionExists) {
                    Vector2 v = geoToVec(parent.worldPosToLocalGeo(s.pos)); 
                    if (td.points.Last().Count != 0) { // there is actually stuff to check
                        Vector2 _v = td.points[td.points.Count - 1].Last();
                        if (Vector2.Distance(_v, v) > Screen.height * 0.8f) td.points.Add(new List<Vector2>());
                    }
                    td.points.Last().Add(v);
                }
            }

            master.time.addJulianTime(increment);
        }

        int index = 0;
        foreach (var kvp in data) {
            int jndex = 0;
            foreach (List<Vector2> vs in kvp.Value.points) {
                GameObject go = GameObject.Instantiate(uilrPrefab);
                go.name = $"{kvp.Key.name}-{jndex}";
                go.transform.SetParent(transformParent.transform, false);
                UILineRenderer uilr = go.GetComponent<UILineRenderer>();
                uilr.color = colors[r[index]];
                uilr.Points = vs.ToArray();
                uilr.SetAllDirty();
                trailsGo.Add(go);

                jndex++;
            }
            index++;
        }

        master.time.addJulianTime(checkpoint - master.time.julian);
    }

    public void update() {
        if (!instance.active) return;
        if (lastTime == master.time.julian) return;
        lastTime = master.time.julian;

        foreach (var kvp in bodies) moveRepresentation(kvp.Key);
    }

    private void moveRepresentation(body b) {
        geographic g = parent.worldPosToLocalGeo(b.pos);

        bodies[b].anchoredPosition = geoToVec(g);
    }

    private Vector2 geoToVec(geographic g) {
        float percentX = ((float) g.lon + 180f) / 360f;
        float percentY = ((float) g.lat + 90f) / 180f;

        float x = percentX * size.x;
        float y = percentY * size.y;
        
        return new Vector2(x, y);
    }

    private RectTransform generateMarker(string name, markerType mt, int offset) {
        int r = offset + (int) mt;
        GameObject go = GameObject.Instantiate(markers[r]);
        go.name = name;
        
        go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        go.transform.SetParent(satelliteParent.transform, false);

        return go.GetComponent<RectTransform>();
    }

    
    private uiMap() {}
    private static readonly Lazy<uiMap> lazy = new Lazy<uiMap>(() => new uiMap());
    public static uiMap instance => lazy.Value;
}

internal enum markerType {
    satellite = 0, facility = 4
}

internal struct trailData {
    // regular is the ideal, real is accounting for existance time
    public double startTime, endTime, realStartTime, realEndTime, generatedAt;
    public bool identifiedStart, identifiedEnd; // default value of bool is false
    public List<List<Vector2>> points;
}