using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class facilityRepresentation : IJsonFile<jsonFacilityRepresentationStruct>
{
    private planet parent;
    private representationData data;
    private geographic geo;
    private TextMeshProUGUI shownName;
    private LineRenderer lr;
    private List<antennaRepresentation> antennas;
    public MeshRenderer mr {get; private set;}
    public GameObject gameObject;
    private string shownNameText, name;
    private float r;
    public bool selected {get; private set;}
    private bool planetFocusHidden;

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
        this.shownNameText = name;
        this.parent = parent;
        this.geo = geo;
        this.name = name;
        this.data = data;
        this.antennas = new List<antennaRepresentation>();

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

        this.shownName = GameObject.Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(canvas.transform, false);
        shownName.fontSize = 25;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
    }

    public void regenerate() {
        if (gameObject != null) GameObject.Destroy(gameObject);
        if (shownName != null) GameObject.Destroy(shownName.gameObject);

        gameObject = GameObject.Instantiate(data.model);
        gameObject.GetComponent<MeshRenderer>().material = data.material;
        gameObject.transform.parent = parent.representation.gameObject.transform;
        gameObject.transform.localScale = new Vector3(r, r, r);

        this.gameObject.name = name;
        this.shownNameText = name;

        foreach (antennaRepresentation ad in antennas) ad.regenerate(this.gameObject);

        GameObject canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        lr = gameObject.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        mr = gameObject.GetComponent<MeshRenderer>();

        this.shownName = GameObject.Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(canvas.transform, false);
        shownName.fontSize = 25;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
    }

    public void updatePos(planet parent, bool forceHide = false) {
        if (forceHide) {
            gameObject.SetActive(false);
            shownName.text = "";
            return;
        }

        if (planetOverview.usePlanetOverview) {
            gameObject.SetActive(false);
            shownName.text = "";
            return;
        }

        position p = geo.toCartesian(parent.radius) / (2 * parent.radius);
        this.gameObject.transform.localPosition = (Vector3) (p.swapAxis());

        foreach (antennaRepresentation ar in antennas) ar.updatePos(parent);

        if (!planetFocusHidden) {
            RaycastHit hit;
            if (Physics.Raycast(general.camera.transform.position,
                this.gameObject.transform.position - general.camera.transform.position, out hit, 
                Vector3.Distance(this.gameObject.transform.position, general.camera.transform.position), 1 << 6)) {
                shownName.text = "";
            } else {
                shownName.text = shownNameText;
                uiHelper.drawTextOverObject(shownName, this.gameObject.transform.position);
            }
        } else shownName.text = "";
    }

    public void drawSchedulingConnections(List<antennaData> ads) {
        foreach (antennaData ad in ads) {
            if (!ReferenceEquals(ad.schedules, null)) antennas
                .Where(x => x.name == ad.name)
                .ToList()
                .ForEach(x => x.drawSchedulingConnections(ad.schedules));
        }
    }

    public jsonFacilityRepresentationStruct requestJsonFile()
    {
        return new jsonFacilityRepresentationStruct() {
            modelPath = data.modelPath,
            materialPath = data.materialPath};
    }

    public void setActive(bool b)
    {
        this.gameObject.SetActive(b);
        this.shownName.gameObject.SetActive(b);
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
}
