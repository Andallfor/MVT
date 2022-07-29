using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Newtonsoft.Json;
using UnityEngine.Networking;
//using System.Data;
//using Mono.Data.Sqlite;

public class DBReader
{
    public Dictionary<string, object>[] result;

    public IEnumerator getData(string file, Action callback) {
        string url = Path.Combine(Application.streamingAssetsPath, file);

        using (UnityWebRequest uwr = UnityWebRequest.Get(url)) {
            yield return uwr.SendWebRequest();

            result = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(uwr.downloadHandler.text);
        }

        callback();
    }
}   
