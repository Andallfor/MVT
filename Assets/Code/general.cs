using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class general
{
    public static Camera camera;
    public static Vector3 defaultCameraPosition = new Vector3(0, 0, -10);
    public static float defaultCameraFOV = 60;

    public static string parseByteArray(byte[] data) {
        string output = "";
        foreach (byte b in data) {
            output += (char) b;
        }
        return output;
    }

    public static string combineCharArray(char[] data) {
        string output = "";
        foreach (char b in data) {
            output += b;
        }
        return output;
    }
}
