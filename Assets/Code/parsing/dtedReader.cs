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