using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.IO;

/// <summary> Static class that holds most common information regarding frontend stuff. </summary>
/// <remarks>See <see cref="master"/> for the backend version of <see cref="general"/>. </remarks>
public static class general
{
    /// <summary> Reference to the player's camera in the scene. </summary>
    /// <remarks> Use this instead of <see cref="Camera.main"/> as this is much more efficient. </remarks>
    public static Camera camera;
    /// <summary> The Canvas for the UI. </summary>
    public static Canvas canvas = GameObject.FindGameObjectWithTag("ui/canvas").GetComponent<Canvas>();
    /// <summary> Reference to the gameObject that holds all the planets. </summary>
    public static GameObject planetParent = GameObject.FindGameObjectWithTag("planet/parent");

    /// <summary> Default position of the camera. </summary>
    public static Vector3 defaultCameraPosition = new Vector3(0, 0, -10);

    /// <summary> Default FOV of camera. </summary>
    public static float defaultCameraFOV = 60;
    public static event EventHandler onStatusChange = delegate {};
    public static void notifyStatusChange() {onStatusChange(null, EventArgs.Empty);}
    public static controller main;

    /// <summary> Parse an array of bytes into a string. </summary>
    public static string parseByteArray(byte[] data) {
        string output = "";
        foreach (byte b in data) {
            output += (char) b;
        }
        return output;
    }

    /// <summary> Combine an array of chars into a string. </summary>
    public static string combineCharArray(char[] data) {
        string output = "";
        foreach (char b in data) {
            output += b;
        }
        return output;
    }

    public static IEnumerator internalClock(float tickRate, int requestedTicks, Action<int> callback, Action termination)
    {
        float timePerTick = 1000f * (60f / tickRate);
        float tickBucket = 0;
        int tickCount = 0;

        while (tickCount < requestedTicks)
        {
            tickBucket += UnityEngine.Time.deltaTime * 1000f;
            int ticks = (int) Math.Round((tickBucket - (tickBucket % timePerTick)) / timePerTick);
            tickBucket -= ticks *  timePerTick;

            for (int i = 0; i < ticks; i++)
            {
                callback(tickCount);
                tickCount++;
                if (tickCount < requestedTicks) break;
            }

            // using this timer method instead of WaitForSeconds as it is inaccurate for small numbers
            yield return new WaitForEndOfFrame();
        }

        termination();
    }
}

public class webRequest
{
    public string result;

    public void download(string file, Action<string> callback) {
        general.main.StartCoroutine(_download(file, callback));
    }

    private IEnumerator _download(string file, Action<string> callback) {
        #if UNITY_WEBGL
        string url = Path.Combine(Application.streamingAssetsPath, file);
        #elif UNITY_EDITOR    
        string url = $"http://localhost:65020/StreamingAssets/{file}";
        #endif

        using (UnityWebRequest uwr = UnityWebRequest.Get(url)) {
            yield return uwr.SendWebRequest();

            result = uwr.downloadHandler.text;
        }

        callback(result);
    }
}