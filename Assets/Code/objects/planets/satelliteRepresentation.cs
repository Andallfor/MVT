using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using TMPro;

public class satelliteRepresentation {
    private objectName uiName;
    private GameObject canvas, planetParent;
    public satellite parent;
    private representationData data;
    public static readonly float minScale = 0.05f;
    private float _r = minScale;
    private MeshRenderer mrSelf;
    private string name;
    private trailRenderer tr;
    public GameObject gameObject;

    public satelliteRepresentation(string name, representationData data, satellite parent) {
        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        gameObject.name = name;
        gameObject.GetComponent<SphereCollider>().enabled = false;

        this.name = name;
        this.data = data;
        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        this.parent = parent;

        uiName = new objectName(gameObject, objectNameType.satellite, name);

        mrSelf = gameObject.GetComponent<MeshRenderer>();

        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
    }

    public void setPosition(position pos, bool forceHide = false) {
        if (planetOverview.instance.active) {
            if (!planetOverview.instance.obeyingSatellites.Exists(x => x.name == name)) {hide(); return;}
            pos = planetOverview.instance.planetOverviewPosition(pos - planetOverview.instance.focus.pos + master.currentPosition + master.referenceFrame);
        }

        Vector3 p = new Vector3(
            (float) (pos.x / master.scale),
            (float) (pos.y / master.scale),
            (float) (pos.z / master.scale));
        
        gameObject.transform.localPosition = p;

        uiName.tryDraw();

        if (uiName.isHidden || uiMap.instance.active || forceHide || isTooSmall()) hide();
        else show();
    }

    private bool isTooSmall() {
        float parentScale = 1;
        if (parent.parent != default(planet)) parentScale = parent.parent.representation.gameObject.transform.localScale.x;
        if (gameObject.transform.localScale.x * parentScale < 0.0001f) return true;

        // check screen size
        float f = uiHelper.screenSize(mrSelf, gameObject.transform.position);
        if (f < 1) return true;

        return false;
    }

    private void hide() {
        if (gameObject.activeSelf) gameObject.SetActive(false);

        // there may be an animation going on, only hide if the uiName is fully shown
        if (!uiName.isHidden) uiName.hide();
    }

    private void show() {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        uiName.show();
    }

    public void setRadius(double radius)
    {
        _r = ((float) Math.Max((radius * 2) / master.scale, 0.03));
        gameObject.transform.localScale = new Vector3(_r, _r, _r);
    }
}
