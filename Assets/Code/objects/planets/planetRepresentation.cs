using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class planetRepresentation
{
    private float _r;
    private planetType pType;
    private GameObject canvas, planetParent;
    public GameObject gameObject;
    public MeshRenderer mrSelf;
    public SphereCollider hitbox;
    private objectName uiName;
    private string name;
    private double radius;

    private representationData data;
    public bool forceHide = false, forceDisable = false;

    public planetRepresentation(string name, double radius, planetType pType, representationData data) {
        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        gameObject.name = name;

        this.radius = radius;
        this.setRadius(radius);
        this.data = data;
        this.pType = pType;
        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        this.mrSelf = gameObject.GetComponent<MeshRenderer>();
        this.hitbox = gameObject.GetComponent<SphereCollider>();
        this.hitbox.radius = .497f;
        this.name = name;
        this.uiName = new objectName(gameObject, objectNameType.planet, name);

        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
    }
  
    // updating shown values
    public void setPosition(position pos)
    {
        if (uiMap.instance.active) return;

        bool endDisable = false;
        if (planetOverview.instance.active) {
            if (!planetOverview.instance.obeyingPlanets.Exists(x => x.name == name)) {
                uiName.hide();
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

        if (Vector3.Distance(p, Vector3.zero) - radius / master.scale > 1000f) endDisable = false; // hide if too far away
        else {
            endDisable = true;
            gameObject.transform.localPosition = p;

            if (master.requestReferenceFrame().name != name || planetOverview.instance.active) uiName.show();
            else uiName.hide();
        }

        uiName.tryDraw();

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
        _r = (float) ((radius * 2) / master.scale);
        gameObject.transform.localScale = new Vector3(_r, _r, _r);
    }
}
