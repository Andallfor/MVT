using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public sealed class facilityFocus : IMode {
    protected override IModeParameters modePara => new facilityFocusParameters();
    private facility focus;
    private bool querySuccess = false;

    public Vector3 rotation;
    public float zoom = 60;

    public void query() {
        if (!(master.requestReferenceFrame() is planet)) {
            querySuccess = false;
            return;
        }

        RaycastHit hit;
        Transform ct = general.camera.gameObject.transform;
        if (Physics.Raycast(ct.position, ct.forward, out hit, 10)) {
            planet p = (planet) master.requestReferenceFrame();
            Vector3 v = hit.point - p.representation.gameObject.transform.position;
            focus = new facility("↓", p, new facilityData("↓", p.localPosToLocalGeo(v), 0,
                new List<antennaData>()),
                new representationData("facility", "defaultMat"));

            focus.representation.setNameFont(Resources.Load<TMP_FontAsset>("Fonts/inter/Inter-Light SDF"));
        } else {
            querySuccess = false;
            return;
        }

        querySuccess = true;
        return;
    }

    protected override bool enable() {
        // expects query to happen before actually changing into scene
        if (!querySuccess) return false;

        master.requestScaleUpdate();
        
        master.currentPosition = focus.parent.rotateLocalGeo(focus.geo, focus.parent.radius * 0.05);
        master.requestPositionUpdate();
        general.camera.transform.LookAt(focus.representation.gameObject.transform);

        querySuccess = false;
        return true;
    }

    protected override bool disable() {
        master.removeFacility(focus);
        focus = null;
        zoom = 60;

        querySuccess = false;
        return true;
    }

    protected override void _initialize() {}

    public void update() {
        master.currentPosition = focus.parent.rotateLocalGeo(focus.geo, 0);

        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.up, rotation.y);

        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        general.camera.fieldOfView = zoom;
    }

    private facilityFocus() {}
    private static readonly Lazy<facilityFocus> lazy = new Lazy<facilityFocus>(() => new facilityFocus());
    public static facilityFocus instance => lazy.Value;
}
