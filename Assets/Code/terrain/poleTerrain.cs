using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using NumSharp;
using System.Threading.Tasks;
using UnityEditor;
using System.Diagnostics;
using B83.MeshTools;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;


public class poleTerrain {
    private List<GameObject> meshes = new List<GameObject>();
    private List<int> scales = new List<int>();
    private Dictionary<int, string> scaleKey = new Dictionary<int, string>();
    private int currentScaleIndex = 0;
    private Transform parent;
    private GameObject model;
    private Material mat;
    public bool currentlyDrawing = false;

    public static Dictionary<string, Dictionary<string, long[]>> savedPositions = new Dictionary<string, Dictionary<string, long[]>>();

    public poleTerrain(Dictionary<int, string> scaleKey, Transform parent) {
        this.scaleKey = scaleKey;
        this.parent = parent;
        this.scales = scaleKey.Keys.ToList();
        scales.Sort();
        scales.Reverse();

        // TODO: why are we insatntiating here?
        model = GameObject.Instantiate(Resources.Load("Prefabs/PlanetMesh") as GameObject);
        mat = Resources.Load("Materials/planets/moon/lunarSouthPole") as Material;

        //saveMeshes(20, "C:/Users/leozw/Desktop/poles/100m", "C:/Users/leozw/Desktop/terrain/polesBinary/100m");
        //saveMeshes(10, "C:/Users/leozw/Desktop/poles/50m", "C:/Users/leozw/Desktop/terrain/polesBinary/50m");
        //saveMeshes(5, "C:/Users/leozw/Desktop/poles/25m", "C:/Users/leozw/Desktop/terrain/polesBinary/25m");
    }

    private async void generateScale(int scale) {
        currentlyDrawing = true;
        savedPositions = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, long[]>>>(
            File.ReadAllText(Path.Combine(scaleKey[scale], "data.json")));
        
        int totalFiles = 0;
        Queue<string> files = new Queue<string>();

        foreach (string file in Directory.EnumerateFiles(scaleKey[scale])) {
            if (Path.GetExtension(file) != ".trn") continue;

            files.Enqueue(file);
            totalFiles++;
        }

        // load data in batches
        float batchCount = 2;
        for (int i = 0; i < (float) totalFiles / batchCount; i++) {
            List<Task> tasks = new List<Task>();

            for (int j = 0; j < batchCount; j++) {
                if (files.Count == 0) break;

                string file = files.Dequeue();

                tasks.Add(Task.Run(async () => {
                    poleTerrainFile ptf = new poleTerrainFile(file, scale);
                    deserialzedMeshData dmd = await ptf.load();

                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        GameObject go = GameObject.Instantiate(model);
                        go.name = "a";
                        go.transform.parent = parent;
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.GetComponent<MeshRenderer>().material = mat;
                        go.GetComponent<MeshFilter>().mesh = dmd.generate();

                        // the meshes were saved with a master.scale of 1000, however the current scale may not match
                        // adjust the scale of the meshes so that it matches master.scale
                        float diff = 1000f / (float) master.scale;
                        go.transform.localScale *= diff;

                        meshes.Add(go);
                    });
                }));
            }

            await Task.WhenAll(tasks);
        }

        currentlyDrawing = false;
    }

    private void saveMeshes(int scale, string srcFolder, string output) {
        foreach (string file in Directory.EnumerateFiles(srcFolder)) {
            if (Path.GetExtension(file) != ".npy") continue;
            
            poleTerrainFile ptf = new poleTerrainFile(file, scale);
            ptf.saveMesh(Path.Combine(output, $"{ptf.name}.trn"));
        }

        File.WriteAllText(Path.Combine(output, "data.json"), JsonConvert.SerializeObject(savedPositions));
    }

    public void decreaseScale() {
        if (currentScaleIndex == 0 || currentlyDrawing) return;
        clear();
        currentScaleIndex--;
        generateScale(scales[currentScaleIndex]);
    }

    public void increaseScale() {
        if (currentScaleIndex == scales.Count - 1 || currentlyDrawing) return;
        clear();
        currentScaleIndex++;
        generateScale(scales[currentScaleIndex]);
    }

    public void genMinScale() {
        if (currentlyDrawing) return;
        clear();
        currentScaleIndex = 0;
        generateScale(scales[currentScaleIndex]);
    }

    public void clear() {
        foreach (GameObject go in meshes) {
            GameObject.Destroy(go.GetComponent<MeshFilter>().sharedMesh);
            GameObject.Destroy(go);
        }
        meshes = new List<GameObject>();
    }
}

public class poleTerrainFile {
    // top left from view of python
    private position pos, size;
    private geographic geo;
    public string name, filePath;
    private double scale;

    private const double circumference = 10_917; // km
    private double maxSize = 40_000;
    private GameObject go;

    public poleTerrainFile(string file, double scale) {
        this.filePath = file;
        this.name = Path.GetFileNameWithoutExtension(file);
        this.scale = scale;

        string p = name.Split('_').First();
        string s = name.Split('_').Last();

        this.pos = new position(
            double.Parse(p.Split('x').First()),
            double.Parse(p.Split('x').Last()), 0);
        
        this.size = new position(
            double.Parse(s.Split('x').Last()),
            double.Parse(s.Split('x').First()), 0);
        
        this.geo = cartToGeo(this.pos);
    }

    //public deserialzedMeshData load() => MeshSerializer.DeserializeMesh(File.ReadAllBytes(this.filePath));
    public async Task<deserialzedMeshData> load() => await MeshSerializer.quickDeserialize(this.filePath, poleTerrain.savedPositions);

    public IEnumerator draw(deserialzedMeshData md, GameObject model, Material mat) {
        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = null;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        go.GetComponent<MeshRenderer>().material = mat;
        Mesh m = md.generate();
        go.GetComponent<MeshFilter>().mesh = m;

        yield return null;
    }

    public void clear() {GameObject.Destroy(go);}

    public void saveMesh(string path) {
        NDArray data = np.load(filePath);
        poleTerrainMesh ptm = new poleTerrainMesh(
            data.shape[1], data.shape[0],
            new position(pos.x / scale, pos.y / scale, 0), new position(maxSize / scale, maxSize / scale, 1), true,
            (p) => new Vector2(
                ((float) p.x * (float) scale) / 40_000f,
                (40_000f - (float) p.y * (float) scale) / 40_000f
            ));

        for (int y = 0; y < data.shape[0]; y++) {
            for (int x = 0; x < data.shape[1]; x++) {
                ptm.addPoint(x, y, cartToGeo(new position(x * scale, y * scale, 0) + pos), (float) data[y, x]);
            }
        }

        // dont question it
        GameObject go = ptm.drawMesh("Materials/default", "a", null);

        File.WriteAllBytes(path, MeshSerializer.SerializeMesh(go.GetComponent<MeshFilter>().mesh, name, ref poleTerrain.savedPositions));

        GameObject.Destroy(go);
    }

    private geographic cartToGeo(position p) {
        // https://www.lpi.usra.edu/lunar/lunar-south-pole-atlas/maps/SPole_80S_LOLA-PSR_v20190515.pdf reference for lat lon
        double maxDist = Math.Sqrt(maxSize * maxSize / 4.0 + maxSize * maxSize / 4.0); // pythag theorem
        double maxKmChange = Math.Sqrt(100.0 * 100.0 + 100.0 * 100.0);
        double latKmChange = (position.distance(p, new position(maxSize / 2.0, maxSize / 2.0, 0)) / maxDist) * maxKmChange;
        double latGeoChange = -latKmChange / (circumference / 2.0) * 180.0;

        double lon = Math.Atan2(p.x - maxSize / 2.0, p.y - maxSize / 2.0) * 180.0 / Math.PI; // (x, y) is intentional -> aligns with lon (plot in desmos)
        
        return new geographic(90.0 + latGeoChange, lon);
    }
}

public class poleTerrainMesh : IMesh {
    public poleTerrainMesh(int width, int height, position ll, position max, bool reverse, Func<Vector2Int, Vector2> customUV) {
        base.init(width, height, ll, max, reverse, customUV: customUV);
    }

    // y is ignored
    public override Vector3 addPoint(int x, int y, geographic g, double h) {
        Vector3 v = (Vector3) (g.toCartesian(1737.4 + h / 1000.0).swapAxis() / master.scale);
        this.verts[toIndex(x, y)] = v;
        
        return v;
    }

    public GameObject drawMesh(string materialPath, string name, Transform parent)
    {
        return base.drawMesh(Resources.Load(materialPath) as Material,
                             Resources.Load("Prefabs/PlanetMesh") as GameObject,
                             name, parent);
    }
}
