using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class poleTerrain {
    private List<poleTerrainFile> meshes = new List<poleTerrainFile>();
    private Transform parent;
    private GameObject model;
    private Material mat;

    public poleTerrain(Transform parent) {
        this.parent = parent;
        model = Resources.Load("Prefabs/PlanetMesh") as GameObject;
        mat = Resources.Load("Materials/planets/moon/lunarSouthPole") as Material;
    }

    public void registerMesh(Mesh m) {
        meshes.Add(new poleTerrainFile(m.name, m, parent, model, mat));
    }

    public void generate() {
        foreach (poleTerrainFile ptf in meshes) ptf.show();
    }

    public void clear() {
        foreach (poleTerrainFile ptf in meshes) ptf.hide();
    }
}

public class poleTerrainFile {
    private GameObject go;
    private MeshRenderer mr;

    public poleTerrainFile(string name, Mesh m, Transform parent, GameObject model, Material mat) {
        go = GameObject.Instantiate(model);
        go.name = name;
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        go.GetComponent<MeshRenderer>().material = mat;
        go.GetComponent<MeshFilter>().sharedMesh = m;
        mr = go.GetComponent<MeshRenderer>();
    }

    public void show() {
        mr.enabled = true;
    }

    public void hide() {
        mr.enabled = false;
    }
}
