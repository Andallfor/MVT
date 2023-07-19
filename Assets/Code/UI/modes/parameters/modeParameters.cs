using System;
using UnityEngine;

public static class modeParameters {
    public static void load<T>(T t) where T : IModeParameters {
        IModeParameters d = new emptyParameters();
        if (t.ignoreNonOverride) {
            if (t.cameraPos != d.cameraPos) general.camera.transform.position = t.cameraPos;
            if (t.cameraRotation != d.cameraRotation) general.camera.transform.rotation = t.cameraRotation;
            if (t.isOrthographic != d.isOrthographic) general.camera.orthographic = t.isOrthographic;
            if (t.orthographicSize != d.orthographicSize) general.camera.orthographicSize = t.orthographicSize;
            if (t.fov != d.fov) general.camera.fieldOfView = t.fov;
            if (t.scale != d.scale) master.scale = t.scale;
            if (t.pos != d.pos) master.currentPosition = t.pos;
        } else {
            general.camera.transform.position = t.cameraPos;
            general.camera.transform.rotation = t.cameraRotation;
            general.camera.orthographic = t.isOrthographic;
            general.camera.orthographicSize = t.orthographicSize;
            general.camera.fieldOfView = t.fov;
            master.scale = t.scale;
            master.currentPosition = t.pos;
        }
    }
}

public abstract class IModeParameters {
    public virtual Vector3 cameraPos {get;} = general.defaultCameraPosition;
    public virtual Quaternion cameraRotation {get;} = Quaternion.Euler(0, 0, 0);
    public virtual float orthographicSize {get;} = 0;
    public virtual float fov {get;} = general.defaultCameraFOV;
    public virtual bool isOrthographic {get;} = false;
    public virtual position pos {get;} = new position(0, 0, 0);
    public virtual double scale {get;} = 1000;
    public virtual bool ignoreNonOverride {get;} = false;
}

public class defaultParameters : IModeParameters {
    public override Vector3 cameraPos => new Vector3(0, 0, -10);
    public override Quaternion cameraRotation => Quaternion.Euler(0, 0, 0);
    public override float orthographicSize => 0;
    public override float fov => 60;
    public override bool isOrthographic => false;
}

public class emptyParameters : IModeParameters {}
