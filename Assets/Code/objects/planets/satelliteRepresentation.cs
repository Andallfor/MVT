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

    public void setPosition(position pos, bool forceHide = false)
    {
        if (uiMap.instance.active) return;

        if (forceHide) {hide(); return;}

        if (planetOverview.instance.active) {
            if (!planetOverview.instance.obeyingSatellites.Exists(x => x.name == name)) {hide(); return;}
            pos = planetOverview.instance.planetOverviewPosition(pos - planetOverview.instance.focus.pos + master.currentPosition + master.referenceFrame);
        }

        Vector3 p = new Vector3(
            (float) (pos.x / master.scale),
            (float) (pos.y / master.scale),
            (float) (pos.z / master.scale));

        if (Vector3.Distance(p, Vector3.zero) > 10000f) mrSelf.enabled = false;
        else
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (!mrSelf.enabled) mrSelf.enabled = true;
            gameObject.transform.localPosition = p;

            float distance = Vector3.Distance(Vector3.zero, this.gameObject.transform.position);
            float scale = 0.01f * distance + 0;
            float r = Mathf.Max(Mathf.Min(this.gameObject.transform.localScale.x, planetOverview.instance.active ? _r : minScale), scale);
            gameObject.transform.localScale = new Vector3(r, r, r);

            if (!(parent is null)) gameObject.transform.LookAt(parent.representation.gameObject.transform.position);
        }

        uiName.tryDraw();
    }

    private void hide() {
        mrSelf.enabled = false;
        uiName.hide();
    }

    public void setRadius(double radius)
    {
        _r = ((float) Math.Max((radius * 2) / master.scale, 0.00001));
        gameObject.transform.localScale = new Vector3(_r, _r, _r);
    }
}
