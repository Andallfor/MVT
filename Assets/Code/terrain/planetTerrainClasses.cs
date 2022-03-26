using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System.Linq;
using System;
using System.IO;

public class planetTerrainMesh
{
    public planetTerrainFile ptf;
    public planetTerrainFolderInfo ptfi;
    public planetTerrain pt;
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private Vector3[] verts;
    private Mesh mesh;
    private GameObject go;

    public planetTerrainMesh(planetTerrainFile ptf, planetTerrainFolderInfo ptfi, planetTerrain pt)
    {
        this.ptf = ptf;
        this.ptfi = ptfi;
        this.pt = pt;

        this.verts = new Vector3[(int) ((ptf.ncols + 2) * (ptf.nrows + 2))];

        for (int y = 0; y < (int) ptf.nrows + 2; y++)
        {
            for (int x = 0; x < (int) ptf.ncols + 2; x++)
            {
                if (x != (int) ptf.ncols + 1 && y != (int) ptf.nrows + 1)
                {
                    triangles.Add(toIndex(x, y));
                    triangles.Add(toIndex(x, y + 1));
                    triangles.Add(toIndex(x + 1, y + 1));
                    
                    triangles.Add(toIndex(x, y));
                    triangles.Add(toIndex(x + 1, y + 1));
                    triangles.Add(toIndex(x + 1, y));
                }

                uvs.Add(new Vector2(
                    (float) ((ptf.cartPosition.x + x) / (ptfi.ncols)),
                    (float) ((ptf.cartPosition.y + y) / (ptfi.nrows))));
                
                this.verts[toIndex(x, y)] = NODATA_vector;
            }
        }
    }

    public void drawMesh()
    {
        if (verts.All(v => v != NODATA_vector))
        {
            mesh = new Mesh();
            //mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();

            //mesh.RecalculateBounds();
            //mesh.RecalculateNormals();
            //mesh.Optimize();
            //mesh.UploadMeshData(true);

            go = GameObject.Instantiate(Resources.Load("Prefabs/PlanetMesh") as GameObject);
            go.name = ptf.name;
            //go.transform.position = Vector3.zero;
            go.transform.parent = pt.parent.representation.transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = Vector3.zero;
            
            go.GetComponent<MeshRenderer>().material = Resources.Load("Materials/planets/earth/earth") as Material;
            go.GetComponent<MeshFilter>().mesh = mesh;

            this.triangles.Clear();
            this.uvs.Clear();
            this.verts = new Vector3[0];
        }
        /*
        Mesh mesh = new Mesh();

        mesh.vertices = verts;
        mesh.uv = uvs.ToArray();
        mesh.SetIndices(Enumerable.Range(0, verts.Length).ToArray(), MeshTopology.Points, 0);

        GameObject go = GameObject.Instantiate(Resources.Load("Prefabs/PlanetMesh") as GameObject);
        go.transform.position = Vector3.zero;
        
        go.GetComponent<MeshRenderer>().material = Resources.Load("Materials/earthLatLonTest") as Material;
        go.GetComponent<MeshFilter>().mesh = mesh;*/
    }

    public void clearMesh()
    {
        this.mesh.Clear();
        this.mesh = new Mesh();
        GameObject.Destroy(go);
    }

    public void drawBoundaries(string path)
    {
        string[] fileData = File.ReadAllLines(path);
        string[] n = fileData[1].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();
        string[] e = fileData[2].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();
        string[] s = fileData[3].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();
        string[] w = fileData[4].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToArray();

        for (int x = 0; x < (int) ptf.ncols + 2; x++)
        {
            geographic np = ptf.cartToGeo(x, (int) ptf.nrows + 1);
            addPoint(x, (int) ptf.nrows + 1, np, double.Parse(n[x], System.Globalization.NumberStyles.Any));

            geographic sp = ptf.cartToGeo(x, 0);
            addPoint(x, 0, sp, double.Parse(s[x], System.Globalization.NumberStyles.Any));
        }

        for (int y = 0; y < (int) ptf.nrows + 2; y++)
        {
            geographic ep = ptf.cartToGeo((int) ptf.ncols + 1, y);
            addPoint((int) ptf.ncols + 1, y, ep, double.Parse(e[y], System.Globalization.NumberStyles.Any));

            geographic wp = ptf.cartToGeo(0, y);
            addPoint(0, y, wp, double.Parse(w[y], System.Globalization.NumberStyles.Any));
        }
    }

    public void addPoint(int x, int y, geographic g, double h)
    {
        this.verts[toIndex(x, y)] = (Vector3) (g.toCartesian(pt.radius + (h * pt.heightMulti) / 1000.0).swapAxis() / master.scale);
    }

    public int toIndex(int x, int y) => y * ((int) ptf.ncols + 2) + x;

    public override int GetHashCode() => ptf.GetHashCode();
    public static readonly Vector3 NODATA_vector = new Vector3(
        terrainProcessor.NODATA_value, terrainProcessor.NODATA_value, terrainProcessor.NODATA_value);
    //public static readonly Vector3 NODATA_vector = Vector3.zero;
}
