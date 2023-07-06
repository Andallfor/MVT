using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class IMesh
{
    protected int[] triangles = new int[0];
    protected Vector2[] uvs = new Vector2[0];
    public Vector3[] verts;
    protected Mesh mesh;
    public Vector2Int shape {get; protected set;}
    protected GameObject go;

    protected static Dictionary<Vector2Int, int[]> trianglesForwards = new Dictionary<Vector2Int, int[]>();
    protected static Dictionary<Vector2Int, int[]> trianglesBackwards = new Dictionary<Vector2Int, int[]>();
    protected static Dictionary<Vector2Int, Vector2[]> defaultUVs = new Dictionary<Vector2Int, Vector2[]>();

    private position mapSize;

    public void init(int cols, int rows, position initialPosition, position mapSize, bool reverse = false, Func<Vector2Int, Vector2> customUV = null) {
        shape = new Vector2Int(cols, rows);
        verts = new Vector3[cols * rows];
        triangles = new int[(rows - 1) * (cols - 1) * 6];
        uvs = new Vector2[rows * cols];

        this.mapSize = mapSize;

        bool trianglesSatisfied = false;
        if (reverse && trianglesBackwards.ContainsKey(shape)) {
            triangles = trianglesBackwards[shape];
            trianglesSatisfied = true;
        } else if (trianglesForwards.ContainsKey(shape)) {
            triangles = trianglesForwards[shape];
            trianglesSatisfied = true;
        }

        bool uvSatisfied = false;
        bool usingCustomUV = customUV != null;
        if (!usingCustomUV) {
            customUV = defaultUV;
            if (defaultUVs.ContainsKey(shape)) {
                uvs = defaultUVs[shape];
                uvSatisfied = true;
            }
        }

        if (!uvSatisfied || !trianglesSatisfied) {
            int index = 0;
            for (int y = 0; y < rows; y++) {
                for (int x = 0; x < cols; x++) {
                    if (!trianglesSatisfied && x != cols - 1 && y != rows - 1) {
                        if (reverse) {
                            triangles[index++] = toIndex(x + 1, y + 1);
                            triangles[index++] = toIndex(x, y + 1);
                            triangles[index++] = toIndex(x, y);

                            triangles[index++] = toIndex(x + 1, y);
                            triangles[index++] = toIndex(x + 1, y + 1);
                            triangles[index++] = toIndex(x, y);
                        } else {
                            triangles[index++] = toIndex(x, y);
                            triangles[index++] = toIndex(x, y + 1);
                            triangles[index++] = toIndex(x + 1, y + 1);
                            
                            triangles[index++] = toIndex(x, y);
                            triangles[index++] = toIndex(x + 1, y + 1);
                            triangles[index++] = toIndex(x + 1, y);
                        }
                    }

                    if (!uvSatisfied) uvs[toIndex(x, y)] = customUV(new Vector2Int(x + (int) initialPosition.x, y + (int) initialPosition.y));
                }
            }
        }

        if (reverse) trianglesBackwards[shape] = triangles;
        else trianglesForwards[shape] = triangles;
        if (!usingCustomUV) defaultUVs[shape] = uvs;
    }

    private Vector2 defaultUV(Vector2Int v) => new Vector2((float) (v.x / mapSize.x), (float) (v.y / mapSize.y));

    public GameObject drawMesh(Material mat, GameObject model, string name, Transform parent) {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();

        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        
        go.GetComponent<MeshRenderer>().material = mat;
        go.GetComponent<MeshFilter>().mesh = mesh;

        this.triangles = new int[0];
        this.uvs = new Vector2[0];
        this.verts = new Vector3[0];

        return go;
    }

    public void addCollider() {
        go.AddComponent<MeshCollider>();
    }

    public void clearMesh() {
        this.mesh.Clear();
        this.mesh = new Mesh();
        GameObject.Destroy(go);
    }

    public void hide() {go.GetComponent<MeshRenderer>().enabled = false;}

    public void show() {go.GetComponent<MeshRenderer>().enabled = true;}

    public void forceSetPoint(int x, int y, Vector3 v) => this.verts[toIndex(x, y)] = v;

    public abstract Vector3 addPoint(int x, int y, geographic g, double h);

    public int toIndex(int x, int y) => y * shape.x + x;
}
