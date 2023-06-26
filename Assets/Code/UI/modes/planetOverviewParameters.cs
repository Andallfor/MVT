using System;
using UnityEngine;

public class planetOverviewParameters : IModeParameters {
    public readonly new Vector3 cameraPos = new Vector3(-15, 7.5f, -15);
    public readonly new Quaternion cameraRotation = Quaternion.Euler(19.471f, 45, 0);
    public readonly new float orthographicSize = 5, fov = 0;
    public readonly new bool isOrthographic = true;
}
