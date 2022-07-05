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
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;

        int id = currentId;
        currentId++;

        runningScripts.Add(id, process);

        process.OutputDataReceived += onNewData;
        process.Exited += onExit;

        process.Exited += new EventHandler((sender, e) => pythonWrapper.kill(id));
        process.Disposed += new EventHandler((sender, e) => pythonWrapper.kill(id));

        string errorOuput = "";
        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
            if (String.IsNullOrEmpty(e.Data) && !String.IsNullOrEmpty(errorOuput)) UnityEngine.Debug.LogError($"Script '{Path.GetFileName(scriptPath)}' failed: {errorOuput}");
            else errorOuput += e.Data + '\n';
        });

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) { 
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C {pythonLocation} {scriptPath} {args}";
        }
        if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor) {
            process.StartInfo.FileName = pythonLocation;
            process.StartInfo.Arguments = $"{scriptPath} {args}";
        }

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        return id;
    }

    private static void locatePython() {
        if (pythonLocation != default(string)) return;

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
            pythonLocation = Path.Combine(Application.streamingAssetsPath, Path.Combine("pythonWindows", "python.exe"));
        }
        else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor) {
            pythonLocation = Path.Combine(Application.streamingAssetsPath, Path.Combine("pythonMac", Path.Combine("bin", "python3.10")));
        }
        else throw new System.ApplicationException("Use Mac/Windows- Linux support will be added later");

        
    }

    public static void kill(int id) {
        if (!runningScripts.ContainsKey(id)) return;

        if (!runningScripts[id].HasExited) runningScripts[id].Kill();

        runningScripts.Remove(id);
    }
}
