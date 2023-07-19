using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class webglBridge {
    [DllImport("__Internal")]
    public static extern void BrowserTextDownload(string filename, string textContent);
}
