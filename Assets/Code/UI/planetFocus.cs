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

    public static planet focus {get; private set;}
    public static facility hoveringOver = null;

    public static void enable(bool use) {
        _upf = use;
        if (use) {
            // only if we are focused on a planet
            if (master.requestReferenceFrame() is planet) {
                focus = (planet) master.requestReferenceFrame();
                master.currentPosition = new position(0, 0, 0);
                master.requestPositionUpdate();
                general.camera.transform.LookAt(focus.representation.gameObject.transform.position);

            } else _upf = false;
        } else {
            general.camera.transform.position = general.defaultCameraPosition;
            general.camera.fieldOfView = general.defaultCameraFOV;
            general.camera.transform.rotation = Quaternion.Euler(0, 0, 0);
            rotation = Vector3.zero;
        }
    }

    public static void update() {
        Vector3 ogPos = general.camera.transform.position;
        general.camera.transform.RotateAround(focus.representation.gameObject.transform.position, general.camera.transform.right, rotation.x);
        general.camera.transform.RotateAround(focus.representation.gameObject.transform.position, general.camera.transform.up, rotation.y);

        general.camera.transform.rotation *= Quaternion.AngleAxis(rotation.z, Vector3.forward);

        general.camera.fieldOfView = zoom;

        // can this be optimized? yes
        // will i do it? not now at least
        List<facility> validTargets = new List<facility>();
        float minDist = 1000000;
        facility target = null;
        foreach (facility f in master.allFacilites) {
            if (f.representation.mr.enabled) {
                Vector3 screenPosition = general.camera.WorldToScreenPoint(f.representation.gameObject.transform.position);
                float d = Vector3.Distance(screenPosition, Input.mousePosition);
                // dont scale with screen size since we assume its constant (1280x720 or smth)
                // im like 99% sure this is going to be an issue later on but it doesnt affect me rn sooooooooo
                float size = uiHelper.screenSize(f.representation.mr, f.representation.gameObject.transform.position) / 2.5f;
                if (d < size) {
                    if (d < minDist) {
                        minDist = d;
                        target = f;
                    }
                }

                validTargets.Add(f);
            }
        }

        // sue me
        // look im just assume the user isnt going to give us ks of facilites ok
        if (target is facility) {
            foreach (facility f in validTargets) {
                f.representation.select(false, true);
            }

            target.representation.select(true);
            hoveringOver = target;
        } else if (hoveringOver is facility) {
            foreach (facility f in validTargets) {
                f.representation.select(false, false);
            }

            hoveringOver = null;
        }

        if (Input.GetMouseButtonDown(0) && target is facility) {
            planetFocus.enable(false);
            facilityFocus.enable(true, target.name);
        }
    }
}
