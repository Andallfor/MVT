using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class planetRepresentation : IJsonFile<jsonPlanetRepresentationStruct>
{
    private float _r;
    private TextMeshProUGUI shownName;
    private planetType pType;
    private GameObject canvas, planetParent;
    public GameObject gameObject;
    public MeshRenderer mrSelf;
    public Collider hitbox;
    private string shownNameText, name;
    private double radius;

    private representationData data;
    public bool forceHide = false, forceDisable = false;

    public planetRepresentation(string name, double radius, planetType pType, representationData data) {
        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        gameObject.name = name;

        this.shownNameText = name;
        this.radius = radius;
        this.setRadius(radius);
        this.data = data;
        this.pType = pType;
        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        this.mrSelf = gameObject.GetComponent<MeshRenderer>();
        this.hitbox = gameObject.GetComponent<Collider>();
        this.name = name;

        this.shownName = resLoader.createPrefab("bodyName").GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("ui/bodyName").transform, false);
        shownName.fontSize = 25;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold | FontStyles.Italic;

        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
    }

    public void regenerate() {
        if (gameObject != null) GameObject.Destroy(gameObject);
        if (shownName != null) GameObject.Destroy(shownName.gameObject);

        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        gameObject.name = name;

        this.shownNameText = name;
        this.setRadius(radius);
        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        this.mrSelf = gameObject.GetComponent<MeshRenderer>();
        this.hitbox = gameObject.GetComponent<Collider>();

        this.shownName = resLoader.createPrefab("bodyName").GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("ui/bodyName").transform, false);
        shownName.fontSize = 25;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold | FontStyles.Italic;

        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
    }

    public jsonPlanetRepresentationStruct requestJsonFile()
    {
        return new jsonPlanetRepresentationStruct() {
            modelPath = this.data.modelPath,
            materialPath = this.data.materialPath};
    }

    // updating shown values
    public void setPosition(position pos)
    {
        if (uiMap.useUiMap) return;

        bool endDisable = false;
        if (planetOverview.instance.active) {
            if (!planetOverview.instance.obeyingPlanets.Exists(x => x.name == name)) {
                shownName.text = "";
                mrSelf.enabled = false;
                return;
            }

            if (name == planetOverview.instance.focus.name) pos = Vector3.zero;
            else pos = planetOverview.instance.planetOverviewPosition(pos - planetOverview.instance.focus.pos + master.currentPosition + master.referenceFrame);
        }

        // scale position
        Vector3 p = new Vector3(
            (float) (pos.x / master.scale),
            (float) (pos.y / master.scale),
            (float) (pos.z / master.scale));

        if (Vector3.Distance(p, Vector3.zero) > 1000f) endDisable = false; // hide if too far away
        else {
            endDisable = true;
            gameObject.transform.localPosition = p;

            if (shownName.text == "") shownName.text = shownNameText;

            // scale far away planets so they can be seen better
            float distance = Vector3.Distance(Vector3.zero, gameObject.transform.position);
            float scale = 0.01f * distance;
            float r = Mathf.Max(Mathf.Min(gameObject.transform.localScale.x, _r), scale);
            gameObject.transform.localScale = new Vector3(r, r, r);
        }

        // position the name of the planet so that it is over the displayed position
        // rotate point since this is the localPosition point, and does not account for possible
        // rotations of its parent
        // if we are in planet overview, position text to the side
        if (planetOverview.instance.active) {
            shownName.alignment = TextAlignmentOptions.Left;
            shownName.rectTransform.pivot = new Vector2(-0.05f, 0.5f);
        } else {
            shownName.alignment = TextAlignmentOptions.Center;
            shownName.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        Vector3 rot = planetParent.transform.rotation.eulerAngles * Mathf.Deg2Rad;
        Vector3 rotatedPoint = uiHelper.vRotate(rot.y, rot.x, rot.z, p);
        uiHelper.drawTextOverObject(shownName, rotatedPoint);

        if (forceDisable && gameObject.activeSelf) gameObject.SetActive(false);
        else if (!forceDisable && gameObject.activeSelf != endDisable) gameObject.SetActive(endDisable);
        if (forceHide && mrSelf.enabled) mrSelf.enabled = false;
        else if (!forceHide && !mrSelf.enabled) mrSelf.enabled = true;
    }
    public Vector3 rotate(position p)
    {
        gameObject.transform.localEulerAngles = new Vector3((float) p.y, (float) p.x, 0);
        return new Vector3(gameObject.transform.localEulerAngles.x % 360,
                           gameObject.transform.localEulerAngles.y % 360,
                           gameObject.transform.localEulerAngles.z % 360);
    }
    public void setRadius(double radius)
    {
        _r = ((float) Math.Max((radius * 2) / master.scale, 0.00001));
        gameObject.transform.localScale = new Vector3(_r, _r, _r);
    }
}
