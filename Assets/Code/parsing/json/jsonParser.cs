using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

public static class jsonParser
{
    public static string downloadPath = KnownFolders.GetPath(KnownFolder.Downloads);
    public static List<jsonQueueStruct> queue = new List<jsonQueueStruct>();

    public static string serialize(planet p, string path)
    {
        string d = JsonConvert.SerializeObject(p.requestJsonFile());

        return writeFile(path, d, p.name, ".pln");
    }
 
    public static string serialize(satellite s, string path)
    {
        string d = JsonConvert.SerializeObject(s.requestJsonFile());

        return writeFile(path, d, s.name, ".sat");
    }

    
    public static string serialize(facility f, string path)
    {
        string d = JsonConvert.SerializeObject(f.requestJsonFile());

        return writeFile(path, d, f.name, ".fac");
    }

    public static string serialize(string path)
    {
        jsonSystemStruct jss = new jsonSystemStruct() {
            planets = new List<jsonPlanetStruct>(),
            satellites = new List<jsonSatelliteStruct>(),
            facilities = new List<jsonFacilityStruct>()};

        foreach (planet p in master.allPlanets) {if (p.name != master.sun.name) jss.planets.Add(p.requestJsonFile());}
        foreach (satellite s in master.allSatellites) jss.satellites.Add(s.requestJsonFile());
        foreach (facility f in master.allFacilities) jss.facilities.Add(f.requestJsonFile());

        return writeFile(path, JsonConvert.SerializeObject(jss), "system" + DateTime.Now.ToFileTime(), ".syt");
    }

    public static void deserialize(string path, jsonType jt)
    {
        string d = File.ReadAllText(path);

        _ds(d, jt);
    }

    public static void deserialize(TextAsset ta, jsonType jt)
    {
        _ds(ta.ToString(), jt);
    }

    private static void _ds(string d, jsonType jt)
    {
        if (jt == jsonType.planet) _dsPlanet(JsonConvert.DeserializeObject<jsonPlanetStruct>(d));
        else if (jt == jsonType.satellite) _dsSatellite(JsonConvert.DeserializeObject<jsonSatelliteStruct>(d));
        else if (jt == jsonType.facility) _dsFacility(JsonConvert.DeserializeObject<jsonFacilityStruct>(d));
        else if (jt == jsonType.system) _dsSystem(JsonConvert.DeserializeObject<jsonSystemStruct>(d));
    }

    // simplify
    private static void _dsPlanet(jsonPlanetStruct jps)
    {
        /*new planet(jps.name,
            //new planetData(
            //    jps.radius, jps.rotate, _dsTimeline(jps.positions), jps.positions.timestep, (planetType) jps.planetType), 
            new planetData(
                jps.radius, _dsTimeline(jps.positions), jps.positions.timestep, (planetType) jps.planetType), 
            new representationData(
                jps.representationData.modelPath, jps.representationData.materialPath));*/
        
        if (jps.bodyData.parent != "") addToQueue(new jsonQueueStruct(jps.bodyData.parent, jps.name));
        foreach (string c in jps.bodyData.children)
        {
            addToQueue(new jsonQueueStruct(jps.name, c));
        }
    }

    private static void _dsSatellite(jsonSatelliteStruct jss)
    {
        new satellite(jss.name, 
            new satelliteData(
                _dsTimeline(jss.positions)),
            new representationData(
                jss.representation.modelPath, jss.representation.materialPath));
        
        if (jss.bodyData.parent != "") addToQueue(new jsonQueueStruct(jss.bodyData.parent, jss.name));
        foreach (string c in jss.bodyData.children)
        {
            addToQueue(new jsonQueueStruct(jss.name, c));
        }
    }

    private static void _dsFacility(jsonFacilityStruct jfs)
    {
        bool parentExists = false;
        if (master.allPlanets.Exists(x => x.name == jfs.parent)) parentExists = true;

        List<antennaData> antennas = new List<antennaData>();
        foreach (jsonAntennaStruct jas in jfs.antennas) antennas.Add(_dsAntenna(jas));

        new facility(
            jfs.name, (parentExists) ? master.allPlanets.First(x => x.name == jfs.parent) : master.sun,
            new facilityData(
                jfs.name,
                _dsGeographic(jfs.geo),
                0,
                antennas
            ), 
            new representationData(jfs.representation.modelPath, jfs.representation.materialPath));
        
        if (!parentExists) addToQueue(new jsonQueueStruct(jfs.parent, jfs.name));
    }

    private static void _dsSystem(jsonSystemStruct jss)
    {
        foreach (jsonPlanetStruct jps in jss.planets) _dsPlanet(jps);
        foreach (jsonSatelliteStruct jsts in jss.satellites) _dsSatellite(jsts);
        foreach (jsonFacilityStruct jfs in jss.facilities) _dsFacility(jfs);
    }

    private static string writeFile(string path, string data, string name, string ending)
    {
        string p = Path.Combine(path, name + ending);
        File.WriteAllText(p, data);

        return p;
    }

    private static antennaData _dsAntenna(jsonAntennaStruct jas) => new antennaData(
        jas.payload,
        jas.groundStation,
        jas.name,
        jas.diameter,
        jas.freqBand,
        jas.centerFreq,
        _dsGeographic(jas.geo),
        jas.alt,
        jas.gPerT,
        jas.maxRate,
        jas.network,
        jas.priority
    );

    private static geographic _dsGeographic(jsonGeographicStruct jgs) => new geographic(jgs.lat, jgs.lon);

    private static Timeline _dsTimeline(jsonTimelineStruct jts)
    {
        Dictionary<double, position> d = new Dictionary<double, position>();

        foreach (KeyValuePair<double, jsonPositionStruct> kvp in jts.positions) d.Add(kvp.Key, _dsPosition(kvp.Value));

        return new Timeline(d, jts.timestep);
    }

    private static position _dsPosition(jsonPositionStruct jps) => new position(jps.x, jps.y, jps.z);

    private static void addToQueue(jsonQueueStruct jqs)
    {
        queue.Add(jqs);

        master.updateJsonQueue += updateQueue;
        updateQueue(null, EventArgs.Empty);
    }

    private static void updateQueue(object sender, EventArgs args)
    {
        int i = 0;
        bool noChanges = false;
        
        // run until no changes to the queue are made
        while (!noChanges)
        {
            noChanges = true;

            // todo: figure out better way to do this
            // because goddamn this is ugly
            List<jsonQueueStruct> queueNew = new List<jsonQueueStruct>();
            foreach (jsonQueueStruct jqs in queue)
            {
                bool pp, pc, sp, sc, fc; // fp cannot exist
                pp = master.allPlanets.Exists(x => x.name == jqs.parent);
                pc = master.allPlanets.Exists(x => x.name == jqs.child);
                sp = master.allSatellites.Exists(x => x.name == jqs.parent);
                sc = master.allSatellites.Exists(x => x.name == jqs.child);
                fc = master.allFacilities.Exists(x => x.name == jqs.child);

                if (pp && pc) body.addFamilyNode(master.allPlanets.First(x => x.name == jqs.parent), master.allPlanets.First(x => x.name == jqs.child));
                else if (pp && sc) body.addFamilyNode(master.allPlanets.First(x => x.name == jqs.parent), master.allSatellites.First(x => x.name == jqs.child));
                else if (pp && fc) master.allFacilities.First(x => x.name == jqs.child).setParent(master.allPlanets.First(x => x.name == jqs.parent));
                else if (sp && pc) body.addFamilyNode(master.allSatellites.First(x => x.name == jqs.parent), master.allPlanets.First(x => x.name == jqs.child));
                else if (sp && sc) body.addFamilyNode(master.allSatellites.First(x => x.name == jqs.parent), master.allSatellites.First(x => x.name == jqs.child));
                else if (noChanges)
                {
                    // find a better way
                    queueNew.Add(jqs);
                    continue;
                }

                noChanges = false;
            }

            queue = queueNew;
            i++;

            if (i > 100)
            {
                Debug.Log("inf loop idiot");
                break;
            }
        }
    }
}

public enum jsonType
{
    planet, satellite, facility, system
}


public struct jsonSystemStruct
{
    public List<jsonPlanetStruct> planets;
    public List<jsonFacilityStruct> facilities;
    public List<jsonSatelliteStruct> satellites;
    
}
public class jsonQueueStruct
{
    public string parent, child;
    public jsonQueueStruct(string parent, string child)
    {
        this.parent = parent;
        this.child = child;
    }
}
public readonly struct jsonPath
{
    public readonly TextAsset data;

    public jsonPath(string path)
    {
        // do testing
        data = Resources.Load(path) as TextAsset;
    }

    public jsonPath(TextAsset ta)
    {
        data = ta;
    }
}
