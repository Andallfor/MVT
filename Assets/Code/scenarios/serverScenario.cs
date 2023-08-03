using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;
using System.IO;

public class serverScenario : IScenario {
    private static bool done = false;

    protected override IEnumerator _generate() {
        done = false;

        yield return new WaitForSeconds(5f); // TODO: web.initialize doesnt seem to be blocking
        web.sendMessage((byte) userWebHandles.requestServerScenario, new byte[0]);

        double elapsedTime = 0;

        while (!done) {
            Debug.Log("Loading scenario...");
            yield return new WaitForSeconds(0.5f);

            elapsedTime += 0.5f;

            if (elapsedTime % 30 == 0) {
                web.sendMessage((byte) userWebHandles.requestServerScenario, new byte[0]);
            }

            if (elapsedTime % 200 == 0) {
                throw new Exception("Unable to download scenario from server");
            }
        }

        Debug.Log("outside of done loop");

        metadata.importantBodies = new Dictionary<string, body>();
        metadata.importantBodies["Earth"] = master.allPlanets.Find(x => x.name == "Earth");
        metadata.importantBodies["Luna"] = master.allPlanets.Find(x => x.name == "Luna");
        metadata.timeStart = 2461021.5;

        Debug.Log("returning from generate loop");

        yield return null;
    }

    public static byte[] serializeScenario() {
        // can only serialize kepler based objects

        // num planets (byte) | planet data (planets then moons)
        // num satellites (byte) | satellite data
        // num facilities (byte) | facility data

        // planet format
        // name (string) | radius (double) | rot type (byte) |
        // timeline kepler
        // planetType (byte) | texture (string)
        // parent (string)
        byte[] planets = serializePlanets();

        // satellite format
        // name (string) | timeline kepler | parent (string)
        byte[] satellites = serializeSatellites();

        // facility format
        // name (string) | lat (double) | lon (double) | alt (double) | parent (string) | startTime (double) | endTime (double)
        byte[] facilities = serializeFacilities();

        byte[] data = planets.Concat(satellites).Concat(facilities).ToArray();

        return data;
    }

    private static byte[] serializeFacilities() {
        byte numFacs = 0;

        byte[] data;
        using (MemoryStream ms = new MemoryStream()) {
            using (BinaryWriter bw = new BinaryWriter(ms)) {
                bw.Write((byte) 0); // holder for numFacs

                foreach (facility f in master.allFacilities) {
                    if (f.parent.positions.selection == TimelineSelection.positions) continue;

                    bw.Write(f.name);
                    bw.Write(f.data.geo.lat);
                    bw.Write(f.data.geo.lon);
                    bw.Write(f.data.alt);
                    bw.Write(f.parent.name);
                    bw.Write(f.data.start.julian);
                    bw.Write(f.data.end.julian);

                    numFacs++;
                }
            }

            data = ms.ToArray();
            data[0] = numFacs;
        }

        return data;
    }

    private static byte[] serializeSatellites() {
        byte numSats = 0;

        byte[] data;
        using (MemoryStream ms = new MemoryStream()) {
            using (BinaryWriter bw = new BinaryWriter(ms)) {
                bw.Write((byte) 0); // holder for numSats

                foreach (satellite s in master.allSatellites) {
                    if (s.positions.selection == TimelineSelection.positions) continue;
                    if (s.parent.positions.selection == TimelineSelection.positions) continue;

                    bw.Write(s.name);
                    serializeTimeline(bw, (TimelineKepler) s.positions.getSource());
                    bw.Write(s.parent.name);

                    numSats++;
                }
            }

            data = ms.ToArray();
            data[0] = numSats;
        }

        return data;
    }

    private static byte[] serializePlanets() {
        byte numPlanets = 0;

        // planets should come before moons
        List<planet> planets = new List<planet>(master.allPlanets);
        planets.Sort((a, b) => {
            if (a.pType == b.pType) return 0;
            if (a.pType == planetType.planet && b.pType == planetType.moon) return -1;
            return 1;
        });

        byte[] data;
        using (MemoryStream ms = new MemoryStream()) {
            using (BinaryWriter bw = new BinaryWriter(ms)) {
                bw.Write((byte) 0); // holder for numPlanets

                foreach (planet p in planets) {
                    if (p.positions.selection == TimelineSelection.positions) continue;
                    if (p.GetHashCode() == master.sun.GetHashCode()) continue; // sun is auto created no matter the scenario, dont bother sending it    

                    bw.Write(p.name);
                    bw.Write(p.radius);
                    bw.Write((byte) p.getRotationType());
                    
                    serializeTimeline(bw, (TimelineKepler) (p.positions.getSource()));

                    bw.Write((byte) p.pType);
                    bw.Write(p.representation.data.materialPath);
                    bw.Write(p.parent == null ? master.sun.name : p.parent.name);

                    numPlanets++;
                }
            }

            data = ms.ToArray();
            data[0] = numPlanets;
        }

        return data;
    }

    public static void deserializeScenario(byte[] data) {
        using (BinaryReader br = new BinaryReader(new MemoryStream(data))) {
            deserializePlanets(br);
            deserializeSatellites(br);
            deserializeFacilities(br);
        }

        done = true;
    }

    private static void deserializePlanets(BinaryReader br) {
        Debug.Log("trying to read");
        byte numPlanets = br.ReadByte();
        for (int i = 0; i < numPlanets; i++) {
            string name = br.ReadString();
            double radius = br.ReadDouble();
            rotationType rotType = (rotationType) br.ReadByte();
            Timeline t = deserializeTimeline(br);
            planetType pType = (planetType) br.ReadByte();
            string texture = br.ReadString();
            string parent = br.ReadString();

            new planet(name, new  planetData(radius, rotType, t, pType), new representationData("planet", texture), master.allPlanets.Find(x => x.name == parent));
        }

        Debug.Log("end");
    }

    private static void deserializeSatellites(BinaryReader br) {
        byte numSats = br.ReadByte();

        for (int i = 0; i < numSats; i++) {
            string name = br.ReadString();
            Timeline t = deserializeTimeline(br);
            string parent = br.ReadString();

            new satellite(name, new satelliteData(t), new representationData("planet", "defaultMat"), master.allPlanets.Find(x => x.name == parent));
        }
    }

    private static void deserializeFacilities(BinaryReader br) {
        byte numFacs = br.ReadByte();

        for (int i = 0; i < numFacs; i++) {
            string name = br.ReadString();
            double lat = br.ReadDouble();
            double lon = br.ReadDouble();
            double alt = br.ReadDouble();
            string parent = br.ReadString();
            double startTime = br.ReadDouble();
            double endTime = br.ReadDouble();

            new facility(name, master.allPlanets.Find(x => x.name == parent), new facilityData(name, new geographic(lat, lon), alt, new List<antennaData>(), new Time(startTime), new Time(endTime)), new representationData("facility", "defaultMat"));
        }
    }

    private static void serializeTimeline(BinaryWriter bw, TimelineKepler t) {
        double radToDeg = 180.0 / Math.PI;
        bw.Write(t.semiMajorAxis);
        bw.Write(t.eccentricity);
        bw.Write(t.inclination * radToDeg);
        bw.Write(t.argOfPerigee * radToDeg);
        bw.Write(t.longOfAscNode * radToDeg);
        bw.Write(t.startingMeanAnom);
        bw.Write(t.startingEpoch);
        bw.Write(t.mu);
    }

    private static Timeline deserializeTimeline(BinaryReader br) {
        double semiMajor = br.ReadDouble();
        double ecc = br.ReadDouble();
        double incl = br.ReadDouble();
        double argOfPer = br.ReadDouble();
        double longOfAsc = br.ReadDouble();
        double meanAnom = br.ReadDouble();
        double startTime = br.ReadDouble();
        double mu = br.ReadDouble();

        return new Timeline(semiMajor, ecc, incl, argOfPer, longOfAsc, meanAnom, 1, startTime, mu);
    }
}
