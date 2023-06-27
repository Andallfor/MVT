using System;
using UnityEngine;

public class planetOverviewParameters : IModeParameters {
    public override Vector3 cameraPos => new Vector3(-15, 7.5f, -15);
    public override Quaternion cameraRotation => Quaternion.Euler(19.471f, 45, 0);
    public override float orthographicSize => 5;
    public override float fov => 0;
    public override bool isOrthographic => true;
}
