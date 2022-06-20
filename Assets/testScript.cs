using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class testScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    public void Update() {
        if (Input.GetKeyDown("f")) {
            facilityFocus.enable(false, "");
            SceneManager.LoadScene("main", LoadSceneMode.Single);
        }
    }
}
