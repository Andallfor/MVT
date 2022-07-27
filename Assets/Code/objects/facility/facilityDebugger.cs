using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class facilityDebugger : MonoBehaviour
{
    public float lat, lon;
    public facilityRepresentation parent;

    public void OnValidate() {
        if (parent is null) return;
        parent.forceChangeGeo(new geographic((double) lat, (double) lon));
    }
}
