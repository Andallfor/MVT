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
    private string shownNameText;

    public double lat, lon;

    public void init(string name, geographic geo, GameObject parent, representationData data)
    {
        this.gameObject.name = name;
        this.shownNameText = name;
        this.parent = parent;
        this.geo = geo;
        this.data = data;

        float r = 25f / (float) master.scale;
        this.gameObject.transform.localScale = new Vector3(r, r, r);

        GameObject canvas = GameObject.FindGameObjectWithTag("ui/canvas");
        lr = this.GetComponent<LineRenderer>();
        lr.positionCount = 2;

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

        lat = this.geo.lat;
        lon = this.geo.lon;
        position p = geo.toCartesian(radius) / (2 * radius);
        this.gameObject.transform.localPosition = (Vector3) (p.swapAxis());

        RaycastHit hit;
        if (Physics.Raycast(general.camera.transform.position, this.gameObject.transform.position - general.camera.transform.position, out hit, Vector3.Distance(this.gameObject.transform.position, general.camera.transform.position), 1 << 6))
        {
            shownName.text = "";
        }
        else
        {
            shownName.text = shownNameText;
            uiHelper.drawTextOverObject(shownName, this.gameObject.transform.position);
        }
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
}
