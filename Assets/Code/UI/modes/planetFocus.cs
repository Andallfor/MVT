using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public sealed class planetFocus : IMode {
    public Vector3 rotation;
    public float zoom = general.defaultCameraFOV;
    public bool usePoleFocus {get; private set;} = false;

    public planet focus {get; private set;}
    public position movementOffset;

    protected override IModeParameters modePara => new planetFocusParameters();

    protected override void enable() {
        reset();
        if (master.requestReferenceFrame() is planet) {
            // we want planet to take up about 60%
            focus = (planet) master.requestReferenceFrame();
            master.scale = focus.radius / (0.6 * -general.defaultCameraPosition.z);
            master.requestPositionUpdate();
            general.camera.transform.LookAt(focus.representation.gameObject.transform.position);
        } else active = false;
    }

    protected override void disable() {
        general.pt.unload();
        general.plt.clear();
    }

    protected override void callback() {
        usePoleFocus = false;
    }

    public void togglePoleFocus(bool use) {
        if (focus.name != "Luna") {
            usePoleFocus = false;
            return;
        }
        usePoleFocus = use;
        reset();

        if (use) {
            master.scale = 50;
            master.currentPosition = focus.rotateLocalGeo(new geographic(-90, 0), 0);

            focus.representation.forceHide = true;
        } else {
            master.scale = focus.radius / (0.6 * -general.defaultCameraPosition.z);
            focus.representation.forceHide = false;
        }
    }

    public void reset() {
        modeParameters.load(new planetFocusParameters());
        zoom = general.defaultCameraFOV;
        rotation = Vector3.zero;
        movementOffset = new position(0, 0, 0);
    }

    public void update() {
        if (usePoleFocus) master.currentPosition = focus.rotateLocalGeo(new geographic(-90, 0), 0) + movementOffset;
        else master.currentPosition = movementOffset;

        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.up, rotation.y);

        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        general.camera.fieldOfView = zoom;
    }

    private planetFocus() {}
    private static readonly Lazy<planetFocus> lazy = new Lazy<planetFocus>(() => new planetFocus());
    public static planetFocus instance => lazy.Value;
}
