using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class highResTerrain {
    public static List<nearbyFacilites> neededAreas() {
        List<nearbyFacilites> nfs = new List<nearbyFacilites>();
        foreach (facilityData fd in csvParser.loadFacilites("CSVS/FACILITIES/stationList")) {
            bool foundValidParent = false;
            foreach (nearbyFacilites nf in nfs) {
                if (nf.tryJoin(fd)) {
                    foundValidParent = true;
                    break;
                }
            }

            if (!foundValidParent) nfs.Add(new nearbyFacilites(fd));
        }

        return nfs;
    }
}

public class nearbyFacilites {
    private const double threshold = 0.2, radius = 0.5;
    public List<facilityData> facilities = new List<facilityData>();
    /// <summary> NE, SE, SW, NW </summary>
    public List<geographic> corners = new List<geographic>();

    public nearbyFacilites(facilityData f) {
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