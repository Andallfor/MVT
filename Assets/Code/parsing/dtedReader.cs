using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public static class dtedReader {
    public static int VOID_VALUE = -32767;
    public static void read(string filePath) {
        byte[] header = new byte[80];
        byte[] dsi = new byte[648];
        byte[] acc = new byte[2700];
        using (BinaryReader r = new BinaryReader(new FileStream(filePath, FileMode.Open))) {
            r.Read(header, 0, 80);
            r.Read(dsi, 0, 648);
            r.Read(acc, 0, 2700);
        }

        dtedHeader dh = new dtedHeader(header);
        Debug.Log(dh.origin);
        Debug.Log(dh.interval);
        Debug.Log(dh.vertAcc);
        Debug.Log(dh.multiAcc);
        Debug.Log(dh.sentinel);
        Debug.Log(dh.securityCode);
        Debug.Log(dh.reference);
        Debug.Log(dh.shape);

        Debug.Log("========");

        dtedDsi dd = new dtedDsi(dsi);
        Debug.Log(dd.sentinel);
        Debug.Log(dd.securityCode);
        Debug.Log(dd.releaseMarking);
        Debug.Log(dd.securityHandle);
        Debug.Log(dd.dtedLevel);
        Debug.Log(dd.producerCode);
        Debug.Log(dd.verticalDatum);
        Debug.Log(dd.horizontalDatum);
        Debug.Log(dd.collectionSystem);
        Debug.Log(dd.origin);
        Debug.Log(dd.sw);
        Debug.Log(dd.nw);
        Debug.Log(dd.ne);
        Debug.Log(dd.se);
        Debug.Log(dd.shape);
        Debug.Log(dd.interval);
        Debug.Log(dd.percentageFilled);
        Debug.Log(dd.reference);
        Debug.Log(dd.dataEdition);
        Debug.Log(dd.matchMergeVersion);
    }
}

public struct dtedHeader {
    public geographic origin;
    public double vertAcc;
    public bool multiAcc;
    public string sentinel, securityCode, reference;
    public position shape, interval; // z is ignored, interval is in seconds
    public dtedHeader(byte[] data) {
        dtedArrayWrapper<byte> reader = new dtedArrayWrapper<byte>(data);
        sentinel = general.parseByteArray(reader.read(4));
        string lon = general.parseByteArray(reader.read(8));
        string lat = general.parseByteArray(reader.read(8));
        origin = new geographic(lat, lon);
        double lonInterval = double.Parse(general.parseByteArray(reader.read(4))) / 10.0;
        double latInterval = double.Parse(general.parseByteArray(reader.read(4))) / 10.0;
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
    public int dataEdition;
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
        double latInterval = int.Parse(general.parseByteArray(reader.read(4))) / 10.0;
        double lonInterval = int.Parse(general.parseByteArray(reader.read(4))) / 10.0;
        interval = new position(lonInterval, latInterval, 0);

        double latShape = int.Parse(general.parseByteArray(reader.read(4)));
        double lonShape = int.Parse(general.parseByteArray(reader.read(4)));
        shape = new position(lonShape, latShape, 0);
        percentageFilled = int.Parse(general.parseByteArray(reader.read(2)));
        percentageFilled = percentageFilled == 0 ? 100 : percentageFilled;
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