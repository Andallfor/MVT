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
    Dictionary<string, planetTerrainFolderInfo> folderInfos = new Dictionary<string, planetTerrainFolderInfo>();
    Dictionary<planetTerrainFile, planetTerrainMesh> existingMeshes = new Dictionary<planetTerrainFile, planetTerrainMesh>();
    List<planetTerrainFolderInfo> sortedResolutions = new List<planetTerrainFolderInfo>();
    public readonly double radius, heightMulti;
    public planet parent;

    public planetTerrain(double radius, double heightMulti, planet parent)
    {
        this.radius = radius;
        this.heightMulti = heightMulti;
        this.parent = parent;
    }

    public void generateFolderInfos(string[] folders)
    {
        foreach (string folder in folders)
        {
            planetTerrainFolderInfo ptfi = new planetTerrainFolderInfo(folder);

            folderInfos[ptfi.name] = ptfi;
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

    readonly float moveThreshold = 1f, rotateThreshold = 3000, fRotateThreshold = 0.01f;
    readonly int tickThreshold = 60; // terrain can only update every x ticks
    position lastPlayerPos, lastRotation;
    Vector2 lastFRot;
    int lastTick = -1000;
    bool finishedRunning = true;
    // TODO: if the time step is too great, unload terrain and use sphere instead- we dont want to be constantly loading and unloading terrain
    private async Task _updateTerrain(bool force = false)
    {
        double distToPlanet = Vector3.Distance(general.camera.transform.position, this.parent.representation.gameObject.transform.position);
        float planetZ = general.camera.WorldToScreenPoint(this.parent.representation.gameObject.transform.position).z;

        if (distToPlanet > 35) {unloadTerrain(); return;}
        if (planetOverview.usePlanetOverview) {unloadTerrain(); return;}
        
        bool move = position.distance(master.currentPosition, lastPlayerPos) > moveThreshold;
        bool tick = master.currentTick - lastTick > tickThreshold;
        position g = parent.geoOnPlanet(geographic.toGeographic(master.currentPosition - parent.pos, parent.radius), 0);
        bool rot = position.distance(g, lastRotation) > rotateThreshold;
        bool fRot = Vector2.Distance(lastFRot, planetFocus.rotation) > fRotateThreshold;

        // force bypasses all conditions
        // tick is necessary for basic conditions
        // move and rot are basic conditions
        if (((move || rot || fRot) && tick) || force)
        {
            planetTerrainFolderInfo p = sortedResolutions[0];

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
                    if (z <= planetZ && z > 0)
                    {
                        desiredMeshes.Add(new geographic(b.min.y, b.min.x));
                        break;
                    }
                }
            }

            // if we dont want to generate anything, quit
            if (desiredMeshes.Count == 0) return;

            // get rid of all ptfs that are no longer seen
            int existingMeshesCount = existingMeshes.Count;
            List<planetTerrainFile> existingCopy = existingMeshes.Keys.ToList();
            List<geographic> toIgnore = new List<geographic>();
            for (int i = 0; i < existingMeshesCount; i++)
            {
                if (!desiredMeshes.Contains(existingCopy[i].geoPosition))
                {
                    existingMeshes[existingCopy[i]].clearMesh();
                    existingMeshes.Remove(existingCopy[i]);
                }
                else toIgnore.Add(existingCopy[i].geoPosition); // these meshes have already been generated so dont regen them
            }

            // if we arent going to make any changes, return
            if (toIgnore.Count == desiredMeshes.Count) return;

            // generate meshes
            ConcurrentDictionary<planetTerrainFile, planetTerrainMesh> emCopy = new ConcurrentDictionary<planetTerrainFile, planetTerrainMesh>();
            List<Task> toDraw = new List<Task>();
            int desiredCount = desiredMeshes.Count;
            for (int i = 0; i < desiredCount; i++)
            {
                if (toIgnore.Contains(desiredMeshes[i])) continue;

                planetTerrainFile ptf = new planetTerrainFile(Path.Combine(
                    p.folderPath,
                    terrainProcessor.fileName(desiredMeshes[i], p.increment)), p);

                // thread the generation of ptm because it requires a large amount of mem allocation, assign it afterwards
                Task t = new Task(() => {
                    planetTerrainMesh ptm = new planetTerrainMesh(ptf, p, this);
                    ptf.generate(ptm);

                    emCopy.TryAdd(ptf, ptm);
                });
                toDraw.Add(t);
                t.Start();
            }
            await Task.WhenAll(toDraw);

            // dont generate all meshes at once, generate them one a time (to prevent freezing)
            // cannot thread mesh generation so this seems to be the best option
            foreach (planetTerrainMesh ptm in emCopy.Values)
            {
                ptm.drawMesh();
                await Task.Delay(5); // TODO: maybe replace with value that is determined by how fast the terrain gens?
            }

            // copy emCopy over to existingMeshes
            foreach (KeyValuePair<planetTerrainFile, planetTerrainMesh> kvp in emCopy) existingMeshes[kvp.Key] = kvp.Value;

            lastPlayerPos = master.currentPosition;
            lastRotation = g;
            lastTick = master.currentTick;

            if (existingMeshes.Count > 0) parent.representation.forceHide = true;
            else parent.representation.forceHide = false;
        }
    }

    public void unloadTerrain()
    {
        parent.representation.forceHide = false;
        parent.representation.setPosition(parent.pos - master.currentPosition - master.referenceFrame);
        foreach (planetTerrainMesh ptm in existingMeshes.Values) ptm.clearMesh();

        existingMeshes = new Dictionary<planetTerrainFile, planetTerrainMesh>();
    }
}
