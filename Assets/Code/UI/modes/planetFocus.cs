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
    public static position movementOffset;

    public static void enable(bool use) {
        usePlanetFocus = use;
        usePoleFocus = false;
        reset();
        if (use) {
            // only if we are focused on a planet
            if (master.requestReferenceFrame() is planet) {
                // we want planet to take up about 60%
                focus = (planet) master.requestReferenceFrame();
                master.scale = focus.radius / (0.6 * -general.defaultCameraPosition.z);
                master.requestPositionUpdate();
                general.camera.transform.LookAt(focus.representation.gameObject.transform.position);
            } else usePlanetFocus = false;
        }
    }

    public static void togglePoleFocus(bool use) {
        usePoleFocus = use;
        reset();

        if (use) {
            float newHeight = (float) (focus.radius / master.scale);
            master.scale = 50;
            master.currentPosition = focus.rotateLocalGeo(new geographic(-90, 0), 0);

            focus.representation.forceHide = true;
        } else {
            master.scale = focus.radius / (0.6 * -general.defaultCameraPosition.z);
            focus.representation.forceDisable = false;
        }
    }

    private static void reset() {
        general.camera.transform.position = general.defaultCameraPosition;
        general.camera.fieldOfView = general.defaultCameraFOV;
        zoom = general.defaultCameraFOV;
        general.camera.transform.rotation = Quaternion.Euler(0, 0, 0);
        master.currentPosition = new position(0, 0, 0);
        master.scale = 1000;
        rotation = Vector3.zero;
        movementOffset = new position(0, 0, 0);
    }

    public static void update() {
        if (usePoleFocus) {
            master.currentPosition = planetFocus.focus.rotateLocalGeo(new geographic(-90, 0), 0) + movementOffset;
        } else master.currentPosition = movementOffset;

        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(Vector3.zero, general.camera.transform.up, rotation.y);

        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        general.camera.fieldOfView = zoom;
    }
}
