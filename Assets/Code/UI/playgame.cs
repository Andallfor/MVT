using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playgame : MonoBehaviour
{
    // Start is called before the first frame update
    public void Playgame(){
        SceneManager.LoadScene(1);
    }
}
