using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using HtmlAgilityPack;
using System.Linq;
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

/*
To generate a texture for each dted file, we are using sentinel images. However, these images do not provide a perfect texture, as many areas are blacked out.
Because of this, we need to combine multiple sentinel textures to create one full texture. 
Due to the way the sentinel textures are formatted, one area can have multiple textures with different areas filled in and different areas left blank. We take these textures that cover the same area and effectively layer them on top of each other to mask the parts left blank.
However, the sentinel files are not perfectly accurate. The texture provided is a square, implying that either sides should be parallel to each other. But the area covered by the texture (including the blank areas) is not a square. There is a few percent difference between opposite sides, so there will be minor distortation in the resultant texture.
So, when overlayed on the dted mesh, the texture will be every so slightly off.
*/

public static class dtedImageCombiner
{
    private static List<sentinelFile> sentFiles = new List<sentinelFile>();
    private const double resolution = 10980; // This value must be <= then sentinelFile.imageSize
    public static Texture2D generateImage(geographic boundMin, geographic boundMax, string imageFolder, string outputFolder, string facilityName) {
        DirectoryInfo folder = new DirectoryInfo(imageFolder);
        foreach (DirectoryInfo tile in folder.EnumerateDirectories()) sentFiles.Add(new sentinelFile(tile.FullName));
        List<sentinelArea> areas = sentinelArea.sortFiles(sentFiles);

        double maxDist = Math.Max(boundMax.lon - boundMin.lon, boundMax.lat - boundMin.lat);
        double resX = resolution * (boundMax.lon - boundMin.lon) / maxDist;
        double resY = resolution * (boundMax.lat - boundMin.lat) / maxDist;

        Texture2D texture = new Texture2D((int) resX, (int) resY);

        double xIncrement = (boundMax.lon - boundMin.lon) / resX;
        double yIncrement = (boundMax.lat - boundMin.lat) / resY;

        for (double x = 0; x < resX; x++) {
            for (double y = 0; y < resY; y++) {
                // the color that will be drawn onto the picture
                Color32 c = new Color32(0, 0, 0, 0);
                geographic point = boundMin + new geographic(yIncrement * y, xIncrement * x);

                bool pixelExists = false;
                foreach (sentinelArea sa in areas) {
                    double xIndex = sentinelFile.imageSize * ((point.lon - sa.lines[3].xIntersection(point.lat)) / (sa.lines[1].xIntersection(point.lat) - sa.lines[3].xIntersection(point.lat)));
                    double yIndex = sentinelFile.imageSize - sentinelFile.imageSize * ((point.lat - sa.lines[2].yIntersection(point.lon)) / (sa.lines[0].yIntersection(point.lon) - sa.lines[2].yIntersection(point.lon)));

                    if (xIndex < 0 || xIndex > sentinelFile.imageSize - 1 || yIndex < 0 || yIndex > sentinelFile.imageSize - 1) continue;

                    foreach (sentinelFile sf in sa.files) {
                        // check to see if the image has the desired point
                        Rgb24 pixel = sf.texture[(int) Math.Round(xIndex), (int) Math.Round(yIndex)];

                        // if the pixel is black (0, 0, 0) that means that the file does not have the desired pixel
                        if (pixel.R == 0 && pixel.G == 0 && pixel.B == 0) continue;
                        else {
                            c = new Color32(pixel.R, pixel.G, pixel.B, 255);
                            break;
                        }                        
                    }

                    // if the alpha changes that means weve found a valid pixel, so no need to process more
                    if (c.a != 0) break;
                }

                if (pixelExists && c.a == 0) c = new Color32(0, 255, 255, 255);

                texture.SetPixel((int) x, (int) y, c);
            }    
        }

        File.WriteAllBytes(Path.Combine(outputFolder, $"{facilityName}.png"), texture.EncodeToJPG());
        File.WriteAllText(Path.Combine(outputFolder, $"{facilityName}.txt"), $"{boundMin.lat}, {boundMin.lon}, {boundMax.lat}, {boundMax.lon}");

        return texture;
    }

    /// <summary> Returns a csv in the format tile, centerlat, centerlon, NWlat, NWlon, NElat, NElon, SElat, SElon, SWlat, SWlon, NWlat, NWlon </summary>
    public static void parseSentinelKML(string path, string outputPath) {
        HtmlDocument doc = new HtmlDocument();
        doc.Load(path);

        List<HtmlNode> placemarks = doc.DocumentNode.SelectNodes("//placemark").ToList();

        StringBuilder csv = new StringBuilder();

        char[] seperator = new char[1] {' '};

        foreach (HtmlNode placemark in placemarks) {
            string tile = placemark.SelectSingleNode("name").InnerText;

            // points are in the form lon, lat, alt
            string[] centerArray = new string[2];
            placemark.SelectSingleNode("multigeometry/point/coordinates").InnerText.Trim().Replace('\n', ' ').Split(',').ToList().CopyTo(0, centerArray, 0, 2);
            string center = centerArray[1] + ", " + centerArray[0];

            csv.Append($"{tile}, {center}");

            // get boundary
            // some tiles have multiple boundaries (im not sure why), but we only care about the first one
            List<string> bounds = placemark.SelectSingleNode("multigeometry/polygon/outerboundaryis/linearring/coordinates").InnerText.Split(seperator, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string b in bounds) {
                List<string> split = b.Trim().Split(',').ToList();
                if (split.Count == 1) continue;

                string[] pointArray = new string[2];
                split.CopyTo(0, pointArray, 0, 2);
                string point = pointArray[1] + ", " + pointArray[0];
                point = point.Replace('\n', ' ');

                csv.Append($", {point}");
            }

            csv.Append('\n');
        }

        File.WriteAllText(outputPath, csv.ToString());
    }
}

public class sentinelArea {
    public static Dictionary<string, string> tileKey;
    public string tile;
    /// <summary> NW, NE, SE, SW </summary>
    public List<geographic> corners = new List<geographic>();
    /// <summary> N, E, S, W </summary>
    public List<double> sides = new List<double>();
    /// <summary> N, E, S, W </summary>
    public List<line> lines = new List<line>();
    public geographic center;
    public List<sentinelFile> files;

    public sentinelArea(string tile, List<sentinelFile> files) {
        this.tile = tile;
        this.files = files;

        // get the bounds and center data
        List<string> data = tileKey[tile].Split(new string[1] {", "}, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();

        string[] center = data.Take(2).ToArray();
        this.center = new geographic(double.Parse(center[0]), double.Parse(center[1]));

        List<string[]> bounds = new List<string[]>() {
            data.Skip(2).Take(2).ToArray(), // nw
            data.Skip(4).Take(2).ToArray(), // ne
            data.Skip(6).Take(2).ToArray(), // se
            data.Skip(8).Take(2).ToArray()  // sw
        };

        foreach (string[] corner in bounds) corners.Add(new geographic(double.Parse(corner[0]), double.Parse(corner[1])));

        sides.Add(Math.Max(corners[0].lat, corners[1].lat)); // N
        sides.Add(Math.Max(corners[1].lon, corners[2].lon)); // E
        sides.Add(Math.Min(corners[2].lat, corners[3].lat)); // S
        sides.Add(Math.Min(corners[3].lon, corners[1].lon)); // W

        lines = new List<line>() {
            new line(new List<geographic>() {corners[0], corners[1]}), // N
            new line(new List<geographic>() {corners[1], corners[2]}), // E
            new line(new List<geographic>() {corners[2], corners[3]}), // S
            new line(new List<geographic>() {corners[3], corners[0]})  // W
        };
    }

    public static List<sentinelArea> sortFiles(List<sentinelFile> files) {
        Dictionary<string, List<sentinelFile>> tiles = new Dictionary<string, List<sentinelFile>>();

        // sort the files in respect to their tile
        foreach (sentinelFile sf in files) {
            if (tiles.ContainsKey(sf.tile)) tiles[sf.tile].Add(sf);
            else tiles[sf.tile] = new List<sentinelFile>() {sf};
        }

        // create the sentinelArea
        List<sentinelArea> output = new List<sentinelArea>();
        foreach (List<sentinelFile> sfs in tiles.Values) output.Add(new sentinelArea(sfs[0].tile, sfs));

        return output;
    }
}

/// <summary> Represents a singular sentinel file (folder?). </summary>
public class sentinelFile {
    public List<geographic> bounds = new List<geographic>();
    public Image<Rgb24> texture;
    public string tile;
    public const int imageSize = 10980;
    public sentinelFile(string folder) {
        string pathToHtml = Path.Combine(folder, "HTML", "UserProduct_index.html");
        HtmlDocument doc = new HtmlDocument();
        doc.Load(pathToHtml);

        // parse the product uri
        HtmlNode uri = doc.DocumentNode.SelectSingleNode("//body/table/tr[last()]/td");
        tile = uri.InnerText.Split('_')[5].Remove(0, 1); // T at the start only indicates that it is a tile, which we dont care about

        // parse every value inside the Global Footprint header in the sentinel html file
        HtmlNode node = doc.DocumentNode.SelectSingleNode("//body").Elements("h1").ToList()[1];
        string boundString = node.NextSibling.InnerText.Replace('\t', ' ');
        string[] splitBoundString = boundString.Split(new char[1] {' '}, StringSplitOptions.RemoveEmptyEntries);
        List<geographic> boundCorners = new List<geographic>();
        for (int i = 0; i < splitBoundString.Length; i += 2) {
            double[] db = new double[2];
            for (int j = 0; j < 2; j++) {
                db[j] = double.Parse(splitBoundString[i + j]);
            }

            // each entry is lat, lon
            bounds.Add(new geographic(db[0], db[1]));
        }

        // load the pregenerated .png file
        // TODO: figure out how to parse the jpg2000 files without having the manually convert them
        DirectoryInfo di = new DirectoryInfo(Path.Combine(folder, "GRANULE")).GetDirectories()[0].GetDirectories().First(x => x.Name == "IMG_DATA");
        texture = Image<Rgb24>.Load(di.GetFiles().First(x => x.Name.Contains(".png")).FullName) as Image<Rgb24>;
    }
}
