using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;
    int connectedPlayers = 0;

    [SerializeField] Sprite[] characterVariants;
    [SerializeField] Sprite[] blockerVariants;

    public NetworkVariable<int> hostVariant = new NetworkVariable<int>(0);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        connectedPlayers++;
        Debug.Log("Player connected. Total players: " + connectedPlayers);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void StartNewRoundRpc()
    {

    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void GameOverRpc()
    {
        PlayerMover.MyPlayerInstance.SetCanMove(false);
    }
}
