using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class playgame : MonoBehaviour
{
    public GameObject info, controls;

    public void start() {SceneManager.LoadScene(1);}
    public void showInfo() {info.SetActive(true);}
    public void showControls() {controls.SetActive(true);}
    public void hideInfo() {info.SetActive(false);}
    public void hideControls() {controls.SetActive(false);}
    public void quit() {Application.Quit();}
}
