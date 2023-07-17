using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class universalTerrainJp2File : IUniversalTerrainFile<universalTerrainMesh> {
    public bool isForAccessCalls = false;

    /// <summary> Callback for access call generation. Geo is the point's geo, double is the height (not including radius)</summary>
    public Action<Vector2Int, geographic, double> accessCallAction;

    private List<geographic> accessCallGeo;
    private List<double> accessCallHeight;
    private List<Vector2Int> accessCallGrid;
    private geographic generatedLL, generatedUR;
    private Vector2Int generatedStart, generatedEnd;
    private int generatedPower;
    private Func<geographic, double, position> toCart;

    public universalTerrainJp2File(string dataPath, string metadataPath, bool isForAccessCalls = false) :
        base(dataPath, metadataPath, universalTerrainFileSources.jp2) {
        this.isForAccessCalls = isForAccessCalls;

        toCart = geographic.toCartesian;
    }

    public universalTerrainJp2File(string path, bool isForAccessCalls = false) :
        base(Path.Combine(path, "data.jp2"), Path.Combine(path, "metadata.txt"), universalTerrainFileSources.jp2) {
        this.isForAccessCalls = isForAccessCalls;

        toCart = geographic.toCartesian;
    }

    // TODO currently doesnt do anything
    public void overrideToCart(Func<geographic, double, position> f) {
        toCart = f;
    }

    public void consumeAccessCallData() {
        if (!isForAccessCalls) throw new ArgumentException("This instance is not marked as isForAccessCalls");
        if (isForAccessCalls && accessCallAction == null) throw new MissingFieldException("isForAccessCalls is true but accessCallAction is null");

        int len = accessCallGeo.Count;
        for (int i = 0; i < len; i++) accessCallAction(accessCallGrid[i], accessCallGeo[i], accessCallHeight[i]);

        accessCallGeo = new List<geographic>();
        accessCallHeight = new List<double>();
        accessCallGrid = new List<Vector2Int>();
    }

    public double getHeight(geographic g) {
        double deltaY = nrows * cellSize;
        double deltaX = ncols * cellSize;

        geographic _g = g - llCorner;
        Vector2 percent = new Vector2((float) (_g.lon / deltaX), 1f - (float) (_g.lat / deltaY));
        Vector2Int point = new Vector2Int((int) (percent.x * ncols), (int) (percent.y * nrows));

        int[] heights = openJpegWrapper.requestTerrain(dataPath, point, point + Vector2Int.one, 0, 0);

        return (heights[0] - 32767.0) / 1000.0;
    }

    public override meshDistributor<universalTerrainMesh> load(geographic start, geographic end, double radius, uint res, position posOffset = default(position)) {
        accessCallGeo = new List<geographic>();
        accessCallHeight = new List<double>();
        accessCallGrid = new List<Vector2Int>();
        
        if (start.lat >= end.lat || start.lon >= end.lon) {
            throw new ArgumentException($"End geo ({end}) must be strictly greater than start geo ({start})");
        }

        double deltaY = nrows * cellSize;
        double deltaX = ncols * cellSize;

        geographic _s = start - llCorner;
        geographic _e = end - llCorner;

        Vector2 s = new Vector2((float) (_s.lon / deltaX), 1f - (float) (_e.lat / deltaY));
        Vector2 e = new Vector2((float) (_e.lon / deltaX), 1f - (float) (_s.lat / deltaY));

        s.x = clamp(s.x, 0, 1);
        s.y = clamp(s.y, 0, 1);
        e.x = clamp(e.x, 0, 1);
        e.y = clamp(e.y, 0, 1);

        return load(s, e, radius, res, posOffset);
    }

    private float clamp(float v, float min, float max) => Math.Max(Math.Min(v, max), min);

    /// <summary> Try to match res to get the desired amount of points </summary>
    public uint getBestResolution(geographic ll, geographic up, double pointThreshold) {
        geographic delta = up - ll;
        double deltaY = delta.lat / cellSize;
        double deltaX = delta.lon / cellSize;

        int cols = (int) Math.Floor(deltaX);
        int rows = (int) Math.Floor(deltaY);

        double total = cols * rows;
        if (pointThreshold > total) return 0;

        double p = Math.Log(total / pointThreshold, 2) / 2.0;
        p = Math.Round(p);
        if (p > 5) p = 5;
        if (p < 0) p = 0;

        return (uint) p;
    }

    public override meshDistributor<universalTerrainMesh> load(geographic center, double planetRadius, uint res, double offset = 0.5, position posOffset = default(position)) {
        accessCallGeo = new List<geographic>();
        accessCallHeight = new List<double>();
        accessCallGrid = new List<Vector2Int>();

        geographic o = new geographic(offset, offset);
        return load(center - o, center + o, planetRadius, res, posOffset);
    }

    public override meshDistributor<universalTerrainMesh> load(Vector2 startPercent, Vector2 endPercent, double radius, uint res, position posOffset) {
        startPercent.x = clamp(startPercent.x, 0, 1);
        startPercent.y = clamp(startPercent.y, 0, 1);
        endPercent.x = clamp(endPercent.x, 0, 1);
        endPercent.y = clamp(endPercent.y, 0, 1);

        accessCallGeo = new List<geographic>();
        accessCallHeight = new List<double>();
        accessCallGrid = new List<Vector2Int>();

        if (res < 0 || res > metadata["res"]) throw new ArgumentException($"Invalid res (0 <= x <= {res})");
        int power = (int) Math.Pow(2, res);

        Vector2Int start = new Vector2Int((int) (startPercent.x * ncols), (int) (startPercent.y * nrows));
        Vector2Int end = new Vector2Int((int) (endPercent.x * ncols), (int) (endPercent.y * nrows));

        // ensure that (start - end) will be a multiple of 2^res
        end.x -= (end.x - start.x) % power;
        end.y -= (end.y - start.y) % power;

        start /= power;
        end /= power;

        generatedStart = start;
        generatedEnd = end;
        generatedPower = power;

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        meshDistributor<universalTerrainMesh> m = new meshDistributor<universalTerrainMesh>(
            new Vector2Int(end.x - start.x, end.y - start.y),
            Vector2Int.zero, Vector2Int.zero, true, meshEdgeOffset: false);

        controller.meshTimerInit += sw.ElapsedMilliseconds;
        Debug.Log($"Time to init mesh: {sw.ElapsedMilliseconds}ms");
        sw.Restart();
        
        int[] heights = openJpegWrapper.requestTerrain(dataPath, start * power, end * power, res, 0);
        controller.meshTimerRead += sw.ElapsedMilliseconds;
        Debug.Log($"Time to read {(end.y - start.y) * (end.x - start.x)} pixels: {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        //int colLen = end.x - start.x;
        //int maxHeight = (int) (nrows / power);
        //for (int r = start.y; r < end.y; r++) {
        //    for (int c = start.x; c < end.x; c++) {
        //        int x = c - start.x;
        //        int y = r - start.y;
//
        //        geographic g = new geographic((maxHeight - r) * cellSize * power, c * cellSize * power) + llCorner;
        //        double height = (heights[(int) (y * colLen + x)] - 32767) / 1000.0; // +32767 bc data is offset in jp2 writer
        //        position p = toCart(g, radius + height);
        //        p += posOffset;
        //        p = p.swapAxis() / master.scale;
//
        //        if (isForAccessCalls && height != 0) {
        //            accessCallGeo.Add(g);
        //            accessCallHeight.Add(height);
        //            accessCallGrid.Add(new Vector2Int(x, y));
        //        }
//
        //        m.addPoint(x, y, p);
        //    }
        //}

        Vector3[] output = computeShaderPoints(heights, start, end, power);
        long s1 = sw.ElapsedMilliseconds;
        sw.Restart();

        m.forceSetAllPoints(output);
        //Debug.Log($"(cs) <color=orange>Point copying time: {sw.ElapsedMilliseconds}</color>");
        controller.meshTimerWrite += s1 + sw.ElapsedMilliseconds;
        Debug.Log($"Total processing time: {s1 + sw.ElapsedMilliseconds}");
        sw.Stop();

        //Debug.Log("Loaded area of " + (end.y - start.y) * (end.x - start.x) + " pixels");   

        return m;
    }

    public void overridePoint(meshDistributor<universalTerrainMesh> src, geographic g, position p) {
        g -= llCorner;
        double dy = g.lat / size.lat;
        double dx = g.lon / size.lon;

        int y = (int) Math.Round((1.0 - dy) * nrows / generatedPower - generatedStart.y);
        int x = (int) Math.Round(dx * ncols / generatedPower - generatedStart.x);

        src.addPoint(x, y, p.swapAxis() / master.scale);
    }

    private Vector3[] computeShaderPoints(int[] heights, Vector2Int start, Vector2Int end, int power) {
        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        //sw.Start();

        ComputeShader cs = Resources.Load<ComputeShader>("Materials/terrainWGSComputeSingle");

        Vector2Int shape = end - start;
        Vector2Int meshes = new Vector2Int(Mathf.CeilToInt((float) shape.x / 256f), Mathf.CeilToInt((float) shape.y / 256f));
        //Debug.Log($"(cs) <color=orange>Initial setup: {sw.ElapsedMilliseconds}</color>");
        //sw.Restart();

        Vector3[] vectors = new Vector3[shape.x * shape.y];
        //Debug.Log($"(cs) <color=orange>Output array allocation time: {sw.ElapsedMilliseconds}</color>");
        //sw.Restart();

        ComputeBuffer vectorBuffer = new ComputeBuffer(vectors.Length, sizeof(float) * 3);
        vectorBuffer.SetData(vectors);

        ComputeBuffer heightBuffer = new ComputeBuffer(heights.Length, sizeof(int));
        heightBuffer.SetData(heights);

        cs.SetBuffer(0, "vectors", vectorBuffer);
        cs.SetBuffer(0, "heights", heightBuffer);

        //Debug.Log($"(cs) <color=orange>Set buffers: {sw.ElapsedMilliseconds}</color>");
        //sw.Restart();

        cs.SetInts("meshCount", new int[] {meshes.x, meshes.y});
        cs.SetInts("pointCount", new int[] {shape.x, shape.y});
        cs.SetFloat("scale", (float) master.scale);
        cs.SetFloat("cellsize", (float) cellSize);
        cs.SetFloat("power", power);
        cs.SetFloat("totalNRows", (float) nrows);
        cs.SetFloats("llCorner", new float[2] {
            (float) (start.x * cellSize * power + llCorner.lon),
            (float) (start.y * cellSize * power + llCorner.lat)
        });

        //Debug.Log($"(cs) <color=orange>Set non-buffer variables: {sw.ElapsedMilliseconds}</color>");
        //sw.Restart();

        cs.Dispatch(0, 2 * meshes.x * meshes.y, 1, 1);
        //Debug.Log($"(cs) <color=orange>Compute shader running time: {sw.ElapsedMilliseconds}</color>");
        //sw.Restart();

        vectorBuffer.GetData(vectors);
        //Debug.Log($"(cs) <color=orange>GPU to CPU transfer time: {sw.ElapsedMilliseconds}</color>");
        //sw.Stop();

        vectorBuffer.Dispose();
        heightBuffer.Dispose();

        return vectors;
    }
}
