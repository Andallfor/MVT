using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Text;
using System.Runtime.InteropServices;

public class meshDistributor {
    public const int maxVertSize = 256;
}

public class meshDistributor<T> : meshDistributor where T : IMesh, new() {
    private Dictionary<Vector2Int, T> map = new Dictionary<Vector2Int, T>();
    public T baseType;

    public Vector2Int shape;

    public List<T> allMeshesOrdered;
    
    // try to create as many 255x255 meshes as possible
    public meshDistributor(Vector2Int size, Vector2Int maxSize, Vector2Int offset, bool reverse = false, Func<Vector2Int, Vector2> customUV = null, bool fastVerts = false, bool meshEdgeOffset = true) {
        baseType = new T();

        allMeshesOrdered = new List<T>();

        for (int y = 0; y < size.y; y += maxVertSize) {
            for (int x = 0; x < size.x; x += maxVertSize) {
                int xLeft = (x + maxVertSize >= size.x) ? size.x % maxVertSize : maxVertSize;
                int yLeft = (y + maxVertSize >= size.y) ? size.y % maxVertSize : maxVertSize;

                // dont create a mesh if it has 0 area
                if (xLeft != 0 && yLeft != 0) {
                    T t = new T();
                    Vector2Int _o = new Vector2Int(
                        x + maxVertSize > size.x ? 0 : 1,
                        y + maxVertSize > size.y ? 0 : 1);
                    
                    if (!meshEdgeOffset) _o = Vector2Int.zero;

                    t.init(xLeft + _o.x, yLeft + _o.y, new position(x + offset.x, y + offset.y, 0), new position(maxSize.x, maxSize.y, 0), reverse, customUV);
                    if (!fastVerts) t.prepareVerts();

                    map.Add(new Vector2Int(x, y), t);
                    allMeshesOrdered.Add(t);
                }
            }   
        }

        shape = new Vector2Int(Mathf.CeilToInt(size.x / maxVertSize), Mathf.CeilToInt(size.y / maxVertSize));
    }

    public void forceSetAllPoints(Vector3[] arr) {
        int offset = 0;
        Debug.Log(arr.Length);
        for (int i = 0; i < allMeshesOrdered.Count; i++) {
            T m = allMeshesOrdered[i];
            Array.Copy(arr, offset, m.verts, 0, m.verts.Length);

            offset += m.verts.Length;
        }
    }

    public void addPoint(int x, int y, position p) {
        int _x = x % maxVertSize;
        int _y = y % maxVertSize;

        Vector2Int key = new Vector2Int(x - _x, y - _y);

        T m = map[key];
        m.forceSetPoint(_x, _y, (Vector3) p);

        bool isX = _x == 0;
        bool isY = _y == 0;

        if (isX || isY) {
            if (key.x != 0 && key.y != 0 && isX && isY) {
                T _m = map[key - new Vector2Int(maxVertSize, maxVertSize)];
                _m.forceSetPoint(_m.shape.x - 1, _m.shape.y - 1, (Vector3) p);
            }
            if (key.x != 0 && isX) {
                T _m = map[key - new Vector2Int(maxVertSize, 0)];
                _m.forceSetPoint(_m.shape.x - 1, _y, (Vector3) p);
            }
            if (key.y != 0 && isY) {
                T _m = map[key - new Vector2Int(0, maxVertSize)];
                _m.forceSetPoint(_x, _m.shape.y - 1, (Vector3) p);
            }
        }
    }

    public void drawAll(Material mat, GameObject model, string[] name, Transform parent) {
        int i = 0;
        foreach (KeyValuePair<Vector2Int, T> kvp in map) {
            if (name.Length == 0) kvp.Value.drawMesh(mat, model, $"{kvp.Key.ToString()}", parent);
            else kvp.Value.drawMesh(mat, model, name[i], parent);
            i++;
        }
    }

    public void drawAll(Transform parent) {
        Material m = new Material(resLoader.load<Material>("defaultMat"));
        GameObject go = resLoader.load<GameObject>("terrainMesh");
        drawAll(m, go, new string[0], parent);
    }

    public GameObject draw(Vector2Int index, Material mat, GameObject model, string name, Transform parent) {
        return map[index].drawMesh(mat, model, name, parent);
    }

    public void clear() {
        foreach (T t in allMeshesOrdered) t.clearMesh();
        map = null;
        allMeshesOrdered = null;
        shape = Vector2Int.zero;
    }
}
