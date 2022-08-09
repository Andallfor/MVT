using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using UnityEngine.UI;
using B83.MeshTools;
using Newtonsoft.Json;

public class planetTerrain
{
    private GameObject meshMovementDetector, model;
    private Material mat;
    public planet parent;
    private Vector3 lastDetectorPos;
    private Dictionary<planetTerrainFolderInfo, Dictionary<geographic, planetTerrainMeshCreator>> meshes = new Dictionary<planetTerrainFolderInfo, Dictionary<geographic, planetTerrainMeshCreator>>();
    private planetTerrainFolderInfo currentRes;
    private List<planetTerrainFolderInfo> sortedResolutions = new List<planetTerrainFolderInfo>();
    private Dictionary<string, planetTerrainFolderInfo> folderInfos = new Dictionary<string, planetTerrainFolderInfo>();
    private HashSet<geographic> currentDesiredMeshes = new HashSet<geographic>();
    public double radius, heightMulti;
    private Dictionary<string, Dictionary<string, Dictionary<string, long[]>>> savedPositions = new Dictionary<string, Dictionary<string, Dictionary<string, long[]>>>();

    public void updateTerrain() {
        if (!planetFocus.usePlanetFocus) parent.representation.forceHide = false;
        else {
            if (!planetFocus.usePoleFocus) _updateTerrain();
            else unload();
            //parent.representation.forceHide = true;
        }
    }

    private async void _updateTerrain() {
        // check if terrain is allowed to generate
        // something must have changed
        Vector3 currentDetectorPos = general.camera.WorldToScreenPoint(meshMovementDetector.transform.position);
        if (Vector3.Distance(currentDetectorPos, lastDetectorPos) < 0.05) return;
        lastDetectorPos = currentDetectorPos;

        // determine the resolution at which we want to generate
        planetTerrainFolderInfo targetRes = findDesiredResolution();

        // determine what meshes we want
        HashSet<geographic> targetMeshes = findDesiredMeshes(targetRes);

        // figure out what meshes we want to generate
        HashSet<geographic> current = new HashSet<geographic>(currentDesiredMeshes); // toKil
        HashSet<geographic> desired = new HashSet<geographic>(targetMeshes); // toGen
        HashSet<geographic> ignore = new HashSet<geographic>();
        if (targetRes == currentRes) {
            ignore = new HashSet<geographic>(current.Intersect(desired).ToList());
            current.SymmetricExceptWith(ignore);
            desired.SymmetricExceptWith(ignore);
        }

        if (desired.Count == 0) return;

        planetTerrainFolderInfo lastP = new planetTerrainFolderInfo(currentRes);

        currentRes = targetRes;
        currentDesiredMeshes = targetMeshes;

        await drawMeshes(desired);

        // remove current meshes
        requestKill(lastP, current);
    }

    private async Task drawMeshes(HashSet<geographic> m) {
        planetTerrainFolderInfo initalRes = new planetTerrainFolderInfo(currentRes);

        Queue<geographic> queue = new Queue<geographic>(m);

        float batchCount = 2;
        for (int i = 0; i < (float) m.Count / batchCount; i++) {
            List<Task> tasks = new List<Task>();

            for (int j = 0; j < batchCount; j++) {
                if (initalRes.GetHashCode() != currentRes.GetHashCode()) return; // we switched res, so unload everything
                if (queue.Count == 0) break;

                geographic g = queue.Dequeue();
                planetTerrainMeshCreator ptmc = meshes[initalRes][g];

                // we no longer want to generate this mesh
                if (!currentDesiredMeshes.Contains(g)) continue;

                Task t = ptmc.generate(model, mat);
                if (!(t is null)) tasks.Add(t);
            }

            await Task.WhenAll(tasks);
            await Task.Delay(4);
        }
    }

    public void save(string outputPath, string srcFolder) {
        planetTerrainFolderInfo p = new planetTerrainFolderInfo(srcFolder);
        Dictionary<string, Dictionary<string, long[]>> pos = new Dictionary<string, Dictionary<string, long[]>>();

        foreach (Bounds b in p.allBounds) {
            geographic g = new geographic(b.min.y, b.min.x);

            string predictedFileName = terrainProcessor.fileName(
                g,
                p.increment,
                p.type == terrainFileType.npy ? "npy" : "txt");
            string predictedPath = Path.Combine(
                p.folderPath,
                predictedFileName);
            
            if (!File.Exists(predictedPath)) continue;

            planetTerrainFile ptf = new planetTerrainFile(predictedPath, p, p.type);
            planetTerrainMesh ptm = new planetTerrainMesh(ptf, p, this, false);
            ptf.generate(ptm);
            GameObject go = ptm.drawMesh(mat);
            byte[] data = MeshSerializer.SerializeMesh(
                go.GetComponent<MeshFilter>().mesh,
                Path.GetFileNameWithoutExtension(terrainProcessor.terrainName(g, p)),
                ref pos);
            
            File.WriteAllBytes(Path.Combine(outputPath, terrainProcessor.terrainName(g, p)), data);
        }

        File.WriteAllText(Path.Combine(outputPath, "data.json"), JsonConvert.SerializeObject(pos));

        // require resData.txt in output folder for ptfi
        File.Copy(Path.Combine(srcFolder, terrainProcessor.folderInfoName), Path.Combine(outputPath, terrainProcessor.folderInfoName), true);
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

    private void requestKill(planetTerrainFolderInfo res, HashSet<geographic> kill) {foreach (geographic g in kill) meshes[res][g].kill();}
    
    public void unload() {
        if (currentDesiredMeshes.Count == 0) return;
        foreach (geographic g in meshes[currentRes].Keys) meshes[currentRes][g].kill();
        currentDesiredMeshes = new HashSet<geographic>();
        lastDetectorPos = Vector3.negativeInfinity;
    }

    public void generateFolderInfos(string[] folders)
    {
        foreach (string folder in folders)
        {
            planetTerrainFolderInfo ptfi = new planetTerrainFolderInfo(folder);
            savedPositions[ptfi.name] = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, long[]>>>(
                File.ReadAllText(Path.Combine(folder, "data.json")));

            folderInfos[ptfi.name] = ptfi;
            meshes[ptfi] = new Dictionary<geographic, planetTerrainMeshCreator>();

            foreach (Bounds b in ptfi.allBounds) {
                geographic g = new geographic(b.min.y, b.min.x);
                meshes[ptfi][g] = new planetTerrainMeshCreator(
                    ptfi,
                    g,
                    parent.representation.gameObject.transform,
                    Path.Combine(folder, terrainProcessor.terrainName(g, ptfi)),
                    savedPositions[ptfi.name]);
            }
        }

        sortedResolutions = folderInfos.Values.ToList();
        sortedResolutions.Sort((x, y) => x.pointsPerCoord.CompareTo(y.pointsPerCoord));

        currentRes = sortedResolutions[0];
    }

    private planetTerrainFolderInfo findDesiredResolution() {
        if (general.camera.fieldOfView < 17) return sortedResolutions[3];
        if (general.camera.fieldOfView < 30) return sortedResolutions[2];
        if (general.camera.fieldOfView < 45) return sortedResolutions[1];
        return sortedResolutions[0];
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

        return new HashSet<geographic>(points.Where(x => meshes[p].ContainsKey(x)));
    }
}

public class planetTerrainMeshCreator {
    private planetTerrainFolderInfo res;
    private geographic geo;
    private GameObject go;
    private CancellationTokenSource token;
    private Transform parent;
    private Dictionary<string, Dictionary<string, long[]>> savedPositions = new Dictionary<string, Dictionary<string, long[]>>();
    public readonly string name, filePath;
    public bool currentlyRunning {get; private set;} = false;
    public bool exists {get; private set;} = false;

    public planetTerrainMeshCreator(planetTerrainFolderInfo res, geographic geo, Transform parent, string path, Dictionary<string, Dictionary<string, long[]>> pos) {
        this.res = res;
        this.geo = geo;
        this.savedPositions = pos;
        this.parent = parent;
        this.filePath = path;
        this.name = Path.GetFileNameWithoutExtension(filePath);
        token = new CancellationTokenSource();

        if (File.Exists(this.filePath)) exists = true;
    }

    public Task generate(GameObject model, Material mat) {
        if (!exists) return null;
        if (currentlyRunning) {
            Debug.LogWarning("Trying to generate mesh that's currently generating.");
            return null;
        }

        currentlyRunning = true;

        if (go != null) {
            // we didnt destroy the go which means that its currently hiding
            go.GetComponent<MeshRenderer>().enabled = true;

            if (token.IsCancellationRequested) token = new CancellationTokenSource();
            currentlyRunning = false;
            return null;
        } else {
            Task t = Task.Run(async () => {
                if (token.IsCancellationRequested) return;
                deserialzedMeshData dmd = await MeshSerializer.quickDeserialize(filePath, savedPositions);
                if (token.IsCancellationRequested) return;

                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    if (token.IsCancellationRequested) return;
                    go = GameObject.Instantiate(model);
                    go.name = name;
                    go.transform.parent = parent;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localEulerAngles = Vector3.zero;
                    go.GetComponent<MeshRenderer>().material = mat;
                    Mesh m = dmd.generate();
                    go.GetComponent<MeshFilter>().mesh = m;
                    if (controller.useTerrainVisibility) go.GetComponent<MeshCollider>().sharedMesh = m;

                    // the meshes were saved with a master.scale of 1000, however the current scale may not match
                    // adjust the scale of the meshes so that it matches master.scale
                    float diff = 1000f / (float) master.scale;
                    go.transform.localScale *= diff;
                });
            });

            if (token.IsCancellationRequested) token = new CancellationTokenSource();

            currentlyRunning = false;

            return t;
        }
    }

    public void kill() {
        if (!exists) return;
        if (go != null) {
            GameObject.Destroy(go.GetComponent<MeshFilter>().sharedMesh);
            GameObject.Destroy(go);
        }
        token.Cancel();
    }
}
