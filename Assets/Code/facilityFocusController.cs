using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class facilityFocusController : MonoBehaviour
{
    private double speed = 0.00005;
    public void init() {
        // load the representations for each object
        foreach (planet p in master.allPlanets) facilityFocus.representations[p.name] = tryAddingBody(p);
        foreach (satellite s in master.allSatellites) facilityFocus.representations[s.name] = tryAddingBody(s);
        foreach (facility f in master.allFacilites) {
            foreach (antennaData ad in f.data.antennas) {
                if (ad.geo.distAs2DVector(facilityFocus.sw) > 2) continue;
                facilityFocus.representations[ad.name] = new facilityFocusRepresentation(
                    ad.name, dtedReader.centerDtedPoint(
                        facilityFocus.sw, ad.geo, facilityFocus.pointCenteringOffset, ad.alt
                    ));
            }
        }

        StartCoroutine(general.internalClock(7200, int.MaxValue, (tick) => {
            if (master.pause)  {
                master.tickStart(master.time);
                master.time.addJulianTime(0, true);
            } else {
                Time t = new Time(master.time.julian);
                t.addJulianTime(speed);
                master.tickStart(t);
                master.time.addJulianTime(speed, true);
            }

            onFacilityFocusTimeChange(null, EventArgs.Empty);

            master.currentTick = tick;
        }, null));
    }
    public void Update() {
        if (Input.GetKeyDown("f")) {
            facilityFocus.enable(false, "");
            SceneManager.LoadScene("main", LoadSceneMode.Single);
        }
    }

    private facilityFocusRepresentation tryAddingBody(body b) {
        if (!(b.parent is null)) return new facilityFocusRepresentation(b.name, b.positions, b.parent.positions);
        else return new facilityFocusRepresentation(b.name, b.positions);
    }

    public static event EventHandler onFacilityFocusTimeChange = delegate {};
}

public class facilityFocusRepresentation {
    private Timeline pos, parent;
    private GameObject representation;
    private MeshRenderer mr;
    // clean code is for fools
    private bool timelineExists = false, parentExists = false;
    private position failsafePosition;
    private TextMeshProUGUI shownName;

    public string name {get; private set;}

    public facilityFocusRepresentation(string name, Timeline pos, Timeline parent) {
        timelineExists = true;
        parentExists = true;
        this.name = name;
        this.pos = pos;
        this.parent = parent;

        init();
    }
    
    public facilityFocusRepresentation(string name, Timeline pos) {
        timelineExists = true;
        this.name = name;
        this.pos = pos;

        init();
    }

    public facilityFocusRepresentation(string name, position pos) {
        this.name = name;
        this.failsafePosition = pos;

        init();
    }

    private void init() {
        representation = GameObject.Instantiate(Resources.Load("Prefabs/default") as GameObject);
        representation.name = name;

        mr = representation.GetComponent<MeshRenderer>();

        GameObject canvas = GameObject.FindGameObjectWithTag("ui/canvas");

        this.shownName = GameObject.Instantiate(Resources.Load("Prefabs/bodyName") as GameObject).GetComponent<TextMeshProUGUI>();
        shownName.gameObject.transform.SetParent(canvas.transform, false);
        shownName.fontSize = 25;
        shownName.text = name;
        shownName.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;

        updatePosition(null, EventArgs.Empty);

        facilityFocusController.onFacilityFocusTimeChange += updatePosition;        
    }

    private void updatePosition(object sender, EventArgs args) {
        position desiredPosition = new position(0, 0, 0);
        if (parentExists || timelineExists) {
            desiredPosition = pos.find(master.time);
            if (parentExists) desiredPosition += parent.find(master.time);

            // TODO: VERIFY IF THIS IS CORRECT
            // dont rotate if its facility since we rotate it when we input the failsafe position
            desiredPosition = (desiredPosition
                .rotate(0, 0, (-facilityFocus.sw.lon - 0.5) * (Mathf.PI / 180.0))
                .rotate((270.0 + facilityFocus.sw.lat) * (Math.PI / 180.0), 0, 0)
                - facilityFocus.pointCenteringOffset)
                .swapAxis();
        } else desiredPosition = failsafePosition;

        if (position.distance(desiredPosition, general.camera.transform.localPosition) > 1000) mr.enabled = false;
        else mr.enabled = true;

        representation.transform.localPosition = (Vector3) desiredPosition;
        uiHelper.drawTextOverObject(shownName, (Vector3) desiredPosition);
    }
}