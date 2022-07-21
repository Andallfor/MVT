using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class planetFocus
{
    public static bool usePlanetFocus {get; private set;}
    public static Vector3 rotation;
    public static float zoom = general.defaultCameraFOV;
    public static bool usePoleFocus {get; private set;} = false;

    public static planet focus {get; private set;}
    
    private static Vector3 offset = new Vector3();

    public static void enable(bool use) {
        usePlanetFocus = use;
        usePoleFocus = false;
        if (use) {
            // only if we are focused on a planet
            if (master.requestReferenceFrame() is planet) {
                focus = (planet) master.requestReferenceFrame();
                master.currentPosition = new position(0, 0, 0);
                master.requestPositionUpdate();
                general.camera.transform.LookAt(focus.representation.gameObject.transform.position);
            } else usePlanetFocus = false;
        } else reset();
    }

    public static void togglePoleFocus(bool use) {
        usePoleFocus = use;
        reset();

        if (use) {
            float newHeight = (float) (focus.radius / master.scale);
            general.camera.transform.position += new Vector3(0, newHeight, 7);
            offset = new Vector3(0, newHeight, 0);

            zoom = 10;

            focus.representation.forceHide = true;
        } else focus.representation.forceDisable = false;
    }

    private static void reset() {
        general.camera.transform.position = general.defaultCameraPosition;
        general.camera.fieldOfView = general.defaultCameraFOV;
        general.camera.transform.rotation = Quaternion.Euler(0, 0, 0);
        rotation = Vector3.zero;
        offset = new Vector3();
    }

    public static void update() {
        Vector3 ogPos = general.camera.transform.position;
        general.camera.transform.RotateAround(focus.representation.gameObject.transform.position + offset, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(focus.representation.gameObject.transform.position + offset, general.camera.transform.up, rotation.y);

        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        general.camera.fieldOfView = zoom;
    }
}
