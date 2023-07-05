using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class antennaRepresentation
{
    public antennaData data;
    public string name;
    private string shownNameText;
    private GameObject gameObject, parent;
    private LineRenderer lr;
    private MeshRenderer mr;
    private TextMeshProUGUI shownName;

    public antennaRepresentation(antennaData ad, GameObject parent) {
        gameObject = GameObject.Instantiate(Resources.Load("Prefabs/antenna") as GameObject);
        this.parent = parent;
        name = ad.name;
        data = ad;
        lr = gameObject.GetComponent<LineRenderer>();
        mr = gameObject.GetComponent<MeshRenderer>();
        mr.enabled = false;
        gameObject.transform.parent = parent.transform;
        gameObject.name = ad.name;

        this.shownName = GameObject.Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("ui/bodyName").transform, false);
        shownName.fontSize = 28;
        shownNameText = name;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
    }

    public void drawSchedulingConnections(List<scheduling> ss) {
        // can optimize this, instead of looping through maybe use events
        Vector3[] linePositions = new Vector3[2];
        lr.positionCount = 0;
        foreach (scheduling s in ss) {
            // check each satellite this station will connect to
            foreach (schedulingTime st in s.times) {
                // check if that satellite should be currently connected
                if (st.between(master.time.julian)) {
                    lr.positionCount = 2;
                    linePositions[0] = (gameObject.transform.position);
                    linePositions[1] = (s.connectingTo.representation.gameObject.transform.position);
                    break;
                }
            }
        }

        lr.SetPositions(linePositions);
    }

    public void updatePos(planet parent) {
        if (mr.enabled) mr.enabled = false;
        position p = data.geo.toCartesian(parent.radius) / (2 * parent.radius);
        gameObject.transform.localPosition = (Vector3) (p.swapAxis());

        if (!data.parent.representation.planetFocusHidden && general.camera.fieldOfView <= 15) {
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
}
