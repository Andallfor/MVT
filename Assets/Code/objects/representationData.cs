using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class representationData
{
    public GameObject model;
    public Material material;

    public string modelPath, materialPath;

    public representationData(string model, string material)
    {
        this.model = Resources.Load(model) as GameObject;
        this.material = Resources.Load(material) as Material;

        this.modelPath = model;
        this.materialPath = material;
    }
}
