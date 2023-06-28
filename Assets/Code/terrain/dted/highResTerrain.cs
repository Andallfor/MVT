using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;

public static class highResTerrain {
    public static List<nearbyFacilities> neededAreas() {
        List<nearbyFacilities> nfs = new List<nearbyFacilities>();
        foreach (facilityData fd in csvParser.loadFacilities("CSVS/FACILITIES/stationList")) {
            bool foundValidParent = false;
            foreach (nearbyFacilities nf in nfs) {
                if (nf.tryJoin(fd)) {
                    foundValidParent = true;
                    break;
                }
            }

            if (!foundValidParent) nfs.Add(new nearbyFacilities(fd));
        }

        return nfs;
    }

    public static meshDistributor<dtedBasedMesh> readHRT(string path) {
        string[] data = File.ReadAllLines(path);

        double interval = double.Parse(data[0].Split(':')[1]);
        geographic min = new geographic( // this is fine i promise
            double.Parse(data[1].Split(':')[1].Split(',')[0]),
            double.Parse(data[1].Split(':')[1].Split(',')[1]));
        geographic max = new geographic(
            double.Parse(data[2].Split(':')[1].Split(',')[0]),
            double.Parse(data[2].Split(':')[1].Split(',')[1]));
        
        double resX = double.Parse(data[3].Split(':')[1].Split(',')[0]);
        double resY = double.Parse(data[3].Split(':')[1].Split(',')[1]);

        char[] splitter = new char[1] {' '};

        meshDistributor<dtedBasedMesh> distributor = new meshDistributor<dtedBasedMesh>(
            new Vector2Int((int) resX, (int) resY),
            Vector2Int.zero, Vector2Int.zero,
            true,
            customUV: (Vector2Int v) => new Vector2((float) v.x / (float) resX, (float) (resY - v.y) / (float) resY)
        );

        geographic nw = new geographic(max.lat, min.lon);
        position offset = dtedReader.centerDtedPoint(min - new geographic(0.5, 0.5), min, new position(0, 0, 0), 0).swapAxis();

        for (int y = 0; y < resY; y++) {
            string[] line = data[y + 4].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x < resX; x++) {
                double h = double.Parse(line[x]);

                geographic g = nw + new geographic(-(y / resY) * (max.lat - min.lat), (x / resX) * (max.lon - min.lon));

                distributor.addPoint(x, y, dtedReader.centerDtedPoint(min, g, offset, h));
            }
        }

        distributor.baseType.sw = min;
        distributor.baseType.offset = offset;

        return distributor;
    }
}

public class nearbyFacilities {
    private const double threshold = 0.2, radius = 0.5;
    public List<facilityData> facilities = new List<facilityData>();
    /// <summary> NE, SE, SW, NW </summary>
    public List<geographic> corners = new List<geographic>();

    public nearbyFacilities(facilityData f) {
        this.facilities = new List<facilityData>() {f};
        generateBounds();
    }

    public bool tryJoin(facilityData f) {
        foreach (facilityData fac in facilities) {
            if (fac.geo.distAs2DVector(f.geo) < threshold) {
                facilities.Add(f);
                generateBounds();
                return true;
            }
        }
        return false;
    }

    public void generateBounds() {
        geographic min = new geographic(
            facilities.Min(x => x.geo.lat) - radius,
            facilities.Min(x => x.geo.lon) - radius);
        
        geographic max = new geographic(
            facilities.Max(x => x.geo.lat) + radius,
            facilities.Max(x => x.geo.lon) + radius);
        
        this.corners = new List<geographic>() {
            new geographic(max.lat, max.lon), // NE
            new geographic(min.lat, max.lon), // SE
            new geographic(min.lat, min.lon), // SW
            new geographic(max.lat, min.lon)};// NW
    }

    public override string ToString() {
        string s = "";
        foreach (facilityData fd in facilities) s += $"{fd.name} ";
        foreach (geographic g in corners) s += $"\n{g.lat}, {g.lon}";

        return s;
    }
}