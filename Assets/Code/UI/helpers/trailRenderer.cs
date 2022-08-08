using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class trailRenderer
{
    private LineRenderer lr;
    private string name;
    private body b;
    private double orbitalPeriod = 0;
    private Transform transform, parentTransform;

    public const int resolution = 360;
    public bool enabled {get; private set;} = false;

    public trailRenderer(string name, GameObject go, Timeline positions, body b) {
        this.name = name;
        this.b = b;
        this.transform = go.transform;

        if (master.orbitalPeriods.ContainsKey(name)) orbitalPeriod = master.orbitalPeriods[name];
        lr = GameObject.Instantiate(Resources.Load("Prefabs/simpleLine") as GameObject).GetComponent<LineRenderer>();
        lr.gameObject.name = $"{name} trail";

        lr.transform.parent = GameObject.FindGameObjectWithTag("planet/trails").transform;

        general.onStatusChange += disableWrapper;
        master.onFinalSetup += (s, e) => master.onCurrentPositionChange += update;
    }

    private void update(object sender, EventArgs e) {
        if (planetOverview.usePlanetOverview) return;
        body bb = master.requestReferenceFrame();
        GameObject go = null;
        if (bb is planet) go = ((planet) bb).representation.gameObject;
        else go = ((satellite) bb).representation.gameObject;
        lr.gameObject.transform.position = go.transform.position;
    }

    public void enable() {
        if (!transform.gameObject.GetComponent<MeshRenderer>().enabled) return;

        enabled = true;
        lr.positionCount = 0;

        if (orbitalPeriod == 0) Debug.LogWarning($"No orbital period found for {name}");
        else { // we know the orbital period, so draw the orbit
            if (planetOverview.usePlanetOverview && planetOverview.focus.name == name) return;
            if (!planetOverview.usePlanetOverview && master.requestReferenceFrame().name == name) return;

            double checkpoint = master.time.julian;
            master.time.addJulianTime(-orbitalPeriod * 0.5);
            double increment = orbitalPeriod / (double) resolution;

            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < resolution + 1; i++) {
                position pos = b.pos;
                if (planetOverview.usePlanetOverview) pos = planetOverview.planetOverviewPosition(pos - planetOverview.focus.pos);
                else pos -= master.requestReferenceFrame().pos;
                pos /= master.scale;

                positions.Add((Vector3) pos);
                master.time.addJulianTime(increment * 2.0);
            }

            master.time.addJulianTime(checkpoint - master.time.julian);

            setPositions(positions);
        }
    }

    public void setPositions(List<Vector3> v) {
        lr.positionCount = v.Count;
        lr.SetPositions(v.ToArray());

        lr.transform.localScale = new Vector3(
            1f / lr.transform.parent.gameObject.transform.localScale.x,
            1f / lr.transform.parent.gameObject.transform.localScale.y,
            1f / lr.transform.parent.gameObject.transform.localScale.z);
    }

    public void disable() {
        enabled = false;
        lr.positionCount = 0;
    }

    private void disableWrapper(object sender, EventArgs e) {disable();}

    public void enable(bool value) {
        if (!value) disable();
        else enable();

        update(null, EventArgs.Empty);
    }

    public static void drawAllSatelliteTrails(List<satellite> _desired) {
        Dictionary<satellite, List<Vector3>> output = new Dictionary<satellite, List<Vector3>>();

        List<satellite> desired = new List<satellite>();
        foreach (satellite s in _desired) {
            if (s.name == master.requestReferenceFrame().name) continue;
            if (!s.tr.transform.gameObject.GetComponent<MeshRenderer>().enabled) continue;

            desired.Add(s);
            output[s] = new List<Vector3>();
        }

        double totalTime = 1;
        double increment = totalTime / (double) resolution;
        double checkpoint = master.time.julian;

        master.time.addJulianTime(-totalTime * 0.5);

        for (int i = 0; i < resolution + 1; i++) {
            position refPos = master.requestReferenceFrame().pos;
            foreach (satellite s in desired) {
                if (!s.positions.exists(master.time)) continue;

                position p = s.pos;
                if (planetOverview.usePlanetOverview) p = planetOverview.planetOverviewPosition(p - planetOverview.focus.pos);
                else p -= refPos;

                p /= master.scale;

                output[s].Add((Vector3) p);
            }

            master.time.addJulianTime(increment);
        }

        master.time.addJulianTime(checkpoint - master.time.julian);

        foreach (satellite s in desired) {
            s.tr.lr.positionCount = 0;
            s.tr.setPositions(output[s]);
            s.tr.enabled = true;
            s.tr.update(null, EventArgs.Empty);
        }
    }
}
