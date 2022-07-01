using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System;

public static class pythonWrapper
{
    private static string pythonLocation;
    public static Dictionary<int, Process> runningScripts = new Dictionary<int, Process>();
    private static Dictionary<int, string> errorMessages = new Dictionary<int, string>();
    private static int currentId = 0;
    public static int runPython(string scriptPath, string args, DataReceivedEventHandler onNewData = null, EventHandler onExit = null) {
        locatePython();

        Process process = new Process();
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/C {pythonLocation} {scriptPath} {args}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;
        int id = currentId;

        runningScripts.Add(id, process);

        process.OutputDataReceived += onNewData;
        process.Exited += onExit;

        process.Exited += new EventHandler((sender, e) => pythonWrapper.kill(id));
        process.Disposed += new EventHandler((sender, e) => pythonWrapper.kill(id));

        string errorOuput = "";
        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
            if (String.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError($"Script '{Path.GetFileName(scriptPath)}' failed: {errorOuput}");
            else errorOuput += e.Data + '\n';
        });

        process.Start();

        currentId++;
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        return id;
    }

    private static void locatePython() {
        if (pythonLocation != default(string)) return;

        string os = "";
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) os = "Windows";
        else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor) os = "Mac";
        else throw new System.ApplicationException("Use Mac/Windows- Linux support will be added later");

        pythonLocation = Path.Combine(Application.streamingAssetsPath, Path.Combine("python" + os, "python.exe"));
    }

    public static void kill(int id) {
        if (!runningScripts.ContainsKey(id)) return;

        if (!runningScripts[id].HasExited) runningScripts[id].Kill();

        runningScripts.Remove(id);
    }
}
