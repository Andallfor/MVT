using System;
using UnityEngine;

public abstract class IModeParameters {
    public readonly Vector3 cameraPos;
    public readonly Quaternion cameraRotation;
    public readonly float orthographicSize, fov;
    public readonly bool isOrthographic;
}

public class defaultParameters : IModeParameters {
    public readonly new Vector3 cameraPos = new Vector3(0, 0, -10);
    public readonly new Quaternion cameraRotation = Quaternion.Euler(0, 0, 0);
    public readonly new float orthographicSize = 0, fov = 60;
    public readonly new bool isOrthographic = false;
}