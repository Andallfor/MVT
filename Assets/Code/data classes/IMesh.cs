using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class IMesh
{
    protected List<int> triangles = new List<int>();
    protected List<Vector2> uvs = new List<Vector2>();
    protected Vector3[] verts;
    protected Mesh mesh;
    public Vector2Int shape {get; protected set;}
    protected GameObject go;

    public void init(int cols, int rows, position initalPosition, position mapSize, bool reverse = false, Func<Vector2Int, Vector2> customUV = null) {
        shape = new Vector2Int(cols, rows);
        this.verts = new Vector3[cols * rows];

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < cols; x++) {
                if (x != cols - 1 && y != rows - 1) {
                    if (reverse) {
                        triangles.Add(toIndex(x + 1, y + 1));
                        triangles.Add(toIndex(x, y + 1));
                        triangles.Add(toIndex(x, y));
                        
                        triangles.Add(toIndex(x + 1, y));
                        triangles.Add(toIndex(x + 1, y + 1));
                        triangles.Add(toIndex(x, y));
                    } else {
                        triangles.Add(toIndex(x, y));
                        triangles.Add(toIndex(x, y + 1));
                        triangles.Add(toIndex(x + 1, y + 1));
                        
                        triangles.Add(toIndex(x, y));
                        triangles.Add(toIndex(x + 1, y + 1));
                        triangles.Add(toIndex(x + 1, y));
                    }
                }

                if (customUV is null) {
                    uvs.Add(new Vector2(
                        (float) ((initalPosition.x + x) / mapSize.x),
                        (float) ((initalPosition.y + y) / mapSize.y)));
                } else {
                    uvs.Add(customUV(new Vector2Int(x + (int) initalPosition.x, y + (int) initalPosition.y)));
                }

                this.verts[toIndex(x, y)] = planetTerrainMesh.NODATA_vector;
            }
        }
    }

    public GameObject drawMesh(Material mat, GameObject model, string name, Transform parent) {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        
        go.GetComponent<MeshRenderer>().material = mat;
        go.GetComponent<MeshFilter>().mesh = mesh;

        this.triangles.Clear();
        this.uvs.Clear();
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
