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

public class planetTerrain
{
    private Dictionary<string, planetTerrainFolderInfo> folderInfos = new Dictionary<string, planetTerrainFolderInfo>();
    private List<planetTerrainFolderInfo> sortedResolutions = new List<planetTerrainFolderInfo>();
    private Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>> existingMeshes = new Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>>();
    private Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>> hidingMeshes = new Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>>();
    private Dictionary<planetTerrainFolderInfo, List<geographic>> toIgnore = new Dictionary<planetTerrainFolderInfo, List<geographic>>();
    private List<meshContainer> placeholderMeshes = new List<meshContainer>();
    private List<planetTerrainFolderInfo> invincibleFolders = new List<planetTerrainFolderInfo>();
    private ConcurrentBag<CancellationTokenSource> runningTasks = new ConcurrentBag<CancellationTokenSource>();
    private planetTerrainFolderInfo designatedPlaceholder = null;
    public readonly double radius, heightMulti;
    public planet parent;
    public string materialPath;
    private bool invertMesh;
    private terrainUtility terrainUtil;

    private int mainThreadId;
    public planetTerrain(double radius, double heightMulti, planet parent, string materialPath, bool invertMesh = false)
    {
        this.radius = radius;
        this.materialPath = materialPath;
        this.heightMulti = heightMulti;
        this.parent = parent;
        this.invertMesh = invertMesh;

        this.terrainUtil = new terrainUtility(parent);

        mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
    }

    public void generateFolderInfos(string[] folders)
    {
        foreach (string folder in folders)
        {
            planetTerrainFolderInfo ptfi = new planetTerrainFolderInfo(folder);

            folderInfos[ptfi.name] = ptfi;
            existingMeshes[ptfi] = new Dictionary<geographic, meshContainer>();
            hidingMeshes[ptfi] = new Dictionary<geographic, meshContainer>();
            toIgnore[ptfi] = new List<geographic>();
        }

        sortedResolutions = folderInfos.Values.ToList();
        sortedResolutions.Sort((x, y) => x.pointsPerCoord.CompareTo(y.pointsPerCoord));

        lastP = sortedResolutions[0];
    }

    public void preload(string folder, terrainFileType tft) {
        List<string> files = new List<string>();
        foreach (string file in Directory.EnumerateFiles(folder)) {
            if (file.Contains("resInfo") || file.Contains("boundary")) continue;
            if (!(file.EndsWith("npy") || file.EndsWith("txt"))) continue;

            files.Add(file);
        }

        preload(files, tft);
    }

    public void preload(List<string> files, terrainFileType tft) {
        string directoryName = new DirectoryInfo(Path.GetDirectoryName(files[0])).Name;

        if (!folderInfos.ContainsKey(directoryName)) {
            Debug.LogWarning("Unable to find respective folder info");
            return;
        }

        foreach (string file in files) {
            planetTerrainFile ptf = new planetTerrainFile(file, folderInfos[directoryName], tft);
            ptf.preload();
            planetTerrainMesh ptm = new planetTerrainMesh(ptf, folderInfos[directoryName], this, invertMesh);
            ptf.generate(ptm);
            ptm.drawMesh(materialPath);
            ptm.hide();
            meshContainer mc = new meshContainer(ptf, ptm);
            hidingMeshes[folderInfos[directoryName]][ptf.geoPosition] = mc;
            placeholderMeshes.Add(mc);
        }
    }

    public void markInvincible(string name) {
        invincibleFolders.Add(folderInfos[name]);
        designatedPlaceholder = folderInfos[name];
    }

    private List<Vector2> screenCorners = new List<Vector2>() {
        new Vector2(0, 0),
        new Vector2(Screen.width, 0),
        new Vector2(Screen.width, Screen.height),
        new Vector2(0, Screen.height)};
    
    private void finishMeshCleanup(bool clearPlaceholders) {
        foreach (planetTerrainMesh ptm in toKill) ptm.clearMesh();
        foreach (planetTerrainMesh ptm in toHide) ptm.hide();
        if (clearPlaceholders) {
            foreach (meshContainer mc in placeholderMeshes) mc.ptm.hide();
        }

        toKill = new List<planetTerrainMesh>();
        toHide = new List<planetTerrainMesh>();
    }

    private bool finishedRunning = true;
    private planetTerrainFolderInfo lastP;
    public async void updateTerrain(bool force = false)
    {
        if (this.parent.representation.gameObject == null) return;
        if (planetOverview.usePlanetOverview) {unloadTerrain(); return;}
        double distToPlanet = Vector3.Distance(general.camera.transform.position, this.parent.representation.gameObject.transform.position);
        if (distToPlanet > 7 && !planetFocus.usePlanetFocus) {unloadTerrain(); return;}

        bool shouldClearPlaceholder = false;
        planetTerrainFolderInfo desiredP = findDesiredResolution();
        // TODO: add culling when moving around, nto just adjusting res
        if (desiredP != lastP) {
            // resolution changed, unload previous
            // mark previous meshes for cleanup
            cleanupMeshes(lastP, new List<geographic>(), ref toKill, ref toHide);
            // stop previous generation attempt
            foreach (CancellationTokenSource token in runningTasks) token.Cancel();
            runningTasks = new ConcurrentBag<CancellationTokenSource>();

            // if the new resolution is greater then the previous one (zooming out), then load in placeholder meshes to cover the holes
            if (desiredP.pointsPerCoord < lastP.pointsPerCoord && desiredP != sortedResolutions[0]) {
                foreach (meshContainer mc in placeholderMeshes) mc.ptm.show();
                shouldClearPlaceholder = true;
            }

            lastP = desiredP;
            force = true;
        }

        if (force || (terrainUtil.canRun() && finishedRunning)) {
            finishedRunning = false;
            await _updateTerrain(desiredP, force);
            finishMeshCleanup(shouldClearPlaceholder);
            finishedRunning = true;
        }
    }

    private List<planetTerrainMesh> toKill = new List<planetTerrainMesh>(), toHide = new List<planetTerrainMesh>();
    // TODO: if the time step is too great, unload terrain and use sphere instead- we dont want to be constantly loading and unloading terrain
    private async Task _updateTerrain(planetTerrainFolderInfo p, bool force = false)
    {
        float planetZ = general.camera.WorldToScreenPoint(this.parent.representation.gameObject.transform.position).z;

        Dictionary<planetTerrainFolderInfo, List<geographic>> allDesiredMeshes = new Dictionary<planetTerrainFolderInfo, List<geographic>>();
        foreach (planetTerrainFolderInfo ptfi in folderInfos.Values) allDesiredMeshes[ptfi] = new List<geographic>();

        List<geographic> desiredMeshes = findDesiredMeshes(planetZ);

        // if we dont want to generate anything, quit
        if (desiredMeshes.Count == 0 && existingMeshes[p].Count == 0) return;

        int ignoreCount = cleanupMeshes(p, desiredMeshes, ref toKill, ref toHide);

        // if we arent going to make any changes, return
        if (ignoreCount == desiredMeshes.Count) return;

        // generate meshes
        ConcurrentDictionary<planetTerrainFile, planetTerrainMesh> emCopy = new ConcurrentDictionary<planetTerrainFile, planetTerrainMesh>();
        List<Task> toDraw = new List<Task>();
        int desiredCount = desiredMeshes.Count;
        for (int i = 0; i < desiredCount; i++) {
            if (toIgnore[p].Contains(desiredMeshes[i])) continue;

            planetTerrainFile ptf = null;
            if (hidingMeshes[p].ContainsKey(desiredMeshes[i])) {
                meshContainer mc = hidingMeshes[p][desiredMeshes[i]];

                // we already have the mesh saved, so just show it
                if (!(mc.ptm is null)) {
                    mc.ptm.show();
                    existingMeshes[p][desiredMeshes[i]] = mc;
                    hidingMeshes[p].Remove(desiredMeshes[i]);
                } else {
                    // we didnt save the mesh, only the file
                    ptf = mc.ptf;
                }
            } else {
                // generate file normally
                string predictedFileName = terrainProcessor.fileName(
                    desiredMeshes[i],
                    p.increment,
                    p.type == terrainFileType.npy ? "npy" : "txt");
                string predictedPath = Path.Combine(
                    p.folderPath,
                    predictedFileName);
                
                if (!File.Exists(predictedPath)) continue;

                ptf = new planetTerrainFile(predictedPath, p, p.type);
            }

            // this means weve already loaded the mesh
            if (ptf is null) continue;

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task t = new Task(() => {
                planetTerrainMesh ptm = new planetTerrainMesh(ptf, p, this, invertMesh);
                if (token.IsCancellationRequested) return;
                ptf.generate(ptm);

                if (token.IsCancellationRequested) return;
                emCopy.TryAdd(ptf, ptm);
            });
            Task tt = t.ContinueWith((task) => {
                if (token.IsCancellationRequested) return;
                emCopy[ptf].drawMesh(materialPath);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            runningTasks.Add(tokenSource);
            toDraw.Add(tt);
            t.Start();
            
        }
        await Task.WhenAll(toDraw);

        // copy emCopy over to existingMeshes
        foreach (KeyValuePair<planetTerrainFile, planetTerrainMesh> kvp in emCopy) existingMeshes[kvp.Key.ptfi][kvp.Key.geoPosition] = new meshContainer(kvp.Key, kvp.Value);

        // clear toIgnore
        toIgnore[p].Clear();

        if (terrainLoaded()) parent.representation.forceHide = true;
        else parent.representation.forceHide = false;

        // fix issue where the program would detect the function finishing before it actually did
        await Task.Delay(1);
    }

    private int cleanupMeshes(planetTerrainFolderInfo p, List<geographic> desiredMeshes, ref List<planetTerrainMesh> toClear, ref List<planetTerrainMesh> toHide) {
        int ignoreCount = 0;
        geographic[] eMeshes = existingMeshes[p].Keys.ToArray();
        bool isInvincible = invincibleFolders.Contains(p);

        for (int i = 0; i < eMeshes.Length; i++) {
            if (!desiredMeshes.Contains(eMeshes[i])) {
                //Debug.Log("condition 1");
                if (isInvincible) {
                    // invincible, hide mesh and data instead of destroying
                    hidingMeshes[p][eMeshes[i]] = existingMeshes[p][eMeshes[i]];
                    toHide.Add(hidingMeshes[p][eMeshes[i]].ptm);
                } else {
                    if (existingMeshes[p][eMeshes[i]].ptf.preloaded) {
                        // we preloaded this file so dont actually destroy it, but still remove the mesh
                        hidingMeshes[p][eMeshes[i]] = new meshContainer(existingMeshes[p][eMeshes[i]].ptf, null);
                    }

                    toClear.Add(existingMeshes[p][eMeshes[i]].ptm);
                }

                existingMeshes[p].Remove(eMeshes[i]);
            } else {
                //Debug.Log("condition 2");
                toIgnore[p].Add(eMeshes[i]);
                ignoreCount++;
            }
        }

        return ignoreCount;
    }

    List<double> resolutionPercents = quickSum(5, 4);
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
        double minFov = 0.2;
        double maxFov = 75;
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

    private List<geographic> findDesiredMeshes(float planetZ) {
        planetTerrainFolderInfo p = findDesiredResolution();

        List<geographic> points = new List<geographic>();
        for (int i = 0; i < p.allBounds.Count; i++) {
            Bounds b = p.allBounds[i];
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

    public void unloadTerrain() {
        if (!terrainLoaded()) return;

        parent.representation.forceHide = false;
        parent.representation.setPosition(parent.pos - master.currentPosition - master.referenceFrame);
        foreach (KeyValuePair<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>> kvp in existingMeshes) {
            if (invincibleFolders.Contains(kvp.Key)) {
                //  copy to hiding, and dont clear
                hidingMeshes[kvp.Key] = new Dictionary<geographic, meshContainer>(kvp.Value);
            } else {
                foreach (KeyValuePair<geographic, meshContainer> _kvp in kvp.Value) {
                    if (_kvp.Value.ptf.preloaded) {
                        // preloaded, dont delete
                        hidingMeshes[kvp.Key][_kvp.Key] = new meshContainer(_kvp.Value.ptf, null);
                    } else _kvp.Value.ptm.clearMesh();
                }
            }
        }

        existingMeshes = new Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>>();
        foreach (planetTerrainFolderInfo p in folderInfos.Values) existingMeshes[p] = new Dictionary<geographic, meshContainer>();
    }

    public bool terrainLoaded() => existingMeshes.Any(x => x.Value.Values.Count > 0);
}

internal struct meshContainer {
    public planetTerrainFile ptf;
    public planetTerrainMesh ptm;

    public meshContainer(planetTerrainFile ptf, planetTerrainMesh ptm) {
        this.ptf = ptf;
        this.ptm = ptm;
    }
}
