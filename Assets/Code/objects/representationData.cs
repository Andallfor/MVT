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
        if (resLoader.containsName(model)) {
            this.modelPath = resLoader.getPath(model);
            this.model = resLoader.load<GameObject>(model);
        } else {
            this.modelPath = model;
            this.model = Resources.Load(model) as GameObject;
        }

        if (resLoader.containsName(material)) {
            this.materialPath = resLoader.getPath(material);
            this.material = resLoader.load<Material>(material);
        }
        else {
            this.materialPath = material;
            this.material = Resources.Load(material) as Material;
        }
    }
}
