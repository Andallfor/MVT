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
    private Transform transform;
    private static Transform trailParent;

    public const int resolution = 180;
    public bool enabled {get; private set;} = false;

    public trailRenderer(string name, GameObject go, Timeline positions, body b) {
        this.name = name;
        this.b = b;
        this.transform = go.transform;

        lr = GameObject.Instantiate(Resources.Load("Prefabs/simpleLine") as GameObject).GetComponent<LineRenderer>();
        lr.gameObject.name = $"{name} trail";

        lr.transform.parent = GameObject.FindGameObjectWithTag("planet/trails").transform;
        lr.startWidth = 0.015f;
        lr.endWidth = 0.015f;

        general.onStatusChange += disableWrapper;
        master.onReferenceFrameChange += (s, e) => disable();
    }

    public void enable() {
        if (!transform.gameObject.GetComponent<MeshRenderer>().enabled) return;
        if (name == master.requestReferenceFrame().name) return;
        if (enabled) return;

        disable();

        Vector3[] points = new Vector3[resolution];
        double step = 0;
        if (b.positions.selection == TimelineSelection.kepler) {
            double period = b.positions.findOrbitalPeriod();
            step = period / (double) resolution;
        } else if (master.orbitalPeriods.ContainsKey(name)) {
            double period = master.orbitalPeriods[name];
            step = period / (double) resolution;
        } else step = 1.0 / (double) resolution;

        for (int i = 0; i < resolution; i++) {
            double time = master.time.julian + i * step;
            position p = b.requestPosition(time);

            p -= master.requestReferenceFrame().requestPosition(time);
            p /= master.scale;

            points[i] = (Vector3) p;
        }

        setPositions(points);
    }

    private void setPositions(Vector3[] v) {
        lr.positionCount = v.Length;
        lr.SetPositions(v);
        enabled = true;
    }

    public void disable() {
        enabled = false;
        lr.positionCount = 0;
    }

    private void disableWrapper(object sender, EventArgs e) {disable();}

    public static void enableAll() {
        foreach (satellite s in master.allSatellites) s.tr.enable();
        foreach (planet p in master.allPlanets) p.tr.enable();
    }

    public static void disableAll() {
        foreach (satellite s in master.allSatellites) s.tr.disable();
        foreach (planet p in master.allPlanets) p.tr.disable();
    }

    public static void update() {
        if (trailParent == default(Transform)) trailParent = GameObject.FindGameObjectWithTag("planet/trails").transform;

        if (planetOverview.instance.active) trailParent.position = Vector3.zero;
        else trailParent.position = -(Vector3) (master.currentPosition / master.scale);
    }
}
