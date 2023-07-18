using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System.Linq;
using System;
using System.IO;

public class planetTerrainMesh : IMesh
{
    public planetTerrainFile ptf;
    public planetTerrainFolderInfo ptfi;
    public planetTerrain pt;

    public planetTerrainMesh(planetTerrainFile ptf, planetTerrainFolderInfo ptfi, planetTerrain pt, bool reverse)
    {
        this.ptf = ptf;
        this.ptfi = ptfi;
        this.pt = pt;

        base.init((int) ptf.ncols + 2, (int) ptf.nrows + 2, ptf.cartPosition, new position(ptfi.ncols, ptfi.nrows, 0), reverse: reverse);
    }

    public GameObject drawMesh(string materialPath) => drawMesh(Resources.Load(materialPath) as Material);

    public GameObject drawMesh(Material mat)
    {
        return base.drawMesh(mat,
                             Resources.Load("Prefabs/PlanetMesh") as GameObject,
                             ptf.name, pt.parent.representation.gameObject.transform);
    }

    // TODO: add npy support
    public void drawBoundaries(string path) {
        if (ptf.fileType == terrainFileType.txt) drawBoundariesTxt(path);
        else drawBoundariesNpy();
    }

    private void drawBoundariesTxt(string path) {
        string[] fileData = File.ReadAllLines(path);
        string[] n = fileData[1].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();
        string[] e = fileData[2].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();
        string[] s = fileData[3].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();
        string[] w = fileData[4].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();

        for (int x = 0; x < (int) ptf.ncols + 2; x++)
        {
            geographic np = ptf.cartToGeo(x, (int) ptf.nrows + 1);
            addPoint(x, (int) ptf.nrows + 1, np, double.Parse(n[x], System.Globalization.NumberStyles.Any));

            geographic sp = ptf.cartToGeo(x, 0);
            addPoint(x, 0, sp, double.Parse(s[x], System.Globalization.NumberStyles.Any));
        }

        for (int y = 0; y < (int) ptf.nrows + 2; y++)
        {
            geographic ep = ptf.cartToGeo((int) ptf.ncols + 1, y);
            addPoint((int) ptf.ncols + 1, y, ep, double.Parse(e[y], System.Globalization.NumberStyles.Any));

            geographic wp = ptf.cartToGeo(0, y);
            addPoint(0, y, wp, double.Parse(w[y], System.Globalization.NumberStyles.Any));
        }
    }

    private void drawBoundariesNpy() {
#if UNITY_WEBGL
        throw new NotImplementedException("Numpy is not accessible on webgl");
#else
        for (int x = 0; x < (int) ptf.ncols + 2; x++) {
            geographic north = ptf.cartToGeo(x - 1, (int) ptf.nrows);
            addPoint(x, (int) ptf.nrows + 1, north, getHeight(this.verts[toIndex(x, (int) ptf.nrows)]));

            geographic south = ptf.cartToGeo(x, -1);
            addPoint(x, 0, south, getHeight(this.verts[toIndex(x, 1)]));
        }

        for (int y = 0; y < (int) ptf.nrows + 2; y++) {
            geographic east = ptf.cartToGeo((int) ptf.ncols, y - 1);
            addPoint((int) ptf.ncols + 1, y, east, getHeight(this.verts[toIndex((int) ptf.ncols, y)]));

            geographic west = ptf.cartToGeo(-1, y);
            addPoint(0, y, west, getHeight(this.verts[toIndex(1, y)]));
        }
#endif
    }

    private double getHeight(Vector3 v) => Math.Round(((position.distance(((position) v * master.scale).swapAxis(), new position(0, 0, 0)) - pt.radius) * 1000.0) / pt.heightMulti);

    public override Vector3 addPoint(int x, int y, geographic g, double h)
    {
        Vector3 v = (Vector3) (g.toCartesian(pt.radius + (h * pt.heightMulti) / 1000.0).swapAxis() / master.scale);
        this.verts[toIndex(x, y)] = v;
        return v;
    }

    public override int GetHashCode() => ptf.GetHashCode();
    public static readonly Vector3 NODATA_vector = new Vector3(
        terrainProcessor.NODATA_value, terrainProcessor.NODATA_value, terrainProcessor.NODATA_value);
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
