using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;

// theres a lot of variation between for and foreach loops in this code, its bc im still wavering on which one i want to use

public class planetTerrain
{
    private Dictionary<string, planetTerrainFolderInfo> folderInfos = new Dictionary<string, planetTerrainFolderInfo>();
    private List<planetTerrainFolderInfo> sortedResolutions = new List<planetTerrainFolderInfo>();
    private Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>> existingMeshes = new Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>>();
    private Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>> hidingMeshes = new Dictionary<planetTerrainFolderInfo, Dictionary<geographic, meshContainer>>();
    private Dictionary<planetTerrainFolderInfo, List<geographic>> toIgnore = new Dictionary<planetTerrainFolderInfo, List<geographic>>();
    private List<planetTerrainFolderInfo> invincibleFolders = new List<planetTerrainFolderInfo>();
    public readonly double radius, heightMulti;
    public planet parent;
    public string materialPath;
    private bool invertMesh;

    public planetTerrain(double radius, double heightMulti, planet parent, string materialPath, bool invertMesh = false)
    {
        this.radius = radius;
        this.materialPath = materialPath;
        this.heightMulti = heightMulti;
        this.parent = parent;
        this.invertMesh = invertMesh;
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
    }

    public async void updateTerrain(bool force = false)
    {
        if (finishedRunning == false) return;
        finishedRunning = false;
        await _updateTerrain(force);
        finishedRunning = true;
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
            hidingMeshes[folderInfos[directoryName]][ptf.geoPosition] = new meshContainer(ptf, ptm);
        }
    }

    public void markInvincible(string name) {
        invincibleFolders.Add(folderInfos[name]);
    }

    const float moveThreshold = 1f, rotateThreshold = 3000, fRotateThreshold = 0.01f;
    const int tickThreshold = 60; // terrain can only update every x ticks
    position lastPlayerPos, lastRotation;
    Vector2 lastFRot;
    int lastTick = -1000;
    bool finishedRunning = true;
    // TODO: if the time step is too great, unload terrain and use sphere instead- we dont want to be constantly loading and unloading terrain
    private async Task _updateTerrain(bool force = false)
    {
        if (this.parent.representation.gameObject == null) return;

        double distToPlanet = Vector3.Distance(general.camera.transform.position, this.parent.representation.gameObject.transform.position);
        float planetZ = general.camera.WorldToScreenPoint(this.parent.representation.gameObject.transform.position).z;

        if (distToPlanet > 7 && !planetFocus.usePlanetFocus) {unloadTerrain(); return;}
        if (planetOverview.usePlanetOverview) {unloadTerrain(); return;}
        
        bool move = position.distance(master.currentPosition, lastPlayerPos) > moveThreshold;
        bool tick = master.currentTick - lastTick > tickThreshold;
        bool wait = master.currentTick - lastTick > tickThreshold * 3.0;
        position g = parent.geoOnPlanet(geographic.toGeographic(master.currentPosition - parent.pos, parent.radius), 0);
        bool rot = position.distance(g, lastRotation) > rotateThreshold;
        bool fRot = Vector2.Distance(lastFRot, planetFocus.rotation) > fRotateThreshold;

        // force bypasses all conditions
        // tick is necessary for basic conditions
        // move and rot are basic conditions
        if (((move || rot || fRot || wait) && tick) || force)
        {
            // load this folder
            // TODO
            planetTerrainFolderInfo p = sortedResolutions[0];

            // determine what meshes to load based on if we can see them or not
            List<geographic> desiredMeshes = new List<geographic>();
            int boundsCount = p.allBounds.Count;
            for (int i = 0; i < boundsCount; i++)
            {
                Bounds b = p.allBounds[i];
                List<geographic> edges = new List<geographic>() {
                    new geographic(b.min.y, b.min.x),
                    new geographic(b.max.y, b.max.x),
                    new geographic(b.min.y, b.max.x),
                    new geographic(b.max.y, b.min.x)};
                
                for (int j = 0; j < 4; j++)
                {
                    /*for some godforsaken reason raycasting just does not seem to fucking work no matter what i try
                    I have spent a total of 3 hours on this shit, and cannot get it to work so im writing my own detection algorithm instead
                    I even fucking copy pasted values from FacilityRepresentation.cs *that worked* but here and in other scripts they dont for some fucking reason
                    I give up, i dont ever want to see this shit again and holy fuck i have no idea why it doesnt work- the raycast visibly passes through the collider
                    and again, TESTING VALUES THAT WORK results in a failure
                    I cant even find anything online- everything they said I've already done
                    its so fucking retarded i want to die*/

                    Vector3 pos = (Vector3) ((parent.geoOnPlanet(edges[j], -200) + parent.pos - master.currentPosition - master.referenceFrame) / master.scale);
                    float z = general.camera.WorldToScreenPoint(pos).z;
                    if (z <= planetZ && z > 0) {
                        desiredMeshes.Add(new geographic(b.min.y, b.min.x));
                        break;
                    }
                }
            }

            // if we dont want to generate anything, quit
            if (desiredMeshes.Count == 0) return;

            geographic[] eMeshes = existingMeshes[p].Keys.ToArray();
            bool isInvincible = invincibleFolders.Contains(p);
            int ignoreCount = 0;

            for (int i = 0; i < eMeshes.Length; i++) {
                if (!desiredMeshes.Contains(eMeshes[i])) {
                    if (isInvincible) {
                        // invincible, hide mesh and data instead of destroying
                        hidingMeshes[p][eMeshes[i]] = existingMeshes[p][eMeshes[i]];
                        hidingMeshes[p][eMeshes[i]].ptm.hide();
                    } else {
                        if (existingMeshes[p][eMeshes[i]].ptf.preloaded) {
                            // we preloaded this file so dont actually destroy it, but still remove the mesh
                            hidingMeshes[p][eMeshes[i]] = new meshContainer(existingMeshes[p][eMeshes[i]].ptf, null);
                        }

                        existingMeshes[p][eMeshes[i]].ptm.clearMesh();
                    }

                    existingMeshes[p].Remove(eMeshes[i]);
                } else {
                    toIgnore[p].Add(eMeshes[i]);
                    ignoreCount++;
                }
            }

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
                        Debug.Log("passed 1");
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
                    Debug.Log("passed 2");
                }

                // this means weve already loaded the mesh
                if (ptf is null) continue;

                Task t = new Task(() => {
                    planetTerrainMesh ptm = new planetTerrainMesh(ptf, p, this, invertMesh);
                    ptf.generate(ptm);

                    emCopy.TryAdd(ptf, ptm);
                });
                toDraw.Add(t);
                t.Start();
                
            }
            await Task.WhenAll(toDraw);

            // dont generate all meshes at once, generate them one a time (to prevent freezing)
            // cannot thread mesh generation so this seems to be the best option
            foreach (planetTerrainMesh ptm in emCopy.Values) {
                ptm.drawMesh(materialPath);
                await Task.Delay(5); // TODO: maybe replace with value that is determined by how fast the terrain gens?
            }

            // copy emCopy over to existingMeshes
            foreach (KeyValuePair<planetTerrainFile, planetTerrainMesh> kvp in emCopy) existingMeshes[kvp.Key.ptfi][kvp.Key.geoPosition] = new meshContainer(kvp.Key, kvp.Value);

            // clear toIgnore
            foreach (planetTerrainFolderInfo ptfi in toIgnore.Keys) toIgnore[ptfi].Clear();

            lastPlayerPos = master.currentPosition;
            lastRotation = g;
            lastTick = master.currentTick;

            if (existingMeshes.Count > 0) parent.representation.forceHide = true;
            else parent.representation.forceHide = false;

            // fix issue where the program would detect the function finishing before it actually did
            await Task.Delay(1);
        }
    }

    public void unloadTerrain()
    {
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

public class jp2Metadata {
    public double ImageWidth, ImageLength, BitsPerSample, PhotometricInterpretation, StripOffsets, SamplesPerPixel, RowsPerStrip, StripByteCounts, XResolution, YResolution, PlanarConfiguration, ResolutionUnit, width, height, xll, yll;
    public List<double> ModelPixelScale, ModelTiePoint;
    public jp2MetadataGeoKeyDirectory GeoKeyDirectory;
}

public class jp2MetadataGeoKeyDirectory {
    public string version;
    public double numKeys;
    public List<jp2MetadataKey> keys;
}

public class jp2MetadataKey {
    public string id, value;
}
