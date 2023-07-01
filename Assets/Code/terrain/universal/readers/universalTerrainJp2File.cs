using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class universalTerrainJp2File : IUniversalTerrainFile<universalTerrainMesh> {
    public bool isForAccessCalls = false;

    /// <summary> Callback for access call generation. Geo is the point's geo, double is the height (not including radius)</summary>
    public Action<geographic, double> accessCallAction;

    private List<geographic> accessCallGeo;
    private List<double> accessCallHeight;

    public universalTerrainJp2File(string dataPath, string metadataPath, bool isForAccessCalls = false) :
        base(dataPath, metadataPath, universalTerrainFileSources.jp2) {
        this.isForAccessCalls = isForAccessCalls;
    }

    public void consumeAccessCallData() {
        if (isForAccessCalls && accessCallAction == null) throw new MissingFieldException("isForAccessCalls is true but accessCallAction is null");

        int len = accessCallGeo.Count;
        for (int i = 0; i < len; i++) accessCallAction(accessCallGeo[i], accessCallHeight[i]);

        accessCallGeo = new List<geographic>();
        accessCallHeight = new List<double>();
    }

    public override meshDistributor<universalTerrainMesh> load(geographic start, geographic end, double radius, uint res) {
        accessCallGeo = new List<geographic>();
        accessCallHeight = new List<double>();
        
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

        return load(s, e, radius, res);
    }

    private float clamp(float v, float min, float max) => Math.Max(Math.Min(v, max), min);

    public override meshDistributor<universalTerrainMesh> load(Vector2 startPercent, Vector2 endPercent, double radius, uint res) {
        startPercent.x = clamp(startPercent.x, 0, 1);
        startPercent.y = clamp(startPercent.y, 0, 1);
        endPercent.x = clamp(endPercent.x, 0, 1);
        endPercent.y = clamp(endPercent.y, 0, 1);

        accessCallGeo = new List<geographic>();
        accessCallHeight = new List<double>();

        if (res < 0 || res > metadata["res"]) throw new ArgumentException($"Invalid res (0 <= x <= {res})");
        int power = (int) Math.Pow(2, res);

        Vector2Int start = new Vector2Int((int) (startPercent.x * ncols), (int) (startPercent.y * nrows));
        Vector2Int end = new Vector2Int((int) (endPercent.x * ncols), (int) (endPercent.y * nrows));

        // ensure that (start - end) will be a multiple of 2^res
        end.x -= (end.x - start.x) % power;
        end.y -= (end.y - start.y) % power;

        start /= power;
        end /= power;

        meshDistributor<universalTerrainMesh> m = new meshDistributor<universalTerrainMesh>(
            new Vector2Int(end.x - start.x, end.y - start.y),
            Vector2Int.zero, Vector2Int.zero, true);

        int[] heights = openJpegWrapper.requestTerrain(dataPath, start * power, end * power, res, 0);

        int colLen = end.x - start.x;
        int maxHeight = (int) (nrows / power);
        for (int r = start.y; r < end.y; r++) {
            for (int c = start.x; c < end.x; c++) {
                int x = c - start.x;
                int y = r - start.y;

                geographic g = new geographic((maxHeight - r) * cellSize * power, c * cellSize * power) + llCorner;
                double height = (heights[(int) (y * colLen + x)] - 32767) / 1000.0; // +32767 bc data is offset in jp2 writer
                position p = g.toCartesian(radius + height).swapAxis() / master.scale;

                if (isForAccessCalls && height != 0) {
                    accessCallGeo.Add(g);
                    accessCallHeight.Add(height);
                }

                m.addPoint(x, y, p);
            }
        }

        Debug.Log("Loaded area of " + (end.y - start.y) * (end.x - start.x) + " pixels");

        return m;
    }
}
