using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Text;

public static class dtedReader {
    public const int VOID_VALUE = -32767;
    public const int uhlLength = 80;
    public const int dsiLength = 648;
    public const int accLength = 2700;
    public const int textureSize = 10980;

    public static dtedInfo readDted(string dtedPath, string imageBoundsPath, bool saveData = false) {
        FileStream fs = new FileStream(dtedPath, FileMode.Open);

        byte[] uhl = new byte[uhlLength];
        byte[] dsi = new byte[dsiLength];
        byte[] acc = new byte[accLength];
        byte[] data = new byte[fs.Length - (uhlLength + dsiLength + accLength)];
        using (BinaryReader r = new BinaryReader(fs)) {
            r.Read(uhl, 0, 80);
            r.Read(dsi, 0, 648);
            r.Read(acc, 0, 2700);
            r.Read(data, 0, data.Length);
        }

        fs.Close();

        dtedUhl dh = new dtedUhl(uhl);
        dtedDsi dd = new dtedDsi(dsi);
        dtedAcc da = new dtedAcc(acc);


        string[] bounds = File.ReadAllText(imageBoundsPath).Split(',');
        geographic sw = new geographic(double.Parse(bounds[0].Trim()), double.Parse(bounds[1].Trim()));
        geographic ne = new geographic(double.Parse(bounds[2].Trim()), double.Parse(bounds[3].Trim()));


        meshDistributor<dtedBasedMesh> distributor = new meshDistributor<dtedBasedMesh>(
            new Vector2Int((int) dh.shape.x, (int) dh.shape.y),
            Vector2Int.zero, Vector2Int.zero,
            true,
            (Vector2Int v) => {
                geographic p = dd.sw + new geographic((double) v.x / dh.shape.y, (double) v.y / dh.shape.x);

                return new Vector2(
                    (float) ((p.lon - sw.lon) / (ne.lon - sw.lon)),
                    (float) ((p.lat - sw.lat) / (ne.lat - sw.lat)));
            }
        );

        /*
        format of data blocks:
        sentinel
        lon, lat
        [e1, e2, e3, e4...]
        checksum
        */

        position offset = centerDtedPoint(dd.sw - new geographic(0.5, 0.5), dd.sw, new position(0, 0, 0), 0).swapAxis();
        double[] points = new double[0];
        if (saveData) points = new double[(int) (dh.shape.x * dh.shape.y)];

        for (int i = 0; i < data.Length / dd.dataBlockLength; i++) {
            // throw everything into an array
            byte[] block = data.Skip(i * dd.dataBlockLength).Take(dd.dataBlockLength).ToArray();
            dtedArrayWrapper<byte> reader = new dtedArrayWrapper<byte>(block);
            reader.read(4);

            double lon = (double) BitConverter.ToInt16(reader.read(2).Reverse().ToArray(), 0) * dh.interval.x + dd.sw.lon;
            double lat = (double) BitConverter.ToInt16(reader.read(2).Reverse().ToArray(), 0) * dh.interval.y + dd.sw.lat;
            geographic origin = new geographic(lat, lon);

            for (int j = 0; j < dd.dataBlockLength - 12; j += 2) { // first 8 bytes are not needed, last 4 are checksum
                int index = j / 2;
                byte[] _b = reader.read(2).Reverse().ToArray();
                double h = BitConverter.ToInt16(_b, 0);
                geographic g = new geographic(index * dh.interval.y, 0) + origin;
                distributor.addPoint(index, i, centerDtedPoint(dd.sw, g, offset, h));

                // points are given in lat... lon order but we want it to be the other way around 
                // (x corresponds with lon, y with lat) so invert
                if (saveData) points[i + index * ((dd.dataBlockLength - 12) / 2)] = h;
            }
            reader.read(4);
        }

        return new dtedInfo(dh, dd, da, distributor, points);
    }

    public static position centerDtedPoint(geographic sw, geographic g, position p, double h) => (g.toCartesian(6371.0 + h / 1000.0)
        .rotate(0, 0, (-sw.lon - 0.5) * (Mathf.PI / 180.0))
        .rotate((270.0 + sw.lat) * (Math.PI / 180.0), 0, 0)
        - p)
        .swapAxis();
}

public class dtedBasedMesh : IMesh {
    public geographic sw;
    public position offset;
    public override Vector3 addPoint(int x, int y, geographic g, double h) => Vector3.zero;
}

public struct dtedInfo {
    public dtedUhl du;
    public dtedDsi dd;
    public dtedAcc da;
    public meshDistributor<dtedBasedMesh> distributor;
    public double[] points;

    public dtedInfo(dtedUhl du, dtedDsi dd, dtedAcc da, meshDistributor<dtedBasedMesh> m, double[] points) {
        this.du = du;
        this.dd = dd;
        this.da = da;
        this.distributor = m;
        this.points = points;
    }
}

public struct dtedAcc {
    public string sentinel;
    public position acc, relAcc;

    public dtedAcc(byte[] data) {
        dtedArrayWrapper<byte> reader = new dtedArrayWrapper<byte>(data);
        sentinel = general.parseByteArray(reader.read(3));
        string _ah = general.parseByteArray(reader.read(4));
        string _av = general.parseByteArray(reader.read(4));
        string _rah = general.parseByteArray(reader.read(4));
        string _rav = general.parseByteArray(reader.read(4));
        double ah = _ah.Contains("NA") ? 0 : double.Parse(_ah);
        double av = _av.Contains("NA") ? 0 : double.Parse(_av);
        double rah = _rah.Contains("NA") ? 0 : double.Parse(_rah);
        double rav = _rav.Contains("NA") ? 0 : double.Parse(_rav);

        acc = new position(ah, av, 0);
        relAcc = new position(rah, rav, 0);
    }
}

public struct dtedUhl {
    public geographic origin;
    public double vertAcc;
    public bool multiAcc;
    public string sentinel, securityCode, reference;
    public position shape, interval; // z is ignored, interval is in seconds
    public dtedUhl(byte[] data) {
        dtedArrayWrapper<byte> reader = new dtedArrayWrapper<byte>(data);
        sentinel = general.parseByteArray(reader.read(4));
        string lon = general.parseByteArray(reader.read(8));
        string lat = general.parseByteArray(reader.read(8));
        origin = new geographic(lat, lon);
        double lonInterval = double.Parse(general.parseByteArray(reader.read(4))) / 36000.0;
        double latInterval = double.Parse(general.parseByteArray(reader.read(4))) / 36000.0;
        interval = new position(latInterval, lonInterval, 0);
        string _vertAcc = general.parseByteArray(reader.read(4));
        vertAcc = _vertAcc.Contains("NA") ? -1 : double.Parse(_vertAcc);
        securityCode = general.parseByteArray(reader.read(3));
        reference = general.parseByteArray(reader.read(12));
        shape = new position(int.Parse(general.parseByteArray(reader.read(4))), 
                             int.Parse(general.parseByteArray(reader.read(4))), 0);
        multiAcc = general.parseByteArray(reader.read(1)).Contains('0');
    }
}

public struct dtedDsi {
    public string sentinel, securityCode, releaseMarking, securityHandle, dtedLevel, producerCode, verticalDatum, horizontalDatum, collectionSystem, reference;
    public geographic origin, sw, nw, ne, se;
    public position shape, interval; // z is ignored
    public double percentageFilled;
    public int dataEdition, dataBlockLength;
    public char matchMergeVersion;

    public dtedDsi(byte[] data) {
        dtedArrayWrapper<byte> reader = new dtedArrayWrapper<byte>(data);
        sentinel = general.parseByteArray(reader.read(3));
        securityCode = general.parseByteArray(reader.read(1));
        releaseMarking = general.parseByteArray(reader.read(2));
        securityHandle = general.parseByteArray(reader.read(27));
        reader.read(26);
        dtedLevel = general.parseByteArray(reader.read(5));
        reference = general.parseByteArray(reader.read(15));
        reader.read(8);
        dataEdition = int.Parse(general.parseByteArray(reader.read(2)));
        matchMergeVersion = (char) (general.parseByteArray(reader.read(1)).First());
        reader.read(12);
        producerCode = general.parseByteArray(reader.read(8));
        reader.read(31);
        verticalDatum = general.parseByteArray(reader.read(3));
        horizontalDatum = general.parseByteArray(reader.read(5));
        collectionSystem = general.parseByteArray(reader.read(10));
        reader.read(26);

        origin = new geographic(
            general.parseByteArray(reader.read(9)),
            general.parseByteArray(reader.read(10)));
        
        sw = new geographic(
            general.parseByteArray(reader.read(7)),
            general.parseByteArray(reader.read(8)));
        
        nw = new geographic(
            general.parseByteArray(reader.read(7)),
            general.parseByteArray(reader.read(8)));

        ne = new geographic(
            general.parseByteArray(reader.read(7)),
            general.parseByteArray(reader.read(8)));
        
        se = new geographic(
            general.parseByteArray(reader.read(7)),
            general.parseByteArray(reader.read(8)));
        
        reader.read(9);
        double latInterval = double.Parse(general.parseByteArray(reader.read(4))) / 36000.0;
        double lonInterval = double.Parse(general.parseByteArray(reader.read(4))) / 36000.0;
        interval = new position(lonInterval, latInterval, 0);

        double latShape = int.Parse(general.parseByteArray(reader.read(4)));
        double lonShape = int.Parse(general.parseByteArray(reader.read(4)));
        shape = new position(lonShape, latShape, 0);
        percentageFilled = int.Parse(general.parseByteArray(reader.read(2)));
        percentageFilled = percentageFilled == 0 ? 100 : percentageFilled;

        dataBlockLength = (int) (12 + (2 * shape.y));
    }
}

internal struct dtedArrayWrapper<T> {
    private T[] data;
    private int dataLength, readIndex;

    public dtedArrayWrapper(T[] data) {
        this.data = data;
        this.dataLength = data.Length;
        this.readIndex = 0;
    }

    public T[] read(int count) {
        T[] output = data.Skip(readIndex).Take(count).ToArray();
        readIndex += count;
        return output;
    }
}