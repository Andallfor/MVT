using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using TMPro;

public class satelliteRepresentation : IJsonFile<jsonSatelliteRepresentationStruct>
{
    private GameObject canvas;
    private TextMeshProUGUI shownName;
    private representationData data;
    public static readonly float minScale = 0.05f;
    private MeshRenderer mrSelf;
    private string shownNameText;
    private string name;
    public GameObject gameObject;

    public satelliteRepresentation(string name, representationData data) {
        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        gameObject.name = name;

        this.name = name;
        this.shownNameText = name;
        this.data = data;
        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");

        this.shownName = GameObject.Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(this.canvas.transform, false);
        shownName.fontSize = 20;
        shownName.text = name;

        mrSelf = gameObject.GetComponent<MeshRenderer>();
    }

    public void regenerate() {
        if (gameObject != null) GameObject.Destroy(gameObject);
        if (shownName != null) GameObject.Destroy(shownName.gameObject);

        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        gameObject.name = name;
        mrSelf = gameObject.GetComponent<MeshRenderer>();

        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");

        this.shownName = GameObject.Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(this.canvas.transform, false);
        shownName.fontSize = 20;
        shownName.text = name;
    }

    public void setPosition(position pos, bool forceHide = false)
    {
        if (forceHide) {
            mrSelf.enabled = false;
            shownName.text = "";
            return;
        }

        if (planetOverview.usePlanetOverview) {
            mrSelf.enabled = false;
            shownName.text = "";
            return;
        }

        Vector3 p = new Vector3(
            (float) (pos.x / master.scale),
            (float) (pos.z / master.scale),
            (float) (pos.y / master.scale));

        if (Vector3.Distance(p, Vector3.zero) > 1000f) mrSelf.enabled = false;
        else
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            gameObject.transform.localPosition = p;

            float distance = Vector3.Distance(Vector3.zero, this.gameObject.transform.position);
            float scale = 0.01f * distance + 0;
            float r = Mathf.Max(Mathf.Min(this.gameObject.transform.localScale.x, minScale), scale);
            gameObject.transform.localScale = new Vector3(r, r, r);
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, p - Camera.main.transform.position, out hit, Vector3.Distance(p, Camera.main.transform.position), 1 << 6))
        {
            shownName.text = "";
        }
        else
        {
            shownName.text = shownNameText;
            uiHelper.drawTextOverObject(shownName, p);
        }
    }

    public jsonSatelliteRepresentationStruct requestJsonFile()
    {
        return new jsonSatelliteRepresentationStruct() {
            modelPath = data.modelPath,
            materialPath = data.materialPath};
    }
}
