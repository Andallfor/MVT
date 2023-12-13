using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

public static class terrainBenchmarking {
    public static bool isActive = false;
    private static StringBuilder sb;
    private static List<universalTerrainJp2File> allFiles = new List<universalTerrainJp2File>();
    private static Dictionary<string, double> standardDev = new Dictionary<string, double>();

    public static void init() {
        isActive = true;
        sb = new StringBuilder();

        string[] paths = Directory.GetDirectories(Path.Join(Application.streamingAssetsPath, "terrain/facilities/earth"));

        foreach (string path in paths) {
            string metadata = Path.Join(path, "metadata.txt");
            string data = Path.Join(path, "data.jp2");

            var file = new universalTerrainJp2File(path);
            allFiles.Add(file);
        }

        // first get the general info (without having to generate)
        genGeneralInfo();

        // now get the info that requires files to be generated
        genInfo();
        speedNoMesh();
        speedWithMesh();

        File.WriteAllText(Path.Join(KnownFolders.GetPath(KnownFolder.Downloads), "terrainBenchmarking.txt"), sb.ToString());
    }

    private static void genInfo() {
        elevation();
    }

    private static void elevation() {
        foreach (universalTerrainJp2File file in allFiles) {
            int[] heights = openJpegWrapper.requestTerrain(file.dataPath, Vector2Int.zero, new Vector2Int((int) file.ncols, (int) file.nrows), 0, 0);

            long avg = 0;
            for (int i = 0; i < heights.Length; i++) {
                if (heights[i] == 0) avg += 32767;
                else avg += heights[i];
            }

            long a = (int) ((avg / heights.LongLength) - 32767);

            // calc standard dev
            decimal aa = (decimal) a + 32767;
            decimal v = 0;
            for (int i = 0; i < heights.Length; i++) {
                decimal h = heights[i] == 0 ? 32757 : heights[i];
                v += (h - aa) * (h - aa);
            }
            v /= (decimal) heights.Length;

            double d = Math.Sqrt((double) v); // downcast cause no decimal sqrt lol
            standardDev[file.name] = d;

            write($"ele_{file.name}_avg", $"{a}m");
            write($"ele_{file.name}_std", $"{d}");
        }
    }

    private static void speedNoMesh() {
        long avg = 0;
        foreach (universalTerrainJp2File file in allFiles) {
            // warmup
            for (int i = 0; i < 5; i++) openJpegWrapper.requestTerrain(file.dataPath, Vector2Int.zero, new Vector2Int((int) file.ncols, (int) file.nrows), 0, 0);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int n = 20;
            for (int i = 0; i < n; i++) {
                openJpegWrapper.requestTerrain(file.dataPath, Vector2Int.zero, new Vector2Int((int) file.ncols, (int) file.nrows), 0, 0);
            }

            sw.Stop();

            long ms = sw.ElapsedMilliseconds / (long) n;
            long p = (long) (file.ncols * file.nrows);

            write($"t_no_mesh_{file.name}", $"{ms}ms");
            write($"npoints_{file.name}", $"{p}");
            write($"speed_no_mesh_{file.name}", $"{p / ms} points per ms");

            avg += p / ms;
        }

        write($"speed_no_mesh_all", $"{avg / allFiles.Count} points per ms");
    }

    private static void speedWithMesh() {
        long[] speed = new long[6] {0, 0, 0, 0, 0, 0};
        foreach (universalTerrainJp2File file in allFiles) {
            // warmup
            for (int i = 0; i < 5; i++) openJpegWrapper.requestTerrain(file.dataPath, Vector2Int.zero, new Vector2Int((int) file.ncols, (int) file.nrows), 0, 0);

            long[] resSpeed = new long[6] {0, 0, 0, 0, 0, 0};
            int n = 20;

            for (uint r = 0; r < 6; r++) {
                Stopwatch sw = new Stopwatch();

                for (int i = 0; i < n; i++) {
                    sw.Start();
                    var md = file.load(Vector2.zero, Vector2.one, 6371, r, new position(0, 0, 0));
                    md.drawAll(null);
                    sw.Stop();

                    md.clear();
                }

                long s = sw.ElapsedMilliseconds / (long) n;
                resSpeed[r] = s;
                speed[r] += s;
            }

            write($"speed_mesh_{file.name}", $"[{resSpeed[0]}, {resSpeed[1]}, {resSpeed[2]}, {resSpeed[3]}, {resSpeed[4]}, {resSpeed[5]}]ms");
        }

        for (int i = 0; i < speed.Length; i++) speed[i] /= allFiles.Count;
        write($"speed_mesh_all", $"[{speed[0]}, {speed[1]}, {speed[2]}, {speed[3]}, {speed[4]}, {speed[5]}]ms");
    }

    private static void genGeneralInfo() {
        // how many points total are represented
        nPoints();
    }

    private static void nPoints() {
        long n = 0;
        foreach (universalTerrainJp2File file in allFiles) n += (long) (file.nrows * file.ncols);

        write("npoints", $"{n}");
    }

    private static void write(string heading, string value) {
        sb.AppendLine($"{heading}: {value}");
    }
}
