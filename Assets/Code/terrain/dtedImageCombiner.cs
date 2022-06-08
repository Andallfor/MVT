using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using HtmlAgilityPack;
using System.Linq;
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/*
To generate a texture for each dted file, we are using sentinel images. However, these images do not provide a perfect texture, as many areas are blacked out.
Because of this, we need to combine multiple sentinel textures to create one full texture. 
Due to the way the sentinel textures are formatted, one area can have multiple textures with different areas filled in and different areas left blank. We take these textures that cover the same area and effectively layer them on top of each other to mask the parts left blank.
However, the sentinel files are not perfectly accurate. The texture provided is a square, implying that either sides should be parallel to each other. But the area covered by the texture (including the blank areas) is not a square. There is a few percent difference between opposite sides, so there will be minor distortation in the resultant texture.
However, because each texture covers the exact same area, there should not be any noticable image splicing- its just that when overlayed on the dted mesh, the texture will be every so slightly off.
TODO: This may be something we want to account for later, but for the moment the percent diff is small enough that it should be fine to ignore.
*/


public static class dtedImageCombiner
{
    private static List<sentinelFile> sentFiles = new List<sentinelFile>();
    private const double resolution = 8192; // This value must be less then sentinelFile.imageSize
    public static Texture2D generateImage(geographic boundMin, geographic boundMax, string imageFolder) {
        DirectoryInfo folder = new DirectoryInfo(imageFolder);
        foreach (DirectoryInfo tile in folder.EnumerateDirectories()) {
            sentFiles.Add(new sentinelFile(tile.FullName));
        }

        // generate bounds for the files, assuming they all cover the same region
        List<geographic> allBounds = new List<geographic>();
        foreach (sentinelFile sf in sentFiles) allBounds.AddRange(sf.bounds);
        // because the shape may be slightly irregular, we cant just get the bottom/right/etc most point
        // as they may not actually be a corner
        double maxN = -1000, maxS = 1000, maxE = -1000, maxW = 1000;
        foreach (geographic point in allBounds) {
            if (point.lat > maxN) maxN = point.lat;
            if (point.lat < maxS) maxS = point.lat;
            if (point.lon > maxE) maxE = point.lon;
            if (point.lon < maxW) maxW = point.lon;
        }

        // predict the center of each corner
        double cornerRadius = boundMax.distAs2DVector(boundMin) * 0.1;
        List<geographic> predictedBounds = new List<geographic>() {
            new geographic(maxN, maxE), // NE
            new geographic(maxN, maxW), // NW
            new geographic(maxS, maxE), // SE
            new geographic(maxS, maxW)  // SW
        };

        geographic boundCenter = boundMin + (boundMax - boundMin) / 2.0;
        List<geographic> corners = new List<geographic>();
        foreach (geographic p in predictedBounds) {
            double best = 0;
            geographic corner = new geographic(0, 0);
            foreach (geographic g in allBounds) {
                // within the predicted area of each corner, cycle throw all the points in that area
                if (p.distAs2DVector(g) < cornerRadius) {
                    // the corner of a square/rect will have the greatest magntitude compared to the points near it, so the point
                    // that we find that has the greatest magntitude to the center of the bounds is considered the corner
                    double newMag = (p - boundCenter).magnitude();
                    if (newMag > best) {
                        best = newMag;
                        corner = p;
                    }
                }
            }

            if (corner == new geographic(0, 0)) throw new Exception("Unable to find valid corner");

            // because the order of foreach loops is not random, the resultant corners list will also be in the form NE, NW, SE, SW
            corners.Add(corner);
        }

        Texture2D texture = new Texture2D((int) resolution, (int) resolution);

        double xIncrement = (boundMax.lon - boundMin.lon) / resolution;
        double yIncrement = (boundMax.lat - boundMin.lat) / resolution;

        for (double x = 0; x < resolution; x++) {
            for (double y = 0; y < resolution; y++) {
                geographic point = boundMin + new geographic(yIncrement * y, xIncrement * x);
                double xIndex = sentinelFile.imageSize * ((point.lon - corners[3].lon) / (corners[2].lon - corners[3].lon));
                double yIndex = sentinelFile.imageSize * ((point.lat - corners[3].lat) / (corners[1].lat - corners[3].lat));

                Color32 c = new Color32(0, 0, 0, 0);
                // point requested is in the bounds of the sentinel file
                if (yIndex >= 0 && xIndex >= 0 && yIndex < sentinelFile.imageSize && xIndex < sentinelFile.imageSize) {
                    foreach (sentinelFile sf in sentFiles) {
                        // idk why its max - yIndex, it just works
                        Rgb24 pixel = sf.texture[(int) xIndex, sentinelFile.imageSize - 1 - (int) yIndex];
                        // if the pixel is black
                        if (pixel.R == 0 && pixel.G == 0 && pixel.B == 0) continue;
                        else {
                            c = new Color32(pixel.R, pixel.G, pixel.B, 255);
                            break;
                        }
                    }
                }

                if (c.a == 0) c = new Color32(255, 255, 255, 255);
                texture.SetPixel((int) x, (int) y, c);
            }    
        }

        File.WriteAllBytes("C:/Users/leozw/Desktop/dteds/newTexture.png", texture.EncodeToJPG());

        return texture;
    }
}

/// <summary> Represents a singular sentinel file (folder?). </summary>
public class sentinelFile {
    public List<geographic> bounds = new List<geographic>();
    public Image<Rgb24> texture;
    public const int imageSize = 10980;
    public sentinelFile(string folder) {
        string pathToHtml = Path.Combine(folder, "HTML", "UserProduct_index.html");
        HtmlDocument doc = new HtmlDocument();
        doc.Load(pathToHtml);

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
