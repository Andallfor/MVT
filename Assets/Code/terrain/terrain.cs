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
            parent.representation.forceHide = true;
        }
    }

    private void _updateTerrain() {
        // check if terrain is allowed to generate
        // something must have changed
        Vector3 currentDetectorPos = general.camera.WorldToScreenPoint(meshMovementDetector.transform.position);
        if (Vector3.Distance(currentDetectorPos, lastDetectorPos) < 0.05) return;
        lastDetectorPos = currentDetectorPos;

        // determine what meshes we want
        HashSet<geographic> targetMeshes = findDesiredMeshes();

        // figure out what meshes we want to generate
        HashSet<geographic> current = new HashSet<geographic>(currentDesiredMeshes); // toKil
        HashSet<geographic> desired = new HashSet<geographic>(targetMeshes); // toGen
        HashSet<geographic> ignore = new HashSet<geographic>();
        ignore = new HashSet<geographic>(current.Intersect(desired).ToList());
        current.SymmetricExceptWith(ignore);
        desired.SymmetricExceptWith(ignore);

        if (desired.Count == 0) return;

        currentDesiredMeshes = targetMeshes;

        foreach (geographic g in desired) meshes[g].show();

        // remove current meshes
        requestHide(current);
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

    private void requestHide(HashSet<geographic> hide) {foreach (geographic g in hide) meshes[g].hide();}
    
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

        foreach (Bounds b in ptfi.allBounds) {
            geographic g = new geographic(b.min.y, b.min.x);
            meshes[g] = new planetTerrainMeshCreator(
                parent.representation.gameObject.transform,
                $"terrainMeshes/luna/{terrainProcessor.terrainName(g, ptfi)}",
                model, mat);
        }

        currentRes = ptfi;
    }

    private List<Vector2> screenCorners = new List<Vector2>() {
        new Vector2(0, 0),
        new Vector2(Screen.width, 0),
        new Vector2(Screen.width, Screen.height),
        new Vector2(0, Screen.height)};

    private HashSet<geographic> findDesiredMeshes() {
        float planetZ = general.camera.WorldToScreenPoint(this.parent.representation.gameObject.transform.position).z;

        HashSet<geographic> points = new HashSet<geographic>();
        for (int i = 0; i < currentRes.allBounds.Count; i++) {
            Bounds b = currentRes.allBounds[i];
            List<geographic> edges = new List<geographic>() {
                new geographic(b.min.y, b.min.x), // sw
                new geographic(b.max.y, b.max.x), // ne
                new geographic(b.min.y, b.max.x), // se
                new geographic(b.max.y, b.min.x)}; // nw
            
            List<Vector3> screenEdges = new List<Vector3>();
            
            geographic avg = new geographic(0.5 * (b.max.y + b.min.y), 0.5 * (b.max.x + b.min.x));
            bool valid = false;
            
            for (int j = 0; j < 4; j++)
            {
                // check if visible
                position geoPos = parent.geoOnPlanet(edges[j], 10) + parent.pos; // IMPORTANT: if terrain disappears at small fovs, increase alt value
                Vector3 worldPos = (Vector3) ((geoPos - master.currentPosition - master.referenceFrame) / master.scale);
                
                // check if point is behind the player / hidden by planet
                Vector3 v = general.camera.WorldToScreenPoint(worldPos);
                screenEdges.Add(v);

                // is point within screen bounds
                if (v.x < 0 || v.y < 0 || v.x > Screen.width || v.y > Screen.height || v.z < 0 || v.z > planetZ) continue;

                valid = true;
            }

            // check if the player is instead fully within the bounds (and cannot see the corners)
            if (!valid) {
                Vector2 min = new Vector2(100000000, 10000000);
                Vector2 max = new Vector2(-100000000, -10000000);
                int failureCount = 0;
                foreach (Vector3 v in screenEdges) {
                    if (v.z < 0 || v.z > planetZ) failureCount++;

                    if (v.x < min.x) min.x = v.x;
                    if (v.y < min.y) min.y = v.y;
                    if (v.x > max.x) max.x = v.x;
                    if (v.y > max.y) max.y = v.y;
                }

                if (failureCount != 4) {
                    for (int j = 0; j < 4; j++) {
                        // check if any screen corner is within the bounds of the mesh
                        Vector2 v = screenCorners[j];
                        if (v.x > min.x && v.y > min.y && v.x < max.x && v.y < max.y) {
                            valid = true;
                            break;
                        }
                    }
                }
            }

            if (valid) points.Add(new geographic(b.min.y, b.min.x));
        }

        return points;
    }
}

public class planetTerrainMeshCreator {
    private GameObject go;
    private MeshRenderer mr;
    public readonly string name;
    public bool exists {get; private set;} = false;

    public planetTerrainMeshCreator(Transform parent, string path, GameObject model, Material mat) {
        this.name = path;

        Mesh m = Resources.Load<Mesh>(path);

        if (m == null) return;
        exists = true;

        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        go.GetComponent<MeshRenderer>().material = mat;
        go.GetComponent<MeshFilter>().sharedMesh = m;
        mr = go.GetComponent<MeshRenderer>();
    }

    public void hide() {
        if (!exists) return;
        mr.enabled = false;
    }

    public void show() {
        if (!exists) return;
        mr.enabled = true;
    }
}
