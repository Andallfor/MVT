using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using System.Threading.Tasks;

public class planetTerrainFile
{
    public string name, path;
    public geographic increment, geoPosition; // note that position is lower left
    public position cartPosition;
    public planetTerrainFolderInfo ptfi;
    public double ncols, nrows;

    public planetTerrainFile(string path, planetTerrainFolderInfo ptfi)
    {
        name = path.Split('\\').Last(); // get name of file without path
        name = name.Replace(".txt", ""); // without ext
        this.path = path;

        string _increment = name.Split('+').Last();
        _increment = new string(_increment.Where(x => !(x == '(' || x == ')')).ToArray());
        increment = new geographic(
            double.Parse(_increment.Split('_').First(), System.Globalization.NumberStyles.Any),
            double.Parse(_increment.Split('_').Last(), System.Globalization.NumberStyles.Any));
        
        string[] pos = name.Split('_').Take(2).ToArray();
        geoPosition = new geographic(
            double.Parse(pos[0].Split('=').Last(), System.Globalization.NumberStyles.Any),
            double.Parse(pos[1].Split('=').Last(), System.Globalization.NumberStyles.Any));
        
        this.ptfi = ptfi;
        cartPosition = new position(
            ptfi.pointsPerCoord * (geoPosition.lon + 180.0),
            ptfi.pointsPerCoord * (geoPosition.lat + 90.0),
            0);
        
        ncols = (int) (increment.lon * ptfi.pointsPerCoord);
        nrows = (int) (increment.lat * ptfi.pointsPerCoord);
    }

    public void generate(planetTerrainMesh m)
    {
        string[] data = File.ReadAllLines(path);
        data = data.Skip(6).ToArray();

        int y = (int) nrows - 1;
        foreach (string r in data)
        {
            string[] row = r.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();
            int x = 0;
            foreach (string h in row)
            {
                double value = double.Parse(h, System.Globalization.NumberStyles.Any);
                geographic geoPos = cartToGeo(x, y);

                m.addPoint(x + 1, y + 1, geoPos, value);

                x++;
            }
            y--;
        }

        m.drawBoundaries(Path.Combine(m.ptfi.folderPath, terrainProcessor.fileBoundaryName(geoPosition, increment)));

        data = new string[0];
    }

    public geographic cartToGeo(int x, int y) => geoPosition + new geographic(
        (y / nrows) * increment.lat,
        (x / ncols) * increment.lon);

    public override string ToString() => $"{name}: {geoPosition} + {increment}";
    public override int GetHashCode() => name.GetHashCode();
}

public class planetTerrainFolderInfo
{
    public readonly double ncols, nrows, cellsize, pointsPerCoord, filesPerTile, genStep;
    public readonly string name, folderPath;
    public readonly geographic increment;
    public readonly List<Bounds> allBounds;

    public planetTerrainFolderInfo(string folderPath)
    {
        string[] data = File.ReadAllLines(Path.Combine(folderPath, terrainProcessor.folderInfoName));

        ncols = double.Parse(read(data[0]), System.Globalization.NumberStyles.Any);
        nrows = double.Parse(read(data[1]), System.Globalization.NumberStyles.Any);
        cellsize = double.Parse(read(data[2]), System.Globalization.NumberStyles.Any);
        pointsPerCoord = double.Parse(read(data[3]), System.Globalization.NumberStyles.Any);
        filesPerTile = double.Parse(read(data[4]), System.Globalization.NumberStyles.Any);
        genStep = double.Parse(read(data[5]), System.Globalization.NumberStyles.Any);

        name = read(data[6]);
        this.folderPath = folderPath;

        string[] inc = (new string(read(data[7]).Where(x => !(x == '(' || x == ')')).ToArray())).Split('_').ToArray();
        increment = new geographic(
            double.Parse(inc[0].Split('=').Last(), System.Globalization.NumberStyles.Any),
            double.Parse(inc[1].Split('=').Last(), System.Globalization.NumberStyles.Any));
        
        allBounds = new List<Bounds>();
        for (double lat = -90; lat < 90; lat += increment.lat)
        {
            for (double lon = -180; lon < 180; lon += increment.lon)
            {
                allBounds.Add(new Bounds(
                    new Vector3((float) (lon + increment.lon / 2.0), (float) (lat + increment.lat / 2.0), 0),
                    new Vector3((float) (increment.lon), (float) (increment.lat), 1)));
            }
        }
    }

    private string read(string s) => s.Split(' ').Last();

    // expands each bound to the closest points that are on increment
    public Bounds pullBoundsToEnclosing(Bounds b)
    {
        b.min = new Vector3(
            b.min.x - (b.min.x % (int) increment.lon),
            b.min.y - (b.min.y % (int) increment.lat), 0);
        b.max = new Vector3(
            ((int) increment.lon) + (b.max.x - (b.max.x % (int) increment.lon)),
            ((int) increment.lat) + (b.max.y - (b.max.y % (int) increment.lat)), 1);
        return b;
    }
}

