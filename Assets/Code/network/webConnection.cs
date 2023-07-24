using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class webConnection : NetworkBehaviour {
    public bool isActive {get; private set;} = false;
    private static byte[] empty = new byte[0];

    public static webConnection self {
        get {
            // no longer is connected, get new self
            if (ReferenceEquals(_self, null) || !_self.isActive || !_self.isLocalPlayer) {
                GameObject[] connections = GameObject.FindGameObjectsWithTag("network/playerConnection");
                foreach (GameObject go in connections) {
                    webConnection conn = go.GetComponent<webConnection>();
                    if (conn.isLocalPlayer) {
                        _self = conn;
                        return conn;
                    }
                }

                Debug.LogWarning("No local client exists!");
                return null;
            }
            return _self;
        }
    }

    private static webConnection _self = null;

    public void send(byte command, byte[] content) {
        if (!isLocalPlayer) {
            Debug.LogWarning("Attempted to send message on non local player");
            return;
        }

        cmdSend(netIdentity, command, content);
    }

    [Command]
    public void cmdSend(NetworkIdentity sender, byte command, byte[] content) {
        NetworkConnectionToClient target = sender.connectionToClient;

        // proceed is special, dont bother with handshake
        if (command != (byte) constantWebHandles.proceed && !web.handleExists(command)) {
            targetRespond(target, (byte) constantWebHandles.error, getErrorOut(command, webErrorCodes.unknownCommand));
            Debug.LogWarning($"Client attempted to send unknown command {command}");
            return;
        }

        // if proceed command, run the cached content
        if (command == (byte) constantWebHandles.proceed) {
            long hash = getHash(netId, content[0]);

            // run with cached content, if none error out
            if (!cachedContent.ContainsKey(hash)) {
                targetRespond(target, (byte) constantWebHandles.error, getErrorOut(command, webErrorCodes.noContentToProceed));
                return;
            }

            // put this request in the queue
            webRequestContent request = new webRequestContent() {
                key = content[0],
                content = cachedContent[hash],
                id = netId,
                netId = netIdentity};
            
            processingQueue.Enqueue(request);
        } else {
            long hash = getHash(netId, command);

            // we supposed to start a new command, but theres already an instance in the cache
            if (cachedContent.ContainsKey(hash)) {
                // error protocol: {command that caused error, error code}
                targetRespond(target, (byte) constantWebHandles.error, getErrorOut(command, webErrorCodes.commandAlreadyRunning));
                return;
            }

            // cache content to pull from when we run it later
            cachedContent[hash] = content;
            // this seems to be a valid command, send acknowledgment back to client to start whenever they're ready
            targetRespond(target, (byte) constantWebHandles.acknowledgement, new byte[] {command});
        }
    }

    [TargetRpc]
    public void targetRespond(NetworkConnectionToClient target, byte command, byte[] content) {
        if (!web.handleExists(command)) throw new ArgumentException($"Attempted to run unknown command {command}");

        web.runHandleReceive(command, content);
    }

    public void Update() {
        if (!isServer) return;
        if (processingQueue.Count == 0) return;

        webRequestContent req = processingQueue.Dequeue();
        byte[] output = web.runHandleSend(req.key, req.content);
        targetRespond(req.netId.connectionToClient, req.key, output);

        cachedContent.Remove(getHash(req.id, req.key));
    }

    public override void OnStartLocalPlayer() {
        if (!isLocalPlayer) return;
        _self = this;
        isActive = true;
    }

    public override void OnStopAuthority() {
        if (!isLocalPlayer) return;
        _self = null;
        isActive = false;
    }

    public override void OnStopLocalPlayer() {
        if (!isLocalPlayer) return;
        _self = null;
        isActive = false;
    }

    private byte[] getErrorOut(byte cmd, webErrorCodes err) => new byte[] {cmd, (byte) err};

    // queue to actually process the requests
    private Queue<webRequestContent> processingQueue = new Queue<webRequestContent>();

    // holder whilst we the operation is in process
    private Dictionary<long, byte[]> cachedContent = new Dictionary<long, byte[]>();

    private long getHash(long id, byte command) => id << command;
}

struct webRequestContent {
    public byte key;
    public byte[] content;
    public NetworkIdentity netId;
    public long id;
}