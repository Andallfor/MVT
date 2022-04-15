using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class meshDistributor<T> where T : IMesh, new()
{
    public static int maxVerts = 65535;
    private static int maxVertSize = 255;

    private Dictionary<Vector2Int, T> map = new Dictionary<Vector2Int, T>();

    public List<T> allMeshes {get => map.Values.ToList();}
    
    // try to create as many 255x255 meshes as possible
    public meshDistributor(Vector2Int size, Vector2Int maxSize, Vector2Int offset) {
        for (int x = 0; x < size.x; x += maxVertSize) {
            for (int y = 0; y < size.y; y += maxVertSize) {
                int xLeft = (x + maxVertSize >= size.x) ? maxVertSize - ((x + maxVertSize) % size.x) : maxVertSize;
                int yLeft = (y + maxVertSize >= size.y) ? maxVertSize - ((y + maxVertSize) % size.y) : maxVertSize;

                // dont create a mesh if it has 0 area
                if (xLeft != 0 && yLeft != 0) {
                    T t = new T();
                    t.init(xLeft, yLeft, new position(x + offset.x, y + offset.y, 0), new position(maxSize.x, maxSize.y, 0));
                    map.Add(new Vector2Int(x, y), t);
                }
            }   
        }
    }

    public void addPoint(int x, int y, geographic g, double h) {
        map[new Vector2Int(
            x - (x % maxVertSize),
            y - (y % maxVertSize))]
            .addPoint(x % maxVertSize, y % maxVertSize, g, h);
    }

    public void drawAll(Material mat, GameObject model, string[] name, Transform parent) {
        int i = 0;
        foreach (T t in map.Values) {
            if (name.Length == 0) t.drawMesh(mat, model, $"{i}", parent);
            else t.drawMesh(mat, model, name[i], parent);
            i++;
        }
    }
}
