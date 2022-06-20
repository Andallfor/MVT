using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class testScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(master.allSatellites.Count);
        Debug.Log(master.allSatellites[0].name);
    }

    public void Update() {
        if (Input.GetKeyDown("f")) {
            SceneManager.LoadScene("main", LoadSceneMode.Single);
        }
    }
}
