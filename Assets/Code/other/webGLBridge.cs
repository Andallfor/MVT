using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public static class webGLConnection {
    [DllImport("__Internal")]
    public static extern void BrowserTextDownload(string filename, string textContent);
}
