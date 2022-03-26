using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

    public void generateArea(Bounds area, string name, bool forceHideBody = false)
    {
        planetTerrainFolderInfo ptfi = folderInfos[name];

        // get all files that intersect the desired area
        List<geographic> intersections = new List<geographic>();
        for (double lat = -90; lat < 90; lat += ptfi.increment.lat)
        {
            for (double lon = -180; lon < 180; lon += ptfi.increment.lon)
            {
                Bounds b = new Bounds(new Vector3((float) lon, (float) lat, 0), new Vector3((float) ptfi.increment.lon, (float) ptfi.increment.lat, 1));

                if (area.Intersects(b)) intersections.Add(new geographic(lat, lon));
            }
        }

        // generate each file
        foreach (geographic intersection in intersections)
        {
            planetTerrainFile ptf = new planetTerrainFile(Path.Combine(
                ptfi.folderPath,
                terrainProcessor.fileName(intersection, ptfi.increment)), ptfi);
            existingMeshes[ptf] = new planetTerrainMesh(ptf, ptfi, this);

            ptf.generate(existingMeshes[ptf]);
            existingMeshes[ptf].drawMesh();
        }

        if (forceHideBody || intersections.Count > 0) parent.representation.forceHide = true;
        else parent.representation.forceHide = false;
    }

    public async void updateTerrain(bool force = false)
    {
        if (finishedRunning == false) return;
        finishedRunning = false;
        await _updateTerrain(force);
        finishedRunning = true;
    }

    readonly float moveThreshold = 1f, rotateThreshold = 5;
    readonly int tickThreshold = 60; // terrain can only update every x ticks
    position lastPlayerPos;
    double lastRotation;
    int lastTick = -1000;
    bool finishedRunning = true;
    // TODO: if the time step is too great, unload terrain and use sphere instead- we dont want to be constantly loading and unloading terrain
    private async Task _updateTerrain(bool force = false)
    {
        double distToPlanet = Vector3.Distance(general.camera.transform.position, this.parent.representation.transform.position);
        float planetZ = general.camera.WorldToScreenPoint(this.parent.representation.transform.position).z;
        //Debug.Log(Physics.Linecast(general.camera.transform.position, this.parent.representation.transform.position));

        if (distToPlanet > 35) {unloadTerrain(); return;}
        if (planetOverview.usePlanetOverview) {unloadTerrain(); return;}
        //if (general.camera.WorldToScreenPoint(parent.representation.transform.position).z < 0) {unloadTerrain(); return;}
        // bug with terrain generating in wrong places is because we dont account for the earth changing position when we parent the mesh to earth

        if (((position.distance(master.currentPosition, lastPlayerPos) > moveThreshold ||
            parent.representation.gameObject.transform.eulerAngles.y - lastRotation > rotateThreshold) &&
            master.currentTick - lastTick > tickThreshold) || 
            force)
        {
            lastPlayerPos = master.currentPosition;
            lastRotation = parent.representation.gameObject.transform.eulerAngles.y;
            lastTick = master.currentTick;

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

            // generate meshes
            List<Task> toDraw = new List<Task>();
            List<planetTerrainMesh> ptms = new List<planetTerrainMesh>();
            int desiredCount = desiredMeshes.Count;
            for (int i = 0; i < desiredCount; i++)
            {
                if (toIgnore.Contains(desiredMeshes[i])) continue;

                planetTerrainFile ptf = new planetTerrainFile(Path.Combine(
                    p.folderPath,
                    terrainProcessor.fileName(desiredMeshes[i], p.increment)), p);
                
                existingMeshes[ptf] = new planetTerrainMesh(ptf, p, this);
                Task t = new Task(() => {ptf.generate(existingMeshes[ptf]);});
                toDraw.Add(t);
                ptms.Add(existingMeshes[ptf]);
                t.Start();
            }
            //System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
            //st.Start();
            await Task.WhenAll(toDraw);
            //Debug.Log(st.ElapsedMilliseconds);
            foreach (planetTerrainMesh ptm in ptms)
            {
                ptm.drawMesh();
            }
            //Debug.Log(st.ElapsedMilliseconds);
            //st.Stop();

            if (existingMeshes.Count > 0) parent.representation.forceHide = true;
            else parent.representation.forceHide = false;
        }
    }

    public void unloadTerrain()
    {
        parent.representation.forceHide = false;
        master.requestPositionUpdate();
        foreach (planetTerrainMesh ptm in existingMeshes.Values) ptm.clearMesh();

        existingMeshes = new Dictionary<planetTerrainFile, planetTerrainMesh>();
    }
}
