using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;
using Mirror.SimpleWeb;

public static class web {
    public static bool initialized {get; private set;}
    public static bool isClient {get; private set;}
    private static Dictionary<byte, webRequestHandle> handles = new Dictionary<byte, webRequestHandle>();

    public static bool handleExists(byte h) => handles.ContainsKey(h);
    public static void setHandleSend(byte key, Func<byte, byte[], byte[]> send) {
        if (!initialized) throw new Exception("web class has not yet been initialized");
        if (Enum.IsDefined(typeof(constantWebHandles), key)) throw new ArgumentException("Attempting to set handle key to a constant key");
        if (!handleExists(key)) handles.Add(key, default(webRequestHandle));

        if (handles[key].send != default(Func<byte, byte[], byte[]>)) Debug.LogWarning($"Attempting to set handle send {key}, but it already exists.");

        handles[key].send = send;
    }

    public static void setHandleReceive(byte key, Action<byte, byte[]> receive) {
        if (!initialized) throw new Exception("web class has not yet been initialized");
        if (Enum.IsDefined(typeof(constantWebHandles), key)) throw new ArgumentException("Attempting to set handle key to a constant key");
        if (!handleExists(key)) handles.Add(key, default(webRequestHandle));

        if (handles[key].receive != default(Action<byte, byte[]>)) Debug.LogWarning($"Attempting to set handle receive {key}, but it already exists.");

        handles[key].receive = receive;
    }

    public static void removeHandle(byte key) {
        if (!initialized) throw new Exception("web class has not yet been initialized");
        if (Enum.IsDefined(typeof(constantWebHandles), key)) throw new ArgumentException("Attempting to remove a constant key");
        if (!handleExists(key)) throw new ArgumentException("Handle key does not exist");

        handles.Remove(key);
    }

    public static void addHandle(byte key, Func<byte, byte[], byte[]> send, Action<byte, byte[]> receive) {
        webRequestHandle handle = new webRequestHandle() {
            key = key,
            send = send,
            receive = receive
        };

        handles[key] = handle;
    }

    public static void sendMessage(byte key, byte[] content) {
        if (!initialized) throw new Exception("web class has not yet been initialized");
        if (!handleExists(key)) throw new ArgumentException("Handle key does not exist");

        webConnection.self.send(key, content);
    }

    // send is the function we want to run on the server
    public static byte[] runHandleSend(byte key, byte[] content) {
        if (!initialized) throw new Exception("web class has not yet been initialized");
        if (!handleExists(key)) throw new ArgumentException("Handle key does not exist");
        if (handles[key].send == default(Func<byte, byte[], byte[]>)) throw new Exception("Handle send does not exist");

        return handles[key].send(key, content);
    }

    // receive is the function we want to run on the client
    public static void runHandleReceive(byte key, byte[] content) {
        if (!initialized) throw new Exception("web class has not yet been initialized");
        if (!handleExists(key)) throw new ArgumentException("Handle key does not exist");
        if (handles[key].receive == default(Action<byte, byte[]>)) throw new Exception("Handle receive does not exist");

        handles[key].receive(key, content);
    }

    public static IEnumerator initialize() {
        if (initialized) {
            Debug.LogWarning("Attempting to initialize already initialized web class");
            yield break;
        }

        // try and connect
        GameObject managerGo = GameObject.FindGameObjectWithTag("network/manager");
        NetworkManager manager = managerGo.GetComponent<NetworkManager>();
        SimpleWebTransport transport = managerGo.GetComponent<SimpleWebTransport>();

        bool runAsLocalhost = true; // TODO
        isClient = Application.platform == RuntimePlatform.WebGLPlayer;
        if (isClient) {
            if (runAsLocalhost) {
                manager.networkAddress = "localhost";
                transport.port = 7777;
                transport.clientUseWss = false;
            } else {
                transport.port = 27777;
                transport.clientUseWss = true;
                throw new NotImplementedException("add in auto connection to server");
            }

            manager.StartClient();
        } else {
            bool shouldBeServer = true; // TODO

            if (shouldBeServer) {
                if (runAsLocalhost) {
                    transport.port = 7777;
                    manager.networkAddress = "localhost";
                } else transport.port = 27777;

                manager.StartServer();
            } else yield break;
        }

        // block until we connect
        float totalTime = 0;
        float maxTime = 5;
        bool didConnect = true;
        while (true) {
            if (!isClient && transport.ServerActive()) break;
            if (isClient && transport.ClientFullyConnected()) break; // this doesnt seem to work?
            if (totalTime >= maxTime) {
                didConnect = false;
                break;
            }

            yield return new WaitForSeconds(0.5f);
            totalTime += 0.5f;
        }

        if (!didConnect) {
            throw new Exception("Unable to connect/create server!");
        }

        initialized = true;

        addHandle((byte) constantWebHandles.error, 
            send: (cmd, content) => new byte[0],
            receive: (cmd, content) => {
                Debug.LogError($"Error {content[1]} when running {content[0]}");
        });
        
        addHandle((byte) constantWebHandles.acknowledgement, 
            send: (cmd, content) => new byte[0],
            receive: (cmd, content) => {
                // server has acknowledged us, tell them to continue
                sendMessage((byte) constantWebHandles.proceed, content);
        });

        addHandle((byte) constantWebHandles.ping,
            send: (cmd, content) => {
                Debug.Log("Received client ping with content " + content[0]);
                return new byte[] {10};},
            receive: (cmd, content) => {Debug.Log("Received server ping with content " + content[0]);}
        );

        addHandle((byte) constantWebHandles.proceed, null, null);

        // USER HANDLES
        // TODO: have this call a function instead rather than defining them here
        addHandle((byte) userWebHandles.requestServerScenario, 
            send: (cmd, content) => serverScenario.serializeScenario(), 
            receive: (cmd, content) => serverScenario.deserializeScenario(content)
        );

        Debug.Log("Initialized connection");
    }
}

public class webRequestHandle {
    public byte key;
    public Func<byte, byte[], byte[]> send;
    public Action<byte, byte[]> receive;
}

[Flags]
enum constantWebHandles : byte {
    error = 0,
    acknowledgement = 1,
    ping = 2,
    proceed = 3,

    // reserve some handles for future use (if needed)
    r4 = 4, r5 = 5, r6 = 6, r7 = 7, r8 = 8
}

[Flags]
enum userWebHandles : byte {
    requestServerScenario = 9
}

[Flags]
enum webErrorCodes : byte {
    serverBusy = 0,
    commandAlreadyRunning = 1,
    unknownCommand = 2,
    noContentToProceed = 3,
}