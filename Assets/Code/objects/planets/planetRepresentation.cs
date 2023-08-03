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

    public representationData data;
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
        this.uiName = new objectName(gameObject, pType == planetType.planet ? objectNameType.planet : objectNameType.moon, name);

        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
    }
  
    public void setPosition(position pos)
    {
        if (uiMap.instance.active) return;

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
        
        gameObject.transform.localPosition = p;

        uiName.tryDraw();

        if (uiName.isHidden || uiMap.instance.active || forceHide || isTooSmall()) hide();
        else show();

        if (forceDisable) disable();
        else enable();

        if (forceHide) hide();
        else show();
    }

    private bool isTooSmall() {
        if (gameObject.transform.lossyScale.x < 0.01f) return true;

        // check screen size
        float f = uiHelper.screenSize(mrSelf, gameObject.transform.position);
        if (f < 1) return true;

        return false;
    }
    
    private void hide() {
        if (mrSelf.enabled) mrSelf.enabled = false;
        //if (!uiName.isHidden) uiName.hide();
    }

    private void show() {
        if (!mrSelf.enabled) mrSelf.enabled = true;
        //if (uiName.isHidden) uiName.show();
    }

    private void disable() {
        if (gameObject.activeSelf) gameObject.SetActive(false);
        //if (!uiName.isHidden) uiName.hide();
    }

    private void enable() {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        //if (uiName.isHidden) uiName.show();
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
