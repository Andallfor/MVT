using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

public static class terrainProcessor
{
    private static Vector2[] dir = new Vector2[8] {new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1),
                                                   new Vector2(-1, 0),                    new Vector2(1, 0),
                                                   new Vector2(-1,-1), new Vector2(0,-1), new Vector2(1,-1)};
    public static void divideAll(string folder, List<terrainResolution> resolutions, int normalStep, string normalPath)
    {
        List<string> files = Directory.GetFiles(folder).ToList();

        // create folders
        foreach (terrainResolution tr in resolutions)
        {
            Directory.CreateDirectory(tr.dest);
        }

        int ogWidth = 14400;  // lon
        int ogHeight = 10800; // lat

        double totalWidth = ogWidth * 6;
        double totalHeight = ogHeight * 4;

        BoundsInt worldBounds = new BoundsInt(new Vector3Int(-ogWidth * 3, -ogHeight * 2, 0), new Vector3Int(ogWidth * 6, ogHeight * 4, 1));

        terrainNormalMap tnp = new terrainNormalMap((int)totalWidth / normalStep, (int)totalHeight / normalStep, normalStep);

        Vector2Int geoOffset = new Vector2Int(180 * ogWidth, 90 * ogHeight);

        /* #region  load boundary holders */
        Dictionary<string, Dictionary<Vector2Int, terrainBoundaries>> boundaries = new Dictionary<string, Dictionary<Vector2Int, terrainBoundaries>>();
        foreach (terrainResolution res in resolutions)
        {
            boundaries[res.dest] = new Dictionary<Vector2Int, terrainBoundaries>();

            double stepHeight = ogHeight / Math.Sqrt(res.count);
            double stepWidth = ogWidth / Math.Sqrt(res.count);

            geographic geoIncrement = new geographic(
                180 / (totalHeight / stepHeight),
                360 / (totalWidth / stepWidth));

            for (double _x = -totalWidth / 2; _x < totalWidth / 2; _x += stepWidth)
            {
                for (double _y = -totalHeight / 2; _y < totalHeight / 2; _y += stepHeight)
                {
                    // lower left hand corner
                    Vector2Int key = new Vector2Int((int)_x, (int)_y);

                    geographic llcorner = new geographic(
                        180.0 * ((_y + totalHeight / 2.0) / totalHeight) - 90.0,
                        360.0 * ((_x + totalWidth / 2.0) / totalWidth) - 180.0);

                    boundaries[res.dest].Add(key, new terrainBoundaries((int)(stepWidth / res.step), (int)(stepHeight / res.step), res.dest, llcorner, geoIncrement));
                }
            }
        } /* #endregion */

        foreach (string file in files)
        {
            string[] data = File.ReadAllLines(file);

            /* #region  init */
            geographic startGeo = new geographic(
                double.Parse(data[3].Split(' ').Last(), System.Globalization.NumberStyles.Any),
                double.Parse(data[2].Split(' ').Last(), System.Globalization.NumberStyles.Any));

            // get x, y position of lower left
            Vector2Int initCoord = new Vector2Int((int)((startGeo.lon / 60.0) * ogWidth), (int)((startGeo.lat / 45.0) * ogHeight));

            // generate needed files dont question the dicts
            Dictionary<string, Dictionary<Vector2Int, (StreamWriter sw, StringBuilder sb)>> newFiles = new Dictionary<string, Dictionary<Vector2Int, (StreamWriter sw, StringBuilder sb)>>();
            foreach (terrainResolution res in resolutions)
            {
                newFiles[res.dest] = new Dictionary<Vector2Int, (StreamWriter sw, StringBuilder sb)>();

                double stepHeight = ogHeight / Math.Sqrt(res.count);
                double stepWidth = ogWidth / Math.Sqrt(res.count);

                geographic geoIncrement = new geographic(
                    180 / (totalHeight / stepHeight),
                    360 / (totalWidth / stepWidth));

                for (double _x = initCoord.x; _x < initCoord.x + ogWidth; _x += stepWidth)
                {
                    for (double _y = initCoord.y; _y < initCoord.y + ogHeight; _y += stepHeight)
                    {
                        // lower left hand corner
                        Vector2Int key = new Vector2Int((int)_x, (int)_y);

                        geographic llcorner = new geographic(
                            180.0 * ((_y + totalHeight / 2.0) / totalHeight) - 90.0,
                            360.0 * ((_x + totalWidth / 2.0) / totalWidth) - 180.0);

                        StreamWriter sw = File.CreateText(Path.Combine(res.dest, fileName(llcorner, geoIncrement)));
                        sw.WriteLine($"ncols        {stepWidth / res.step}");
                        sw.WriteLine($"nrows        {stepHeight / res.step}");
                        sw.WriteLine($"xllcorner    {llcorner.lon}");
                        sw.WriteLine($"yllcorner    {llcorner.lat}");
                        sw.WriteLine($"cellsize     {geoIncrement.lon / (stepWidth / res.step)}");
                        sw.WriteLine($"NODATA_value {NODATA_value}");

                        newFiles[res.dest][key] = (sw, new StringBuilder());
                    }
                }

                StreamWriter info = File.CreateText(Path.Combine(res.dest, folderInfoName));
                info.WriteLine($"ncols             {Math.Sqrt(res.count) * (stepWidth / res.step) * 6.0}");
                info.WriteLine($"nrows             {Math.Sqrt(res.count) * (stepHeight / res.step) * 4.0}");
                info.WriteLine($"cellsize          {geoIncrement.lon / (stepWidth / res.step)}");
                info.WriteLine($"pointsPerCoord    {(Math.Sqrt(res.count) * (stepWidth / res.step) * 6.0) / 360.0}");
                info.WriteLine($"filePer45_60      {res.count}");
                info.WriteLine($"generationStep    {res.step}");
                info.WriteLine($"name              {res.dest.Split('/').Last()}");
                info.WriteLine($"increment         (lat={geoIncrement.lat}_lon={geoIncrement.lon})");
                info.Close();
            }
            /* #endregion */

            /* #region  create files */
            // generate data to give to files
            string[] heights = data.Skip(6).ToArray();
            double y = ogHeight - 1; // since we start in the upper left, we need to decrease the index as we go downwards
            int heightsLength = heights.Length;
            for (int i = 0; i < heightsLength; i++)
            {
                string r = heights[i];
                // since we expect all new files to be fully contained within one src file, we can just add a new line to them
                // whenever we reach the end of a single loop instead of dynamically determining when to do so.
                Dictionary<int, StringBuilder> modified = new Dictionary<int, StringBuilder>();

                string[] row = r.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

                int hLength = row.Length;
                for (double x = 0; x < hLength; x++)
                {
                    string h = row[(int)x];
                    // add point to file(s)
                    foreach (terrainResolution res in resolutions)
                    {
                        if (x % res.step == 0 && y % res.step == 0)
                        {
                            double stepHeight = ogHeight / Math.Sqrt(res.count);
                            double stepWidth = ogWidth / Math.Sqrt(res.count);

                            Vector2Int k = getKey(x, y, stepWidth, stepHeight);
                            Vector2Int key = initCoord + k;

                            newFiles[res.dest][key].sb.Append(' ' + h);
                            // sometimes the same key pops up in different resolutions, so hash it against the step size to ensure no conflicts
                            int a = key.GetHashCode();
                            int b = res.step;
                            int hash = a >= b ? a * a + a + b : a + b * b;
                            if (!modified.ContainsKey(hash)) modified[hash] = (newFiles[res.dest][key].sb);

                            // log boundaries -> if one more iteration would be the actual boundary, then add to the boundary list
                            for (int j = 0; j < 8; j++)
                            {
                                Vector2Int dp = new Vector2Int((int)(x + dir[j].x * res.step), (int)(y + dir[j].y * res.step));
                                Vector2Int k2 = initCoord + getKey(dp.x, dp.y, stepWidth, stepHeight);
                                Vector2Int rk = wrap(k2, worldBounds);
                                if (k2 != key) // current point is k2's boundary
                                {
                                    // get the position dir is pointing towards
                                    Vector2Int np = initCoord + new Vector2Int((int) x, (int) y);

                                    // determine the new points relationship with k2's boundary
                                    if (np.x >= k2.x + stepWidth) boundaries[res.dest][rk].addPoint(cardinalDirections.east, Math.Abs((k2.y - res.step) - np.y) / res.step, h);
                                    if (np.x < k2.x) boundaries[res.dest][rk].addPoint(cardinalDirections.west, Math.Abs(np.y - (k2.y - res.step)) / res.step, h);
                                    if (np.y >= k2.y + stepHeight) boundaries[res.dest][rk].addPoint(cardinalDirections.north, Math.Abs((k2.x - res.step) - np.x) / res.step, h);
                                    if (np.y < k2.y) boundaries[res.dest][rk].addPoint(cardinalDirections.south, Math.Abs(np.x - (k2.x - res.step)) / res.step, h);
                                }
                            }
                        }
                    }

                    // add point to normal map
                    if (x % normalStep == 0 && y % normalStep == 0) tnp.addPoint((int)x + initCoord.x, (int)y + initCoord.y, int.Parse(h, System.Globalization.NumberStyles.Any)); // dont allow negatives
                    // check texture2d to make sure were not overloading it with info (akin to 16 v 32 indicies for meshes)
                }

                foreach (StringBuilder sb in modified.Values) sb.Append('\n');
                y--;
            }

            // write then close all files
            foreach (Dictionary<Vector2Int, (StreamWriter sw, StringBuilder sb)> d in newFiles.Values)
            {
                foreach ((StreamWriter sw, StringBuilder sb) v in d.Values)
                {
                    v.sw.Write(v.sb);
                    v.sw.Close();
                    v.sb.Clear();
                }
            }

            // write boundary files

            newFiles.Clear();
            /* #endregion */
        }

        foreach (KeyValuePair<string, Dictionary<Vector2Int, terrainBoundaries>> d in boundaries)
        {
            foreach (KeyValuePair<Vector2Int, terrainBoundaries> dd in d.Value)
            {
                StreamWriter sw = File.CreateText(Path.Combine(dd.Value.path, fileBoundaryName(dd.Value.p, dd.Value.inc)));
                sw.WriteLine("n ((0,1)-(1,1)), e ((1,0)-(1,1)), s ((0,0)-(0,1)), w ((0,0)-(0,1))");
                sw.WriteLine(' ' + string.Join(" ", dd.Value.n));
                sw.WriteLine(' ' + string.Join(" ", dd.Value.e));
                sw.WriteLine(' ' + string.Join(" ", dd.Value.s));
                sw.WriteLine(' ' + string.Join(" ", dd.Value.w));

                sw.Close();
            }
        }

        tnp.saveTexture(normalPath);
    }

    private static Vector2Int wrap(Vector2Int v, BoundsInt b)
    {
        if (v.x < b.xMin) v.x += b.xMax - b.xMin;
        else if (v.x >= b.xMax) v.x -= b.xMax - b.xMin;
        if (v.y < b.yMin) v.y += b.yMax - b.yMin;
        else if (v.y >= b.yMax) v.y -= b.yMax - b.yMin;

        return v;
    }

    public static string fileName(geographic pos, geographic inc) => $"lat={Math.Round(pos.lat, 2)}_lon={Math.Round(pos.lon, 2)}_+({inc.lat}_{inc.lon}).txt";
    public static string fileBoundaryName(geographic pos, geographic inc) => fileName(pos, inc).Replace(".txt", "_boundary.txt");
    public const string folderInfoName = "resInfo.txt";
    public const int NODATA_value = -32767;

    private static Vector2Int getKey(double x, double y, double stepWidth, double stepHeight) => new Vector2Int(
       (int)(stepWidth * Math.Floor(x / stepWidth)),
       (int)(stepHeight * Math.Floor(y / stepHeight)));
}

public readonly struct terrainResolution
{
    public readonly string dest;
    public readonly int count, step;

    public terrainResolution(string dest, int count, int step)
    {
        this.dest = dest;
        this.count = count;
        this.step = step;
    }
}

public class terrainBoundaries
{
    public string[] n, e, s, w;
    public int rcol, rrow;
    public string path;
    public geographic p, inc;

    public terrainBoundaries(int ncols, int nrows, string path, geographic p, geographic inc)
    {
        this.rcol = ncols + 2;
        this.rrow = nrows + 2;

        this.path = path;
        this.p = p;
        this.inc = inc;

        n = new string[rcol];
        s = new string[rcol];
        e = new string[rrow];
        w = new string[rrow];
    }

    public void addPoint(cardinalDirections cd, int location, string h)
    {
        switch (cd)
        {
            case cardinalDirections.north:
                n[location] = h;
                break;
            case cardinalDirections.south:
                s[location] = h;
                break;
            case cardinalDirections.east:
                e[location] = h;
                break;
            case cardinalDirections.west:
                w[location] = h;
                break;
        }
    }
}

public enum cardinalDirections
{
    north, east, south, west
}