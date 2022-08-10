using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class downloaderHelper : MonoBehaviour
{

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void downloadToFile(string filename, string textContent);
    public void BrowserTextDownload(string filename, string textContent) {downloadToFile(textContent, filename);}
#else
    public void BrowserTextDownload(string filename, string textContent) {}
#endif

    public static downloaderHelper self;

    public void Awake() {self = this;}
}
