using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class uiMap : MonoBehaviour
{
    public static uiMap map;
    public static bool useUiMap = false;
    public static planet parent;
    private double lastTime;
    private Texture texture;
    private GameObject surface, uilrPrefab;
    private Vector2 size;
    private Dictionary<body, RectTransform> bodies = new Dictionary<body, RectTransform>();
    private List<RectTransform> facilites = new List<RectTransform>();
    private List<GameObject> markers = new List<GameObject>();

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
                    bodies[s] = generateMarker(s.name, markerType.satellite);
                    /*

                    if (!master.orbitalPeriods.ContainsKey(s.name)) continue;
                    GameObject go = GameObject.Instantiate(uilrPrefab);
                    go.transform.SetParent(bodies[s].gameObject.transform);
                    UILineRenderer uilr = go.GetComponent<UILineRenderer>();

                    double orbitalPeriod = master.orbitalPeriods[s.name];
                    
                    Time start = new Time(master.time.julian);
                    Time end = new Time(master.time.julian + orbitalPeriod);
                    double step = (end.julian - start.julian) / 360.0;
                    double checkpoint = start.julian;

                    if (s.positions.selection == TimelineSelection.positions) {
                        start = new Time(s.positions.tryGetStartTime());
                        end = new Time(s.positions.tryGetEndTime());
                    }

                    List<Vector2> positions = new List<Vector2>();
                    for (int i = 0; i < 360 + 1; i++) {
                        position pos = parent.rotatePoint(s.requestPosition(start)); // TODO: account for rotation at different times
                        geographic g = geographic.toGeographic(pos, parent.radius);

                        Debug.Log(pos);

                        positions.Add(geoToVec(g));

                        start.addJulianTime(step);
                        if (start.julian > end.julian + step) break;
                    }

                    uilr.Points = positions.ToArray();
                    uilr.SetAllDirty();*/
                }
            }
            
            if (master.relationshipFacility.ContainsKey(parent)) {
                foreach (facility f in master.relationshipFacility[parent]) {
                    RectTransform rt = generateMarker(f.name, markerType.facility);
                    rt.anchoredPosition = geoToVec(f.geo);
                    facilites.Add(rt);
                }
            }

            foreach (var kvp in bodies) moveRepresentation(kvp.Key);
        } else {
            surface.GetComponent<RawImage>().enabled = false;
            foreach (RectTransform rt in bodies.Values) Destroy(rt.gameObject);
            foreach (RectTransform rt in facilites) Destroy(rt.gameObject);
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
        position p = parent.rotatePoint(b.pos - b.parent.pos);
        geographic g = geographic.toGeographic(p, parent.radius);

        bodies[b].anchoredPosition = geoToVec(g);
    }

    private Vector2 geoToVec(geographic g) {
        float percentX = ((float) g.lon + 180f) / 360f;
        float percentY = ((float) g.lat + 90f) / 180f;

        float x = percentX * size.x;
        float y = percentY * size.y;
        
        return new Vector2(x, y);
    }

    private RectTransform generateMarker(string name, markerType mt) {
        int r = Random.Range(0, 4) + (int) mt;
        GameObject go = GameObject.Instantiate(markers[r]);
        go.name = name;
        
        go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        go.transform.SetParent(surface.transform);

        return go.GetComponent<RectTransform>();
    }
}

internal enum markerType {
    satellite = 0, facility = 4
}