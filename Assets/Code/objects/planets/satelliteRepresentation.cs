using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using TMPro;

public class satelliteRepresentation {
    private objectName uiName;
    private GameObject canvas, planetParent;
    public planet parent;
    private representationData data;
    public static readonly float minScale = 0.05f;
    private float _r = minScale;
    private MeshRenderer mrSelf;
    private string name;
    private trailRenderer tr;
    public GameObject gameObject;

    public satelliteRepresentation(string name, representationData data) {
        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = GameObject.FindGameObjectWithTag("planet/parent").transform;
        gameObject.name = name;
        gameObject.GetComponent<SphereCollider>().enabled = false;

        this.name = name;
        this.data = data;
        this.canvas = GameObject.FindGameObjectWithTag("ui/canvas");

        uiName = new objectName(gameObject, objectNameType.satellite, name);

        mrSelf = gameObject.GetComponent<MeshRenderer>();

        planetParent = GameObject.FindGameObjectWithTag("planet/parent");
    }

    public void setRelationshipParent() {parent = master.relationshipSatellite.First(x => x.Value.Exists(y => y.name == name)).Key;}

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
        if (gameObject.transform.lossyScale.x < 0.01f) return true;

        // check screen size
        float f = uiHelper.screenSize(mrSelf, gameObject.transform.position);
        if (f < 1) return true;

        return false;
    }

    private void hide() {
        if (mrSelf.enabled) mrSelf.enabled = false;

        // there may be an animation going on, only hide if the uiName is fully shown
        if (!uiName.isHidden) uiName.hide();
    }

    private void show() {
        if (!mrSelf.enabled) mrSelf.enabled = true;
        uiName.show();
    }

    public void setRadius(double radius)
    {
        _r = ((float) Math.Max((radius * 2) / master.scale, 0.03));
        gameObject.transform.localScale = new Vector3(_r, _r, _r);
    }
}
