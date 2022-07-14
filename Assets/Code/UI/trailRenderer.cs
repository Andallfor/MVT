using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NumSharp;
using System.Linq;

public class trailRenderer
{
    private LineRenderer lr;
    private string name;
    private body b;
    private GameObject planetParent;
    private double orbitalPeriod = 0;
    private Transform transform;

    public const int resolution = 60;
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

        master.onFinalSetup += findParent;
        general.onStatusChange += disableWrapper;
    }

    private void findParent(object sender, EventArgs e) {
        lr.transform.parent = null;
        // all hail linq
        if (master.relationshipSatellite.Any(x => x.Value.Exists(y => y.name == name)) ||
            master.relationshipPlanet.Any(x => x.Value.Exists(y => y.name == name)) &&
            !planetOverview.usePlanetOverview) {
            if (b is satellite) lr.transform.parent = master.relationshipSatellite.First(x => x.Value.Exists(y => y.name == name)).Key.representation.gameObject.transform;
            else if (b is planet) lr.transform.parent = master.relationshipPlanet.First(x => x.Value.Exists(y => y.name == name)).Key.representation.gameObject.transform;
        } else lr.transform.parent = GameObject.FindGameObjectWithTag("planet/trails").transform;
    }

    public void enable() {
        if (!transform.gameObject.GetComponent<MeshRenderer>().enabled) return;

        enabled = true;

        lr.positionCount = 0;
        findParent(null, EventArgs.Empty);

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

    public void toggle() {
        if (enabled) disable();
        else enable();
    }
}
