using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class antennaRepresentation
{
    public antennaData data;
    public string name;
    private GameObject representation;
    private LineRenderer lr;
    private MeshRenderer mr;

    public antennaRepresentation(antennaData ad, GameObject parent) {
        representation = GameObject.Instantiate(Resources.Load("Prefabs/antenna") as GameObject);
        name = ad.name;
        data = ad;
        lr = representation.GetComponent<LineRenderer>();
        mr = representation.GetComponent<MeshRenderer>();
        mr.enabled = false;
        representation.transform.parent = parent.transform;
        representation.name = ad.name;
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
                    linePositions[0] = (representation.transform.position);
                    linePositions[1] = (s.connectingTo.representation.gameObject.transform.position);
                    break;
                }
            }
        }

        lr.SetPositions(linePositions);
    }

    public void updatePos(planet parent) {
        if (facilityFocus.useFacilityFocus) {
            // TODO: THIS
        } else {
            if (mr.enabled) mr.enabled = false;
            position p = data.geo.toCartesian(parent.radius) / (2 * parent.radius);
            representation.transform.localPosition = (Vector3) (p.swapAxis());
        }
    }
}
