using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class facilityRepresentation : MonoBehaviour
{
    private GameObject parent;
    private representationData data;
    private geographic geo;
    private TextMeshProUGUI shownName;
    private LineRenderer lr;
    public MeshRenderer mr {get; private set;}
    private string shownNameText;
    private float r;
    public bool selected {get; private set;}
    public geographic offset {get; private set;} // because we will at times get facilites with the same/very similar positions,
                                                 // it makes it hard for the ux to be able to tell which one the user wants to point to
                                                 // in planetFocus. so add a slight offset to each representation to alievate this issue

    private bool planetFocusHidden;

    public void init(string name, geographic geo, GameObject parent, representationData data)
    {
        this.gameObject.name = name;
        this.shownNameText = name;
        this.parent = parent;
        this.geo = geo;
        this.data = data;

        r = 25f / (float) master.scale;
        this.gameObject.transform.localScale = new Vector3(r, r, r);

        GameObject canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        lr = this.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        mr = this.GetComponent<MeshRenderer>();

        System.Random ran = new System.Random(UnityEngine.Random.Range(0, 100000));
        offset = new geographic(
            (ran.NextDouble() - 0.5) / 10.0,
            (ran.NextDouble() - 0.5) / 10.0);
        
        this.geo += offset;

        this.shownName = Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(canvas.transform, false);
        shownName.fontSize = 25;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
    }

    public static facilityRepresentation createFacility(string name, geographic geo, GameObject parent, representationData data)
    {
        GameObject go = Instantiate(data.model);
        go.GetComponent<MeshRenderer>().material = data.material;
        go.transform.parent = parent.transform;
        facilityRepresentation fr = go.GetComponent<facilityRepresentation>();
        fr.init(name, geo, parent, data);
        return fr;
    }

    public void updatePos(planet parent, double radius)
    {
        if (planetOverview.usePlanetOverview)
        {
            gameObject.SetActive(false);
            shownName.text = "";
            return;
        }

        position p = geo.toCartesian(radius) / (2 * radius);
        this.gameObject.transform.localPosition = (Vector3) (p.swapAxis());

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

    public void drawSchedulingConnections(List<scheduling> ss)
    {
        // can optimize this, instead of looping through maybe use events
        Vector3[] linePositions = new Vector3[2];
        foreach (scheduling s in ss)
        {
            // check each satellite this station will connect to
            foreach (schedulingTime st in s.times)
            {
                // check if that satellite should be currently connected
                if (st.between(master.time.julian))
                {
                    linePositions[0] = (this.gameObject.transform.position);
                    linePositions[1] = (s.connectingTo.representation.gameObject.transform.position);
                    break;
                }
            }
        }

        lr.SetPositions(linePositions);
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
