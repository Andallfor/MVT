using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class facilityRepresentation
{
    private planet parent;
    private representationData data;
    private geographic geo;
    private LineRenderer lr;
    private List<antennaRepresentation> antennas;
    private objectName uiName;
    public MeshRenderer mr {get; private set;}
    public GameObject gameObject;
    private string name;
    private float r;
    public bool selected {get; private set;}
    public bool planetFocusHidden {get; private set;}

    public void initDebugger() {
        facilityDebugger debugger = gameObject.GetComponent<facilityDebugger>();
        debugger.parent = this;
        debugger.lat = (float) geo.lat;
        debugger.lon = (float) geo.lon;
    }

    /// <summary> WARNING: May break things </summary>
    public void forceChangeGeo(geographic g) {
        this.geo = g;
    }

    public facilityRepresentation(string name, List<antennaData> antennas, geographic geo, planet parent, representationData data) {
        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = parent.representation.gameObject.transform;

        this.gameObject.name = name;
        this.parent = parent;
        this.geo = geo;
        this.name = name;
        this.data = data;
        this.antennas = new List<antennaRepresentation>();
        uiName = new objectName(gameObject, objectNameType.facility, name);

        initDebugger();

        foreach (antennaData ad in antennas) {
            antennaRepresentation ar = new antennaRepresentation(ad, this.gameObject);
            this.antennas.Add(ar);
        }

        r = 25f / (float) master.scale;
        this.gameObject.transform.localScale = new Vector3(r, r, r);

        GameObject canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        lr = gameObject.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        mr = gameObject.GetComponent<MeshRenderer>();
        mr.enabled = false;
    }

    public void addAntennaFromParent(antennaData ad) {
        antennaRepresentation ar = new antennaRepresentation(ad, this.gameObject);
        this.antennas.Add(ar);
    }

    public void updatePos(planet parent, double alt, bool forceHide = false) {
        if (uiMap.instance.active) return;

        if (forceHide || planetOverview.instance.active || position.distance(parent.pos, master.referenceFrame + master.currentPosition) > master.scale * 1000.0) {
            gameObject.SetActive(false);
            uiName.hide();
            foreach (antennaRepresentation ar in antennas) ar.hideName();
            return;
        }

        position p = geo.toCartesianWGS(alt) / (parent.radius * 2);
        this.gameObject.transform.localPosition = (Vector3) (p.swapAxis());

        foreach (antennaRepresentation ar in antennas) ar.updatePos(parent);

        if (antennas.Count == 0 || general.camera.fieldOfView > 15) {
            if (!planetFocusHidden) {
                RaycastHit hit;
                if (Physics.Raycast(general.camera.transform.position,
                    this.gameObject.transform.position - general.camera.transform.position, out hit,
                    Vector3.Distance(this.gameObject.transform.position, general.camera.transform.position), (1 << 6) | (1 << 7))) {
                    uiName.hide();
                } else {
                    uiName.tryDraw();
                    gameObject.SetActive(true);
                }
            } else {uiName.hide(); gameObject.SetActive(false);}
        } else {uiName.hide(); gameObject.SetActive(false);}


    }

    public void drawSchedulingConnections(List<antennaData> ads) {
        foreach (antennaData ad in ads) {
            if (!ReferenceEquals(ad.schedules, null)) antennas
                .Where(x => x.name == ad.name)
                .ToList()
                .ForEach(x => x.drawSchedulingConnections(ad.schedules));
        }
    }

    public void setActive(bool b)
    {
        this.gameObject.SetActive(b);
        if (b) uiName.show();
        else uiName.hide();
    }

    public void select(bool s, bool hide = false) {
        selected = s;
        if (s) {
            this.gameObject.transform.localScale = new Vector3(r * 1.25f, r * 1.25f, r * 1.25f);
            planetFocusHidden = false;
        } else {
            if (hide) {
                this.gameObject.transform.localScale = new Vector3(r * 0.75f, r * 0.75f, r * 0.75f);
                planetFocusHidden = true;
            }
            else {
                this.gameObject.transform.localScale = new Vector3(r, r, r);
                planetFocusHidden = false;
            }
        }
    }

    public void destroy() {
        uiName.destroy();
        GameObject.Destroy(gameObject);
    }
}
