using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class IMesh {
    public Vector3[] verts;
    public Vector2[] uvs;
    protected Mesh mesh;
    public Vector2Int shape {get; protected set;}
    protected GameObject go;
    protected bool reverse, usingCustomUV;

    protected static Dictionary<Vector2Int, int[]> trianglesForwards = new Dictionary<Vector2Int, int[]>();
    protected static Dictionary<Vector2Int, int[]> trianglesBackwards = new Dictionary<Vector2Int, int[]>();
    protected static Dictionary<Vector2Int, Vector2[]> defaultUVs = new Dictionary<Vector2Int, Vector2[]>();

    private position mapSize;

    public void init(int cols, int rows, position initialPosition, position mapSize, bool reverse = false, Func<Vector2Int, Vector2> customUV = null) {
        shape = new Vector2Int(cols, rows);
        this.reverse = reverse;

        this.mapSize = mapSize;

        bool trianglesSatisfied = false;
        if ((reverse && trianglesBackwards.ContainsKey(shape)) || (!reverse && trianglesForwards.ContainsKey(shape))) trianglesSatisfied = true;

        bool uvSatisfied = false;
        usingCustomUV = customUV != null;
        if (!usingCustomUV) {
            customUV = defaultUV;
            if (defaultUVs.ContainsKey(shape)) uvSatisfied = true;
        }

        int[] triangles = null;
        uvs = null;
        if (!trianglesSatisfied) triangles = new int[(rows - 1) * (cols - 1) * 6];
        if (!uvSatisfied) uvs = new Vector2[rows * cols];

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

        if (!trianglesSatisfied) {
            if (reverse) trianglesBackwards[shape] = triangles;
            else trianglesForwards[shape] = triangles;
        }
        if (!usingCustomUV && !uvSatisfied) defaultUVs[shape] = uvs;
    }

    public void prepareVerts() {
        verts = new Vector3[shape.x * shape.y];
    }

    private Vector2 defaultUV(Vector2Int v) => new Vector2((float) (v.x / mapSize.x), (float) (v.y / mapSize.y));

    public GameObject drawMesh(Material mat, GameObject model, string name, Transform parent) {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;

        if (reverse) mesh.triangles = trianglesBackwards[shape];
        else mesh.triangles = trianglesForwards[shape];

        if (usingCustomUV) mesh.uv = uvs;
        else mesh.uv = defaultUVs[shape];

        mesh.RecalculateNormals();

        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        
        go.GetComponent<MeshRenderer>().material = mat;
        go.GetComponent<MeshFilter>().mesh = mesh;

        this.uvs = null;
        this.verts = new Vector3[0];

        return go;
    }

     public GameObject drawMeshTimed(Material mat, GameObject model, string name, Transform parent, ref long tinit, ref long ttriangle, ref long tuv, ref long tvert, ref long tnormal, ref long tinstantiate) {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        tinit += sw.ElapsedMilliseconds;
        sw.Restart();

        mesh.vertices = verts;
        tvert += sw.ElapsedMilliseconds;
        sw.Restart();

        if (reverse) mesh.triangles = trianglesBackwards[shape];
        else mesh.triangles = trianglesForwards[shape];
        ttriangle += sw.ElapsedMilliseconds;
        sw.Restart();

        if (usingCustomUV) mesh.uv = uvs;
        else mesh.uv = defaultUVs[shape];
        tuv += sw.ElapsedMilliseconds;
        sw.Restart();

        mesh.RecalculateNormals();
        tnormal += sw.ElapsedMilliseconds;
        sw.Restart();

        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        
        go.GetComponent<MeshRenderer>().material = mat;
        go.GetComponent<MeshFilter>().mesh = mesh;

        this.uvs = null;
        this.verts = new Vector3[0];
        tinstantiate += sw.ElapsedMilliseconds;
        sw.Restart();

        return go;
    }

    public void addCollider() {
        MeshCollider mc = go.AddComponent<MeshCollider>();
        //mc.convex = true;
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
