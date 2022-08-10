using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class planetTerrain
{
    private GameObject meshMovementDetector, model;
    private Material mat;
    public planet parent;
    private Vector3 lastDetectorPos;
    private Dictionary<geographic, planetTerrainMeshCreator> meshes = new Dictionary<geographic, planetTerrainMeshCreator>();
    private planetTerrainFolderInfo currentRes;
    private HashSet<geographic> currentDesiredMeshes = new HashSet<geographic>();
    public double radius, heightMulti;

    public void updateTerrain() {
        if (!planetFocus.usePlanetFocus) parent.representation.forceHide = false;
        else {
            if (!planetFocus.usePoleFocus) _updateTerrain();
            else unload();
            //parent.representation.forceHide = true;
        }
    }

    private void _updateTerrain() {
        // check if terrain is allowed to generate
        // something must have changed
        Vector3 currentDetectorPos = general.camera.WorldToScreenPoint(meshMovementDetector.transform.position);
        if (Vector3.Distance(currentDetectorPos, lastDetectorPos) < 0.05) return;
        lastDetectorPos = currentDetectorPos;

        // determine what meshes we want
        HashSet<geographic> targetMeshes = findDesiredMeshes(currentRes);

        // figure out what meshes we want to generate
        HashSet<geographic> current = new HashSet<geographic>(currentDesiredMeshes); // toKil
        HashSet<geographic> desired = new HashSet<geographic>(targetMeshes); // toGen
        HashSet<geographic> ignore = new HashSet<geographic>();
        ignore = new HashSet<geographic>(current.Intersect(desired).ToList());
        current.SymmetricExceptWith(ignore);
        desired.SymmetricExceptWith(ignore);

        if (desired.Count == 0) return;

        currentDesiredMeshes = targetMeshes;

        foreach (geographic g in desired) {if (meshes.ContainsKey(g)) meshes[g].show();};

        // remove current meshes
        foreach (geographic g in current) {if (meshes.ContainsKey(g)) meshes[g].hide();};
    }

    public planetTerrain(planet parent, string materialPath, double radius, double heightMulti) {
        this.parent = parent;
        this.radius = radius;
        this.heightMulti = heightMulti;

        meshMovementDetector = GameObject.Instantiate(Resources.Load("Prefabs/default") as GameObject, parent.representation.gameObject.transform);
        meshMovementDetector.transform.position = new Vector3(1, 1, 1);
        meshMovementDetector.GetComponent<MeshRenderer>().enabled = false;
        lastDetectorPos = Vector3.zero;

        model = Resources.Load("Prefabs/PlanetMesh") as GameObject;
        mat = Resources.Load(materialPath) as Material;
    }

    public void registerMesh(geographic g, Mesh m) {
        meshes[g] = new planetTerrainMeshCreator(parent.representation.gameObject.transform, g.ToString(), m, model, mat);
    }
    
    public void unload() {
        if (currentDesiredMeshes.Count == 0) return;
        foreach (planetTerrainMeshCreator mc in meshes.Values) mc.hide();
        currentDesiredMeshes = new HashSet<geographic>();
        lastDetectorPos = Vector3.negativeInfinity;
    }

    public void generateFolderInfos(string data)
    {
        planetTerrainFolderInfo ptfi = new planetTerrainFolderInfo(data);
        meshes = new Dictionary<geographic, planetTerrainMeshCreator>();

        currentRes = ptfi;
    }

    private List<Vector2> screenCorners = new List<Vector2>() { new Vector2(0, 0), new Vector2(Screen.width, 0), new Vector2(Screen.width, Screen.height), new Vector2(0, Screen.height) };
    
    private Vector2 vec3To2(Vector3 v) => new Vector2(v.x, v.y);
    private List<Vector2> directions = new List<Vector2>() {new Vector2(0, 1), new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1)};
    private HashSet<geographic> findDesiredMeshes(planetTerrainFolderInfo p) {
        float planetZ = general.camera.WorldToScreenPoint(this.parent.representation.gameObject.transform.position).z;

        // get radius of parent in screen length
        // draw line from parent screen center to center of screen
        // find percent of radius screen length to line screen length
        // get x percent of line screen (starting at parent)
        // check if point exists, if yes then parent on screen otherwise no
        // get the geo of said point with code below
        // flood fill alg to find meshes on screen
            // dont use below below code for checking if on screen
            // check if center point is on screen
            // if not but previous was keep, then end that iter
            
            // dont do alg where new queue is adding dynamically, each queue should be added after the current queue is enteirely emptied
            // should resemble a circle growing otuwards

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 parentCenter = vec3To2(general.camera.WorldToScreenPoint(parent.representation.gameObject.transform.position));
        float screenRadius = Vector2.Distance(parentCenter, parentCenter + new Vector2(0, (float) (parent.radius / master.scale)));
        float percent = screenRadius / Vector2.Distance(parentCenter, screenCenter);
        if (float.IsInfinity(percent) || float.IsNaN(percent)) percent = 0;
        float diffX = (parentCenter.x - screenCenter.x) * percent;
        float diffY = (parentCenter.y - screenCenter.y) * percent;
        Vector2 projectedPoint = screenCenter + new Vector2(diffX, diffY);

        List<position> intersections = position.lineSphereInteresection(
            general.camera.ScreenToWorldPoint(new Vector3(projectedPoint.x, projectedPoint.y, 1)),
            general.camera.ScreenToWorldPoint(new Vector3(projectedPoint.x, projectedPoint.y, 10)),
            new position(0, 0, 0),
            parent.radius / master.scale);

        if (intersections.Count == 0) return new HashSet<geographic>();
        
        position preferred = intersections[0];
        if (intersections.Count == 2) {
            double dist1 = position.distance(preferred, general.camera.transform.position);
            double dist2 = position.distance(intersections[1], general.camera.transform.position);
            if (dist2 < dist1) preferred = intersections[1];
        }

        geographic fillCenter = parent.localPosToLocalGeo(preferred + master.currentPosition / master.scale);
        geographic startingMesh = new geographic(
            fillCenter.lat - fillCenter.lat % p.increment.lat,
            fillCenter.lon - fillCenter.lon % p.increment.lon);
        
        Queue<geographic> toCheck = new Queue<geographic>(new List<geographic>() {startingMesh});
        HashSet<geographic> points = new HashSet<geographic>();
        bool allOffscreen = false;
        while (!allOffscreen) {
            allOffscreen = true;
            HashSet<geographic> nextFrontier = new HashSet<geographic>();
            while (toCheck.Count != 0) {
                geographic g = toCheck.Dequeue();

                Vector3 v = general.camera.WorldToScreenPoint(parent.localGeoToUnityPos(g + p.increment / 2.0, 10));
                if (v.z < 0 || v.z > planetZ) continue;
                float dist = Vector2.Distance(vec3To2(v), screenCenter);
                float maxAllowedMeshDist = Vector2.Distance(
                    vec3To2(general.camera.WorldToScreenPoint(parent.localGeoToUnityPos(g, 0))),
                    vec3To2(general.camera.WorldToScreenPoint(parent.localGeoToUnityPos(g + p.increment, 0)))) + screenCenter.magnitude;
                if (dist > maxAllowedMeshDist * 1.05f) continue;
                allOffscreen = false;

                foreach (Vector2 dir in directions) {
                    geographic next = g + new geographic(dir.x * p.increment.lat, dir.y * p.increment.lon);
                    if (!points.Contains(next) && !nextFrontier.Contains(next)) {
                        points.Add(next);
                        nextFrontier.Add(next);
                    }
                }
            }

            toCheck = new Queue<geographic>(nextFrontier);
        }

        return new HashSet<geographic>(points.Where(x => meshes.ContainsKey(x)));
    }
}

public class planetTerrainMeshCreator {
    private GameObject go;
    private MeshRenderer mr;
    public readonly string name;

    public planetTerrainMeshCreator(Transform parent, string name, Mesh m, GameObject model, Material mat) {
        this.name = name;

        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        go.GetComponent<MeshRenderer>().material = mat;
        go.GetComponent<MeshFilter>().sharedMesh = m;
        mr = go.GetComponent<MeshRenderer>();

        mr.enabled = false;
    }

    public void hide() {
        mr.enabled = false;
    }

    public void show() {
        mr.enabled = true;
    }
}
