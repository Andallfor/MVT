using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using NumSharp;
using Newtonsoft.Json;

public static class terrainProcessor
{
    private static Vector2[] dir = new Vector2[8] {new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1),
                                                   new Vector2(-1, 0),                    new Vector2(1, 0),
                                                   new Vector2(-1,-1), new Vector2(0,-1), new Vector2(1,-1)};
    public static void divideGebco(string folder, List<terrainResolution> resolutions)
    {
        List<string> files = Directory.GetFiles(folder).ToList();

        // create folders
        foreach (terrainResolution tr in resolutions) Directory.CreateDirectory(tr.dest);

        int ogWidth = 14400;  // lon
        int ogHeight = 10800; // lat

        double totalWidth = ogWidth * 6;
        double totalHeight = ogHeight * 4;

        BoundsInt worldBounds = new BoundsInt(new Vector3Int(-ogWidth * 3, -ogHeight * 2, 0), new Vector3Int(ogWidth * 6, ogHeight * 4, 1));

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
                info.WriteLine($"filesPerTile      {res.count}");
                info.WriteLine($"generationStep    {res.step}");
                info.WriteLine($"name              {res.dest.Split('/').Last()}");
                info.WriteLine($"increment         (lat={geoIncrement.lat}_lon={geoIncrement.lon})");
                info.WriteLine($"type              txt");
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
    }

    public static void divideJpeg2000(string folderPath, string outputPath, List<terrainResolution> resolutions, bool copyMax = false) {
        Dictionary<string, int> endingKey = new Dictionary<string, int>() {
            {"json", 0},
            {"npy", 1}};

        // sort files (each area has 3 corresponding files)
        // .json has the metadata info
        // .npy contains the body
        List<string> allFiles = Directory.GetFiles(folderPath).ToList();
        Dictionary<string, string[]> sortedFiles = new Dictionary<string, string[]>();
        foreach (string file in allFiles) {
            string name = file.Split('.')[0];
            string end = file.Split('.')[1];
            if (!sortedFiles.ContainsKey(name)) sortedFiles[name] = new string[2];
            sortedFiles[name][endingKey[end]] = file;
        }

        string headerFolder = Path.Combine(outputPath, "headers");
        string maxFolder = Path.Combine(outputPath, "max");
        Directory.CreateDirectory(headerFolder);
        Directory.CreateDirectory(maxFolder);
        bool createdResInfo = false;

        // n, e, s, w
        Dictionary<string, Dictionary<string, NDArray>> boundaries = new Dictionary<string, Dictionary<string, NDArray>>();
        foreach (terrainResolution res in resolutions) boundaries[res.dest] = new Dictionary<string, NDArray>();

        // metadata is sorted seperately
        // combine header and body into one file
        foreach (string[] files in sortedFiles.Values) {
            Debug.Log($"Reading {files[1]}");

            NDArray data = np.load(files[1]);
            jp2Metadata metadata = JsonConvert.DeserializeObject<jp2Metadata>(File.ReadAllText(files[0]));

            foreach (terrainResolution res in resolutions) {
                NDArray downsizedData = data[$"::{res.step}", $"::{res.step}"];
                int rootNumFiles = (int) Math.Sqrt(res.count);
                int lengthPerFileY = (int) ((metadata.height / res.step) / rootNumFiles);
                int lengthPerFileX = (int) ((metadata.width / res.step) / rootNumFiles);

                geographic increase = new geographic(
                    (metadata.ModelPixelScale[0] * metadata.height) / rootNumFiles,
                    (metadata.ModelPixelScale[0] * metadata.width) / rootNumFiles);

                for (int fx = 0; fx < rootNumFiles; fx++) {
                    for (int fy = 0; fy < rootNumFiles; fy++) {
                        geographic ll = new geographic(
                            metadata.yll + fy * increase.lat,
                            metadata.xll - 180.0 + fx * increase.lon); // range given is 0 - 360 but we need it as -180 - 180

                        string fileName = terrainProcessor.fileName(ll, increase, "npy");
                        NDArray arrayData = downsizedData[
                            $"{fy * lengthPerFileY}:{(fy + 1) * lengthPerFileY}",
                            $"{fx * lengthPerFileX}:{(fx + 1) * lengthPerFileX}"];

                        // create bounds array if needed
                        string boundName = Path.Combine(res.dest, terrainProcessor.fileBoundaryName(ll, increase, "npy"));
                        if (!boundaries.ContainsKey(boundName)) {
                            boundaries[res.dest][boundName] = np.full(
                                terrainProcessor.NODATA_value,
                                (4, 2 + (int) Math.Max(lengthPerFileX, lengthPerFileY)),
                                Type.GetType("double"));
                        }
                        
                        // add bounds to our own file
                        int dn = (fy + 1) * lengthPerFileY + 1;
                        int ds = fy * lengthPerFileY - 1;
                        int de = (fx + 1) * lengthPerFileX + 1;
                        int dw = fx * lengthPerFileX - 1;
                        if (dn < downsizedData.shape[0] - 1) boundaries[res.dest][boundName]["0", $"1:{1 + lengthPerFileX}"] = downsizedData[$"{dn}", $"{dw + 1}:{de - 1}"];
                        if (ds > 0) boundaries[res.dest][boundName]["2", $"1:{1 + lengthPerFileX}"] = downsizedData[$"{ds}", $"{dw + 1}:{de - 1}"];
                        if (de < downsizedData.shape[1] - 1) boundaries[res.dest][boundName]["1", $"1:{1 + lengthPerFileY}"] = downsizedData[$"{ds + 1}:{dn - 1}", $"{de}"];
                        if (dw > 0) boundaries[res.dest][boundName]["3", $"1:{1 + lengthPerFileY}"] = downsizedData[$"{ds + 1}:{dn - 1}", $"{dw}"];

                        // add corners of bounds
                        if (fx != 0 && fy != 0) { // sw
                            boundaries[res.dest][boundName]["2", "0"] = downsizedData[$"{ds}", $"{dw}"];
                            boundaries[res.dest][boundName]["3", "0"] = downsizedData[$"{ds}", $"{dw}"];
                        }
                        if (fx != rootNumFiles - 1 && fy != 0) { // se
                            boundaries[res.dest][boundName]["2", $"{lengthPerFileX}"] = downsizedData[$"{ds}", $"{de}"];
                            boundaries[res.dest][boundName]["1", "0"] = downsizedData[$"{ds}", $"{de}"];
                        }
                        if (fx != rootNumFiles - 1 && fy != rootNumFiles - 1) { // ne
                            boundaries[res.dest][boundName]["0", $"{lengthPerFileX}"] = downsizedData[$"{dn}", $"{de}"];
                            boundaries[res.dest][boundName]["1", $"{lengthPerFileY}"] = downsizedData[$"{dn}", $"{de}"];
                        }
                        if (fx != 0 && fy != rootNumFiles - 1) { // nw
                            boundaries[res.dest][boundName]["0", "0"] = downsizedData[$"{dn}", $"{dw}"];
                            boundaries[res.dest][boundName]["3", $"{lengthPerFileY}"] = downsizedData[$"{dn}", $"{dw}"];
                        }

                        // TODO: add bounds to other files
                        if (fx == 0) {
                            
                        }

                        // save data
                        np.save(Path.Combine(res.dest, fileName), arrayData);
                    }
                }

                if (!createdResInfo) {
                    Directory.CreateDirectory(res.dest);
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"ncols             {(15360.0 / Math.Sqrt(res.count) / res.step) * rootNumFiles * 8.0}");
                    sb.AppendLine($"nrows             {(23040.0 / Math.Sqrt(res.count) / res.step) * rootNumFiles * 6.0}"); // tech should be 4 but 6 to allow uvs to gen properly
                    sb.AppendLine($"cellsize          {metadata.ModelPixelScale[0] * res.step}");
                    sb.AppendLine($"pointsPerCoord    {(1.0 / metadata.ModelPixelScale[0]) / res.step}");
                    sb.AppendLine($"filesPerTile      {res.count}");
                    sb.AppendLine($"generationStep    {res.step}");
                    sb.AppendLine($"name              {Path.GetDirectoryName(res.dest)}");
                    sb.AppendLine($"increment         (lat={increase.lat}_lon={increase.lon})");
                    sb.AppendLine($"type              npy");

                    File.WriteAllText(Path.Combine(res.dest, terrainProcessor.folderInfoName), sb.ToString());
                }
            }
            createdResInfo = true;

            foreach (Dictionary<string, NDArray> d in boundaries.Values) {
                foreach (KeyValuePair<string, NDArray> kvp in d) {
                    np.save(kvp.Key, kvp.Value);
                }
            }

            // copy json (metadata) file into output folder
            File.Copy(files[0], Path.Combine(headerFolder, Path.GetFileName(files[0])), true);
            // copy max resolution into folder
            if (copyMax) File.Copy(files[1], Path.Combine(maxFolder, Path.GetFileName(files[1])), true);
        }

        Debug.Log("finished");
    }

    private static bool onCorner(int x, int y, int mx, int my) => 
        (x == 0 && y == 0) || 
        (x == mx && y == my) ||
        (x == 0 && y == my) ||
        (x == mx && y == 0) ? true : false;

    private static Vector2Int wrap(Vector2Int v, BoundsInt b)
    {
        if (v.x < b.xMin) v.x += b.xMax - b.xMin;
        else if (v.x >= b.xMax) v.x -= b.xMax - b.xMin;
        if (v.y < b.yMin) v.y += b.yMax - b.yMin;
        else if (v.y >= b.yMax) v.y -= b.yMax - b.yMin;

        return v;
    }

    public static string fileName(geographic pos, geographic inc, string ending = "txt") => $"lat={Math.Round(pos.lat, 2)}_lon={Math.Round(pos.lon, 2)}_+({inc.lat}_{inc.lon}).{ending}";
    public static string fileBoundaryName(geographic pos, geographic inc, string ending = "txt") => fileName(pos, inc).Replace(".txt", $"_boundary.{ending}");
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
