using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public static class MirrorUtils
{
    public static void PrintNetworkInfo(NetworkBehaviour networkBehaviour)
    {
        Debug.Log($"isServer: {networkBehaviour.isServer} | isClient: {networkBehaviour.isClient} | connectionToClient: {networkBehaviour.connectionToClient} | connectionToServer: {networkBehaviour.connectionToServer}");
    }
}
