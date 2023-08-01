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
    private static Transform trailParent, trailSatellite, trailPlanet;
    private static Dictionary<planet, Transform> trailSatelliteByParent = new Dictionary<planet, Transform>();
    private static double initialScale;

    public const int resolution = 180;
    public bool enabled {get; private set;} = false;

    public trailRenderer(string name, GameObject go, Timeline positions, body b) {
        this.name = name;
        this.b = b;
        this.transform = go.transform;

        lr = GameObject.Instantiate(Resources.Load("Prefabs/simpleLine") as GameObject).GetComponent<LineRenderer>();
        lr.gameObject.name = $"{name} trail";

        if (b is satellite || (b as planet).pType == planetType.moon) {
            if (!trailSatelliteByParent.ContainsKey(b.parent)) {
                GameObject sp = resLoader.createPrefab("empty");
                sp.transform.parent = GameObject.FindGameObjectWithTag("planet/trails/keplerSatellite").transform;
                sp.name = b.parent.name + " trails";

                trailSatelliteByParent[b.parent] = sp.transform;
            }

            lr.transform.parent = trailSatelliteByParent[b.parent];
        } else lr.transform.parent = GameObject.FindGameObjectWithTag("planet/trails/keplerPlanet").transform;
        
        lr.startWidth = 0.015f;
        lr.endWidth = 0.015f;

        general.onStatusChange += disableWrapper;
        master.onReferenceFrameChange += (s, e) => disable();

        disable();
    }

    public void enable() {
        if (enabled) return;
        if (name == master.sun.name) return;

        initialScale = master.scale;

        disable();

        // really should cache these values
        Vector3[] points = new Vector3[resolution + 1];
        double step = 0;
        if (master.orbitalPeriods.ContainsKey(name)) {
            double period = master.orbitalPeriods[name];
            step = period / (double) resolution;
        } else if (b.positions.selection == TimelineSelection.kepler) {
            double period = b.positions.findOrbitalPeriod();
            step = period / (double) resolution;
        } else {
            // cant do positional because we assume what the parents are below, namely to parent/sun
            // positional can be parented to literally anything
            Debug.LogWarning("Unable to display trail for non-keplerian orbit!");
            return;
        }

        for (int i = 0; i < resolution + 1; i++) {
            double time = master.time.julian + i * step;
            position p = b.positions.find(time);

            // satellites should be relative to parent, planets relative to sun
            p /= master.scale;

            points[i] = (Vector3) p;
        }

        setPositions(points);
    }

    private void setPositions(Vector3[] v) {
        lr.positionCount = v.Length;
        lr.SetPositions(v);
        enabled = true;
        lr.gameObject.SetActive(true);
    }

    public void disable() {
        lr.gameObject.SetActive(false);
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
        if (!general.showingTrails) return;
        if (trailParent == default(Transform)) trailParent = GameObject.FindGameObjectWithTag("planet/trails").transform;
        if (trailSatellite == default(Transform)) trailSatellite = GameObject.FindGameObjectWithTag("planet/trails/keplerSatellite").transform;
        if (trailPlanet == default(Transform)) trailPlanet = GameObject.FindGameObjectWithTag("planet/trails/keplerPlanet").transform;

        //if (planetOverview.instance.active) trailParent.position = Vector3.zero;
        //else trailParent.position = -(Vector3) (master.currentPosition / master.scale);

        // TODO: add in fancy code to determine if we can see trail renderer or not. currently it just pretends this isnt an issue
        float scale = (float) (initialScale / master.scale);
        trailParent.localScale = new Vector3(scale, scale, scale);

        trailPlanet.position = -(Vector3) (master.referenceFrame / master.scale);
        foreach (planet p in trailSatelliteByParent.Keys) {
            Transform parent = trailSatelliteByParent[p];
            parent.position = (Vector3) ((p.pos - master.referenceFrame) / master.scale);
        }
    }
}
