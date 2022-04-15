using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class planetFocus
{
    public static bool usePlanetFocus {get => _upf;}
    private static bool _upf = false;
    public static Vector3 rotation;
    public static float zoom = general.defaultCameraFOV;

    private static planet focus;


    public static void enable(bool use) {
        _upf = use;
        if (use) {
            // only if we are focused on a planet
            if (master.requestReferenceFrame() is planet) {
                focus = (planet) master.requestReferenceFrame();
                master.currentPosition = new position(0, 0, 0);
                master.requestPositionUpdate();
                general.camera.transform.LookAt(focus.representation.transform.position);

            } else _upf = false;
        } else {
            general.camera.transform.position = general.defaultCameraPosition;
            general.camera.fieldOfView = general.defaultCameraFOV;
            general.camera.transform.rotation = Quaternion.Euler(0, 0, 0);
            rotation = Vector3.zero;
        }
    }

    public static void update() {
        general.camera.transform.RotateAround(focus.representation.transform.position, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(focus.representation.transform.position, general.camera.transform.up, rotation.y);
        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        general.camera.fieldOfView = zoom;
    }
}
