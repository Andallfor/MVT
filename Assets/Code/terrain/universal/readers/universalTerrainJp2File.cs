using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class universalTerrainJp2File : IUniversalTerrainFile<universalTerrainMesh> {
    public universalTerrainJp2File(string dataPath, string metadataPath) :
        base(dataPath, metadataPath, universalTerrainFileSources.jp2) {
        
    }

    public override meshDistributor<universalTerrainMesh> loadFull(double radius) {
        int i_ncols = (int) ncols;
        int i_nrows = (int) nrows;

        meshDistributor<universalTerrainMesh> m = new meshDistributor<universalTerrainMesh>(
            new Vector2Int(i_nrows, i_ncols), Vector2Int.zero, Vector2Int.zero, true);

        int[] heights = openJpegWrapper.requestTerrain(dataPath, Vector2Int.zero, new Vector2Int((int) ncols, (int) nrows), 0, 0);

        for (double r = 0; r < nrows; r++) {
            for (double c = 0; c < ncols; c++) {
                geographic g = new geographic(cellSize * (nrows - r), cellSize * c) + llCorner;
                position p = g.toCartesian(r + (double) heights[(int) (r * ncols + c)] / 1000.0).swapAxis() / master.scale;
                m.addPoint((int) c, (int) r, p);
            }
        }

        return m;
    }

    public override meshDistributor<universalTerrainMesh> load(geographic start, geographic end, double radius) {
        if (start.lat >= end.lat || start.lon >= end.lon) {
            throw new ArgumentException($"End geo ({end}) must be strictly greater than start geo ({start})");
        }

        // transfer into positions to prevent auto conversion at poles (-180 -> 180, -90 -> 90)
        position _s = new position(start.lon, start.lat, 0);
        position _e = new position(end.lon, end.lat, 0);
        position _l = new position(llCorner.lon, llCorner.lat, 0);
        position delta = _e - _s;
        int deltaGridX = (int) (delta.x / cellSize);
        int deltaGridY = (int) (delta.y / cellSize);

        int startGridX = (int) ((_s.x - _l.x) / cellSize);
        int startGridY = (int) ((_s.y - (_l.y + size.lat)) / cellSize);

        meshDistributor<universalTerrainMesh> m = new meshDistributor<universalTerrainMesh>(
            new Vector2Int(deltaGridX, deltaGridY), Vector2Int.zero, Vector2Int.zero, true);

        int[] heights = openJpegWrapper.requestTerrain(dataPath, new Vector2Int(startGridX, startGridY), new Vector2Int(startGridX + deltaGridX, startGridY + deltaGridY), 0, 0);

        for (double r = startGridY; r < startGridY + deltaGridY; r++) {
            for (double c = startGridX; c < startGridX + deltaGridX; c++) {
                int x = (int) c - startGridX;
                int y = (int) r - startGridY;

                geographic g = new geographic(cellSize * (nrows - r), cellSize * c) + llCorner;
                position p = g.toCartesian(radius + (double) heights[(int) (y * deltaGridX + x)] / 1000.0).swapAxis() / master.scale;

                m.addPoint(x, y, p);
            }
        }

        return m;
    }

    public override meshDistributor<poleTerrainMesh> load(Vector2Int start, Vector2Int end, double radius) {
        meshDistributor<poleTerrainMesh> m = new meshDistributor<poleTerrainMesh>(
            new Vector2Int(end.x - start.x, end.y - start.y), Vector2Int.zero, Vector2Int.zero, false);
        
        int[] heights = openJpegWrapper.requestTerrain(dataPath, start * 32, end * 32, 5, 0);

        Debug.Log(heights.Length);
        Debug.Log("asjkdlf");

        int colLen = end.x - start.x;
        for (int r = start.y; r < end.y; r++) {
            for (int c = start.x; c < end.x; c++) {
                int x = (int) c - start.x;
                int y = (int) r - start.y;

                //geographic g = new geographic(cellSize * (nrows - (double) r), cellSize * (double) c) + llCorner;
                geographic g = new geographic((double) r * (cellSize) * 32.0, 32.0 * (double) c * cellSize) + llCorner;
                //g = new geographic(45, 45);
                //g = new geographic(10.0 + 150 * ((double) x / (double) (end.y - start.y)))
                //Debug.Log((int) (y * colLen + x));
                position p = g.toCartesian(radius + (double) heights[(int) (y * colLen + x)] / 1000.0).swapAxis() / master.scale;
                //position p = g.toCartesian(radius).swapAxis() / master.scale;

                //p = p.normalize();
                //if (x == 0) Debug.Log(p);

                m.addPoint(x, y, p);
            }
        }

        return m;
    }
}
