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

        float batchCount = 5;
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

    List<double> resolutionPercents = quickSum(5, 3);
    private static List<double> quickSum(int count, double denom) {
        List<double> output = new List<double>();
        double value = 0;
        for (int i = 1; i <= count; i++) {
            value += (denom - 1) / (Math.Pow(denom, (double) i));
            output.Add(value);
        }

        return output;
    }

    private planetTerrainFolderInfo findDesiredResolution() {
        double minFov = 4;
        double maxFov = 75;
        // TODO: get rid of fov
        double percent = 1.0 - (general.camera.fieldOfView + minFov) / (maxFov + minFov);

        planetTerrainFolderInfo p = null;
        double closestRes = 100;
        for (int i = 0; i < resolutionPercents.Count; i++) {
            double dist = Math.Abs(percent - resolutionPercents[i]);
            if (dist < closestRes) {
                closestRes = dist;
                p = sortedResolutions[i];
            }
        }

        return p;
    }

    private List<Vector2> screenCorners = new List<Vector2>() {
        new Vector2(0, 0),
        new Vector2(Screen.width, 0),
        new Vector2(Screen.width, Screen.height),
        new Vector2(0, Screen.height)};

    private HashSet<geographic> findDesiredMeshes(planetTerrainFolderInfo p) {
        float planetZ = general.camera.WorldToScreenPoint(this.parent.representation.gameObject.transform.position).z;

        if (controller.useTerrainVisibility) Debug.LogWarning("Warning! You are trying to use visiblity with terrain. This forces us to load everything- be careful when increasing resolution!");

        HashSet<geographic> points = new HashSet<geographic>();
        for (int i = 0; i < p.allBounds.Count; i++) {
            Bounds b = p.allBounds[i];
            List<geographic> edges = new List<geographic>() {
                new geographic(b.min.y, b.min.x), // sw
                new geographic(b.max.y, b.max.x), // ne
                new geographic(b.min.y, b.max.x), // se
                new geographic(b.max.y, b.min.x)}; // nw

            if (controller.useTerrainVisibility) {
                points.Add(new geographic(b.min.y, b.min.x));
                continue;
            }
            
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
