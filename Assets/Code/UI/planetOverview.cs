using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class planetOverview
{
    public static bool usePlanetOverview {get {return _upo;}}
    private static bool _upo = false;
    public static double maxDist = 0;
    public static float rotationalOffset = 0;
    private static float lastRotationalOffset = 0;

    private static Dictionary<string, lineController> axes = new Dictionary<string, lineController>() {
        {"+x", null},
        {"-x", null},
        {"+y", null},
        {"-y", null},
        {"+z", null},
        {"-z", null}};
    private static Dictionary<planet, lineController> planets = new Dictionary<planet, lineController>();

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

    public static void enable(bool use)
    {
        if (use)
        {
            foreach (planet p in master.allPlanets)
            {
                if (p.pType != planetType.planet) continue;
                
                double dist = p.pos.length();
                if (maxDist < dist) maxDist = dist;
            }

            drawAxes();

            Camera c = Camera.main;
            c.transform.position = new Vector3(-15, 7.5f, -15);
            c.transform.rotation = Quaternion.Euler(20, 45, 0);
            c.orthographic = true;
            c.orthographicSize = 5;
        }
        else 
        {
            Camera c = Camera.main;
            c.transform.position = new Vector3(0, 0, -10);
            c.transform.rotation = Quaternion.Euler(0, 0, 0);
            c.orthographic = false;
            c.fieldOfView = 60;
            clearAxes();

            foreach (facility f in master.allFacilites)
            {
                f.representation.setActive(true);
            }
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

        foreach (planet p in master.allPlanets)
        {
            if (p.pType != planetType.planet) continue;
            
            lineController lc = null;
            if (!planets.ContainsKey(p))
            {
                GameObject go = GameObject.Instantiate(Resources.Load("Prefabs/planetOverviewLine") as GameObject);
                lc = go.GetComponent<lineController>();
                planets[p] = lc;
            } else lc = planets[p];

            Color c = UnityEngine.Random.ColorHSV();
            lc.setColor(c);

            MeshRenderer mr = lc.gameObject.GetComponent<MeshRenderer>();
            mr.material.color = c;
            mr.enabled = true;
        }

        updateAxes(true);
    }

    private static void clearAxes()
    {
        rotationalOffset = 0;

        updateAxes(true);
        foreach (lineController lc in axes.Values) lc.clearLine();
        foreach (lineController lc in planets.Values)
        {
            lc.clearLine();
            lc.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    public static void updateAxes(bool force = false)
    {
        // update planetary lines in accordance to the new planets positions
        foreach (KeyValuePair<planet, lineController> kvp in planets)
        {
            Vector3 start = (Vector3) (planetOverviewPosition(kvp.Key.pos) / master.scale);
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
    }

    public static position planetOverviewPosition(position pos)
    {
        if (pos == new position(0, 0, 0)) return new position(0, 0, 0);
        position p = pos.normalize();
        double ratio = pos.length() / planetOverview.maxDist;

        double adjustedRatio = 0.25 * Math.Log(ratio + 0.01, 10) + 0.5;

        return p * adjustedRatio * master.scale * 12.0;
    }
}
