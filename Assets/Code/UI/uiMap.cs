using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;

public class uiMap : MonoBehaviour
{
    public static uiMap map;
    public static bool useUiMap = false;
    public static planet parent;
    private double lastTime;
    private Texture texture;
    private GameObject surface, uilrPrefab, transformParent, satelliteParent;
    private Vector2 size;
    private Dictionary<body, RectTransform> bodies = new Dictionary<body, RectTransform>();
    private List<GameObject> trailsGo = new List<GameObject>();
    private List<RectTransform> facilites = new List<RectTransform>();
    private List<GameObject> markers = new List<GameObject>();
    private Dictionary<body, Vector2[]> trails = new Dictionary<body, Vector2[]>();
    private List<Color32> colors = new List<Color32>() {
        new Color32(255, 102, 0, 75), new Color32(255, 204, 0, 75), new Color32(78, 255, 0, 75), new Color32(0, 179, 255, 75)
    };

    private Dictionary<string, int> offsetKey = new Dictionary<string, int>() {
        {"LCN-1", 0},
        {"LCN-2", 0},
        {"LCN-3", 0},
        {"CubeSat-1", 2},
        {"CubeSat-2", 2},
    };

    public void Awake() {
        uiMap.map = this;

        for (int i = 1; i < 9; i++) {
            markers.Add(Resources.Load($"Prefabs/markers/marker{i}") as GameObject);
        }

        surface = GameObject.FindGameObjectWithTag("ui/map/surface");
        uilrPrefab = Resources.Load("Prefabs/uilr") as GameObject;
        size = new Vector2(
            surface.GetComponent<RectTransform>().rect.width,
            surface.GetComponent<RectTransform>().rect.height);
        transformParent = GameObject.FindGameObjectWithTag("ui/map/trails");
        satelliteParent = GameObject.FindGameObjectWithTag("ui/map/surface/satellites");
    }

    public bool toggle(bool value) {
        if (master.requestReferenceFrame() is planet) {
            parent = master.requestReferenceFrame() as planet;
        } else return false;

        useUiMap = value;
        texture = parent.representation.gameObject.GetComponent<MeshRenderer>().material.mainTexture;
        surface.GetComponent<RawImage>().texture = texture;
        lastTime = master.time.julian;

        if (useUiMap) {
            surface.GetComponent<RawImage>().enabled = true;

            if (master.relationshipSatellite.ContainsKey(parent)) {
                foreach (satellite s in master.relationshipSatellite[parent]) {
                    int r = offsetKey.ContainsKey(s.name) ? offsetKey[s.name] : UnityEngine.Random.Range(0, 4);

                    bodies[s] = generateMarker(s.name, markerType.satellite, r);

                    GameObject go = GameObject.Instantiate(uilrPrefab);
                    go.transform.SetParent(transformParent.transform, false);
                    UILineRenderer uilr = go.GetComponent<UILineRenderer>();
                    uilr.color = colors[r];
                    uilr.Points = checkForTrail(s);
                    uilr.SetAllDirty();

                    trailsGo.Add(go);
                }
            }
            
            if (master.relationshipFacility.ContainsKey(parent)) {
                foreach (facility f in master.relationshipFacility[parent]) {
                    int r = offsetKey.ContainsKey(f.name) ? offsetKey[f.name] : UnityEngine.Random.Range(0, 4);
                    
                    RectTransform rt = generateMarker(f.name, markerType.facility, r);
                    rt.anchoredPosition = geoToVec(f.geo);
                    facilites.Add(rt);
                }
            }

            foreach (var kvp in bodies) moveRepresentation(kvp.Key);
        } else {
            surface.GetComponent<RawImage>().enabled = false;
            foreach (RectTransform rt in bodies.Values) Destroy(rt.gameObject);
            foreach (RectTransform rt in facilites) Destroy(rt.gameObject);
            foreach (GameObject go in trailsGo) Destroy(go);

            bodies = new Dictionary<body, RectTransform>();
            facilites = new List<RectTransform>();
            trailsGo = new List<GameObject>();
        }

        return value;
    }

    public void Update() {
        if (!useUiMap) return;
        if (lastTime == master.time.julian) return;
        lastTime = master.time.julian;

        foreach (var kvp in bodies) moveRepresentation(kvp.Key);
    }

    private void moveRepresentation(body b) {
        geographic g = parent.posToLocalGeo(b.localPos);

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

    private Vector2[] checkForTrail(body b) {
        if (trails.ContainsKey(b)) return trails[b];

        if (!master.orbitalPeriods.ContainsKey(b.name)) {
            trails[b] = new Vector2[0];
            return trails[b];
        }

        double orbitalPeriod = master.orbitalPeriods[b.name];
        
        Time start = master.time;
        Time end = new Time(master.time.julian + orbitalPeriod);
        double step = (end.julian - start.julian) / 360.0;
        double checkpoint = start.julian;

        if (b.positions.selection == TimelineSelection.positions) {
            start = new Time(b.positions.tryGetStartTime());
            end = new Time(b.positions.tryGetEndTime());
        }

        bool jump = false;
        geographic lastGeo = new geographic(0, 0);
        List<Vector2> positions = new List<Vector2>();
        List<Vector2> jumped = new List<Vector2>();
        for (int i = 0; i < 360 + 1; i++) {
            geographic g = parent.posToLocalGeo(b.localPos);

            if (Math.Sign(g.lon) != Math.Sign(lastGeo.lon) && Math.Abs(g.lon) > 150 && Math.Abs(lastGeo.lon) > 150) jump = true;

            if (!jump) positions.Add(geoToVec(g));
            else jumped.Add(geoToVec(g));

            start.addJulianTime(step);
            lastGeo = g;
            if (start.julian > end.julian + step) break;
        }

        master.time.addJulianTime(checkpoint - master.time.julian);

        if (jumped.Count == 0) {
            trails[b] = positions.ToArray();
        } else if (positions.Count == 0) {
            trails[b] = jumped.ToArray();
        } else {
            jumped.AddRange(positions);
            trails[b] = jumped.ToArray();
        }

        return trails[b];
    }
}

internal enum markerType {
    satellite = 0, facility = 4
}