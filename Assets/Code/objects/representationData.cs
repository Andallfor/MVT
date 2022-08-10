using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class representationData
{
    public GameObject model;
    public Material material;
    public Vector3 rotate;

    public string modelPath, materialPath;

    public representationData(string model, string material)
    {
        this.model = Resources.Load(model) as GameObject;
        this.material = Resources.Load(material) as Material;

        this.modelPath = model;
        this.materialPath = material;
        Vector3 rotate = new Vector3(0,0,0);
    }

    public representationData(string model, string material, Vector3 rotate)
    {
        this.model = Resources.Load(model) as GameObject;
        this.material = Resources.Load(material) as Material;

        this.modelPath = model;
        this.materialPath = material;
        this.rotate = rotate;
    }
}
