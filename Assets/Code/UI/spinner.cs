using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spinner : MonoBehaviour
{
    public float direction;

    void Update()
    {
        transform.Rotate(new Vector3(0, 4f, 0) * UnityEngine.Time.deltaTime * direction);
    }
}
