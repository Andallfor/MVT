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
        // num satellites (byte)
        // num facilities (byte)
        // num antennas (byte)

        // planet format
        // full Length (short)
        // name length (short) | name (chars) | radius (double) | rot type (byte) |
        // semimajor (double) | ecc (double) | incl (double) | argOfPer (double) | longOfAsc (double) | meanAnom (double) | startTime (double) | mu (double)
        // note that we dont send mass, not needed
        // planetType (byte) | texture name length (short) | texture
        // parent name length (short) | parent name
        byte[] planets = serializePlanets();

        return planets;
    }

    private static byte[] serializePlanets() {
        Debug.Log("received request");
        //List<byte> data = new List<byte>();
        byte numPlanets = 0;

        double radToDeg = 180.0 / Math.PI;

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
                foreach (planet p in planets) {
                    if (p.positions.selection == TimelineSelection.positions) continue;
                    if (p.GetHashCode() == master.sun.GetHashCode()) continue; // sun is auto created no matter the scenario, dont bother sending it    

                    bw.Write(p.name);
                    bw.Write(p.radius);
                    bw.Write((byte) p.getRotationType());
                    
                    TimelineKepler t = (TimelineKepler) (p.positions.getSource());
                    bw.Write(t.semiMajorAxis);
                    bw.Write(t.eccentricity);
                    bw.Write(t.inclination * radToDeg);
                    bw.Write(t.argOfPerigee * radToDeg);
                    bw.Write(t.longOfAscNode * radToDeg);
                    bw.Write(t.startingMeanAnom);
                    bw.Write(t.startingEpoch);
                    bw.Write(t.mu);

                    bw.Write((byte) p.pType);
                    bw.Write(p.representation.data.materialPath);
                    bw.Write(p.parent == null ? master.sun.name : p.parent.name);

                    numPlanets++;
                }
            }

            // shush
            List<byte> bytes = ms.ToArray().ToList();
            bytes.Insert(0, numPlanets);
            data = bytes.ToArray();
        }

        return data;

        /*
        // TODO: redo with binary writer
        foreach (planet p in master.allPlanets) {
            if (p.positions.selection == TimelineSelection.positions) continue;
            if (p.GetHashCode() == master.sun.GetHashCode()) continue; // sun is auto created no matter the scenario, dont bother sending it

            byte[] name = serializeString(p.name);
            byte[] texture = serializeString(p.representation.data.materialPath);
            byte[] parent = serializeString(p.parent == null ? master.sun.name : p.parent.name);
            byte[] keplerData = new byte[8 * sizeof(double)];

            TimelineKepler t = (TimelineKepler) (p.positions.getSource());
            double[] k = new double[8] {
                t.semiMajorAxis,
                t.eccentricity,
                t.inclination * radToDeg,
                t.argOfPerigee * radToDeg,
                t.longOfAscNode * radToDeg,
                t.startingMeanAnom,
                t.startingEpoch,
                t.mu};

            Buffer.BlockCopy(k, 0, keplerData, 0, keplerData.Length);

            byte[] pd = new byte[name.Length + texture.Length + keplerData.Length + parent.Length + sizeof(double) + 2 + sizeof(short)]; // + radius + rotType + planetType + totalLength

            int index = 0;
            Array.Copy(BitConverter.GetBytes((short) (pd.Length - sizeof(short))), 0, pd, index, sizeof(short)); index += sizeof(short);
            Array.Copy(name, 0, pd, index, name.Length); index += name.Length;
            Array.Copy(BitConverter.GetBytes(p.radius), 0, pd, index, sizeof(double)); index += sizeof(double);
            pd[index] = (byte) p.getRotationType(); index++;
            Array.Copy(keplerData, 0, pd, index, keplerData.Length); index += keplerData.Length;
            pd[index] = (byte) p.pType; index++;
            Array.Copy(texture, 0, pd, index, texture.Length);

            // yeah we save as array then convert to list then convert to array (to send)
            // the idea is that by initially doing array we can enforce that all the byte size calcs are correct, so its easier to debug
            data.AddRange(pd.ToList());
            numPlanets++;
        }

        data.Insert(0, numPlanets);
        */

        //return data.ToArray();
    }

    private static byte[] serializeString(string s) {
        // length | content
        byte[] arr = new byte[sizeof(short) + s.Length];

        if (s.Length > byte.MaxValue) throw new ArgumentException("Unable to serialize string greater than 255 chars long");
        Array.Copy(BitConverter.GetBytes((short) s.Length), arr, sizeof(short));

        byte[] str = Encoding.ASCII.GetBytes(s); // encoded in ascii, is one byte per char
        Buffer.BlockCopy(str, 0, arr, sizeof(short), str.Length);

        return arr;
    }

    public static void deserializeScenario(byte[] data) {
        using (BinaryReader br = new BinaryReader(new MemoryStream(data))) {
            deserializePlanets(br);
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
            double semiMajor = br.ReadDouble();
            double ecc = br.ReadDouble();
            double incl = br.ReadDouble();
            double argOfPer = br.ReadDouble();
            double longOfAsc = br.ReadDouble();
            double meanAnom = br.ReadDouble();
            double startTime = br.ReadDouble();
            double mu = br.ReadDouble();
            planetType pType = (planetType) br.ReadByte();
            string texture = br.ReadString();
            string parent = br.ReadString();

            new planet(name, new  planetData(radius, rotType, new Timeline(semiMajor, ecc, incl, argOfPer, longOfAsc, meanAnom, 1, startTime, mu), pType), new representationData("planet", texture), master.allPlanets.Find(x => x.name == parent));
            /*
            short pDataLength = br.ReadInt16();

            short nameLength = br.ReadInt16();
            string name = general.parseByteArray(br.ReadBytes(nameLength));
            Debug.Log(name);

            double radius = br.ReadDouble();
            rotationType rotType = (rotationType) br.ReadByte();

            double semiMajor = br.ReadDouble();
            double ecc = br.ReadDouble();
            double incl = br.ReadDouble();
            double argOfPer = br.ReadDouble();
            double longOfAsc = br.ReadDouble();
            double meanAnom = br.ReadDouble();
            double startTime = br.ReadDouble();
            double mu = br.ReadDouble();

            planetType pType = (planetType) br.ReadByte();

            short textureLength = br.ReadInt16();
            string textureName = general.parseByteArray(br.ReadBytes(textureLength));

            short parentLength = br.ReadInt16();
            string parentName = general.parseByteArray(br.ReadBytes(parentLength));

            planet parent = master.allPlanets.First(x => x.name == parentName);

            Timeline tl = new Timeline(semiMajor, ecc, incl, argOfPer, longOfAsc, meanAnom, 1, startTime, mu);
            Debug.Log("created timeline");
            representationData rd = new representationData("planet", textureName);
            Debug.Log("created representation data");
            planetData pd = new planetData(radius, rotType, tl, pType);
            Debug.Log("created planet data");
            new planet(name, pd, rd, parent);
            Debug.Log("finished creating planet");
            */
        }

        Debug.Log("end");
    }
}
