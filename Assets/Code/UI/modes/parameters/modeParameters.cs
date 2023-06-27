using System;
using UnityEngine;

public static class modeParameters {
    public static void load<T>(T t) where T : IModeParameters {
        general.camera.transform.position = t.cameraPos;
        general.camera.transform.rotation = t.cameraRotation;
        general.camera.orthographic = t.isOrthographic;
        general.camera.orthographicSize = t.orthographicSize;
        general.camera.fieldOfView = t.fov;
        master.scale = t.scale;
        master.currentPosition = t.pos;
    }
}

public abstract class IModeParameters {
    public virtual Vector3 cameraPos {get;} = general.defaultCameraPosition;
    public virtual Quaternion cameraRotation {get;} = Quaternion.Euler(9, 0, 0);
    public virtual float orthographicSize {get;} = 0;
    public virtual float fov {get;} = general.defaultCameraFOV;
    public virtual bool isOrthographic {get;} = false;
    public virtual position pos {get;} = new position(0, 0, 0);
    public virtual double scale {get;} = 1000;
}

public class defaultParameters : IModeParameters {
    public override Vector3 cameraPos => new Vector3(0, 0, -10);
    public override Quaternion cameraRotation => Quaternion.Euler(0, 0, 0);
    public override float orthographicSize => 0;
    public override float fov => 60;
    public override bool isOrthographic => false;
}