using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [SerializeField] private LineRenderer lr;
    [SerializeField] private Transform[] Tpoints;

    private void OnEnable() 
    {
        this.lr = GetComponent<LineRenderer>();
    }

    public void testlr(Transform[] Tpoints)
    {
        lr.positionCount = Tpoints.Length;
        this.Tpoints = Tpoints;
    }


    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Tpoints.Length; i++)
        {
            lr.SetPosition(i, Tpoints[i].position);
        }
    }
}
