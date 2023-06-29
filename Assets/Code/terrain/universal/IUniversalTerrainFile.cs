using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public abstract class IUniversalTerrainFile<T> where T : IMesh, new() {
    protected string dataPath {get; private set;}
    protected string metadataPath {get; private set;}
    public double cellSize {get; private set;}
    public double nrows {get; private set;}
    public double ncols {get; private set;}
    public uint res {get; private set;}
    public geographic llCorner {get; private set;}
    protected geographic size {get; private set;}
    protected universalTerrainFileSources src {get; private set;}
    protected Dictionary<string, double> metadata {get; private set;}

    protected IUniversalTerrainFile(string dataPath, string metadataPath, universalTerrainFileSources src) {
        this.dataPath = dataPath;
        this.metadataPath = metadataPath;
        this.src = src;

        loadMetadata();
    }

    private void loadMetadata() {
        metadata = new Dictionary<string, double>();

        string[] lines = File.ReadAllLines(this.metadataPath);
        foreach (string line in lines) {
            string[] data = line.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            metadata[data[0]] = Double.Parse(data[1]);
        }

        cellSize = metadata["cellsize"];
        llCorner = new geographic(metadata["yllcorner"], metadata["xllcorner"]);
        ncols = metadata["ncols"];
        nrows = metadata["nrows"];
        res = (uint) metadata["res"];
        size = new geographic(nrows * cellSize, ncols * cellSize);
    }

    public abstract meshDistributor<T> load(geographic start, geographic end, double radius, uint res);

    /// <summary> start and end are percents </summary>
    public abstract meshDistributor<T> load(Vector2 startPercent, Vector2 endPercent, double radius, uint res);
}

public enum universalTerrainFileSources {
    jp2
}