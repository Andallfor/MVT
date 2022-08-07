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
    private GameObject planetParent;
    private double orbitalPeriod = 0;
    private Transform transform;

    public const int resolution = 120;
    public bool enabled {get; private set;} = false;

    public trailRenderer(string name, GameObject go, Timeline positions, body b) {
        this.name = name;
        this.b = b;
        this.transform = go.transform;

        if (master.orbitalPeriods.ContainsKey(name)) orbitalPeriod = master.orbitalPeriods[name];
        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
        lr = GameObject.Instantiate(Resources.Load("Prefabs/simpleLine") as GameObject).GetComponent<LineRenderer>();
        lr.gameObject.name = $"{name} trail";

        lr.transform.parent = GameObject.FindGameObjectWithTag("planet/trails").transform;

        general.onStatusChange += disableWrapper;
    }

    public void enable() {
        if (!transform.gameObject.GetComponent<MeshRenderer>().enabled) return;

        enabled = true;

        lr.positionCount = 0;

        if (orbitalPeriod == 0) Debug.LogWarning($"No orbital period found for {name}");
        else { // we know the orbital period, so draw the orbit
            if (planetOverview.usePlanetOverview && planetOverview.focus.name == name) return;
            if (!planetOverview.usePlanetOverview && master.requestReferenceFrame().name == name) return;

            Time start = master.time;
            Time end = new Time(master.time.julian + orbitalPeriod);
            double step = (end.julian - start.julian) / (double) resolution;
            double checkpoint = start.julian;

            if (b.positions.selection == TimelineSelection.positions) {
                start = new Time(b.positions.tryGetStartTime());
                end = new Time(b.positions.tryGetEndTime());
            }

            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < resolution + 1; i++) {
                position pos = b.requestPosition(start);
                position _p = pos;

                if (planetOverview.usePlanetOverview) pos = planetOverview.planetOverviewPosition(pos - planetOverview.focus.pos);
                else pos -= master.requestReferenceFrame().requestPosition(start);

                pos /= master.scale;

                positions.Add((Vector3) pos);

                start.addJulianTime(step);
                if (start.julian > end.julian + step) break;
            }

            master.time.addJulianTime(checkpoint - master.time.julian);

            lr.positionCount = positions.Count;
            lr.SetPositions(positions.ToArray());

            lr.transform.localScale = new Vector3(
                1f / lr.transform.parent.gameObject.transform.localScale.x,
                1f / lr.transform.parent.gameObject.transform.localScale.y,
                1f / lr.transform.parent.gameObject.transform.localScale.z);
        }
    }

    public void disable() {
        enabled = false;
        lr.positionCount = 0;
    }

    private void disableWrapper(object sender, EventArgs e) {
        disable();
    }

    public void enable(bool value) {
        if (!value) disable();
        else enable();
    }
}
