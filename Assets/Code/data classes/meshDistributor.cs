using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class meshDistributor<T> where T : IMesh, new()
{
    public const int maxVerts = 65535;
    private const int maxVertSize = 250;

    private Dictionary<Vector2Int, T> map = new Dictionary<Vector2Int, T>();
    public T baseType;

    public List<T> allMeshes {get => map.Values.ToList();}
    
    // try to create as many 250x250 meshes as possible
    public meshDistributor(Vector2Int size, Vector2Int maxSize, Vector2Int offset, bool reverse = false, Func<Vector2Int, Vector2> customUV = null) {
        baseType = new T();

        for (int x = 0; x < size.x; x += maxVertSize) {
            for (int y = 0; y < size.y; y += maxVertSize) {
                int xLeft = (x + maxVertSize >= size.x) ? size.x % maxVertSize : maxVertSize;
                int yLeft = (y + maxVertSize >= size.y) ? size.y % maxVertSize : maxVertSize;

                // dont create a mesh if it has 0 area
                if (xLeft != 0 && yLeft != 0) {
                    T t = new T();
                    Vector2Int _o = new Vector2Int(
                        x + maxVertSize > size.x ? 0 : 1,
                        y + maxVertSize > size.y ? 0 : 1);

                    t.init(xLeft + _o.x, yLeft + _o.y, new position(x + offset.x, y + offset.y, 0), new position(maxSize.x, maxSize.y, 0), reverse, customUV);
                    map.Add(new Vector2Int(x, y), t);
                }
            }   
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
        GameObject go = resLoader.load<GameObject>("planetMesh");
        drawAll(m, go, new string[0], parent);
    }

    public GameObject draw(Vector2Int index, Material mat, GameObject model, string name, Transform parent) {
        return map[index].drawMesh(mat, model, name, parent);
    }

    public void clear() {
        foreach (T t in allMeshes) t.clearMesh();
    }
}
