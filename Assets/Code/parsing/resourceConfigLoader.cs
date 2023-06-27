using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class resLoader {
    private static bool initialized = false;
    private static Dictionary<string, string> data = new Dictionary<string, string>();

    public static void initialize() {
        if (initialized) {
            Debug.LogWarning("Resource Config Loader has already been initialized.");
            return;
        }
        initialized = true;

        TextAsset config = Resources.Load<TextAsset>("config");
        string[] lines = config.text.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);

        foreach (string s in lines) {
            if (s.StartsWith("#")) continue; // is a comment, ignore

            string[] parts = s.Split(new string[] {": "}, StringSplitOptions.RemoveEmptyEntries);
            parts[1] = parts[1].Trim();
            data[parts[0].ToLower()] = parts[1];
        }
    }

    public static string getPath(string name) {
        if (!initialized) throw new KeyNotFoundException("Resource Config Loader has not yet been initialized!");
        return data[name.ToLower()];
    }

    public static T load<T>(string name) where T : UnityEngine.Object {
        if (!initialized) throw new KeyNotFoundException("Resource Config Loader has not yet been initialized!");
        return Resources.Load<T>(data[name.ToLower()]);
    }

    public static bool containsName(string name) {
        if (!initialized) throw new KeyNotFoundException("Resource Config Loader has not yet been initialized!");
        return data.ContainsKey(name.ToLower());
    }

    public static GameObject createPrefab(string name) {
        if (!initialized) throw new KeyNotFoundException("Resource Config Loader has not yet been initialized!");
        return GameObject.Instantiate(load<GameObject>(name));
    }

    public static GameObject createPrefab(string name, Transform t) {
        if (!initialized) throw new KeyNotFoundException("Resource Config Loader has not yet been initialized!");
        return GameObject.Instantiate(load<GameObject>(name), t);
    }

    public static GameObject createPrefab(string name, Vector3 v, Quaternion q) {
        if (!initialized) throw new KeyNotFoundException("Resource Config Loader has not yet been initialized!");
        return GameObject.Instantiate(load<GameObject>(name), v, q);
    }

    public static GameObject createPrefab(string name, Vector3 v, Quaternion q, Transform t) {
        if (!initialized) throw new KeyNotFoundException("Resource Config Loader has not yet been initialized!");
        return GameObject.Instantiate(load<GameObject>(name), v, q, t);
    }
}
