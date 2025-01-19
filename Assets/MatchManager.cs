using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : NetworkBehaviour
{
    public NetworkVariable<bool> RoundHasStarted = new NetworkVariable<bool>(false);

    public static MatchManager Instance;

    [SerializeField] Image fadeOutImage;
    bool isFading;

    [SerializeField] public Image[] playerPortraits;
    [SerializeField] public TextMeshProUGUI[] playerTextBoxes;

    [SerializeField] Sprite[] characterVariants;
    [SerializeField] Sprite[] blockerVariants;

    List<PlayerMover> players = new List<PlayerMover>();

    Vector2 player1SpawnPos = new Vector2(-7, 0);
    Vector2 player2SpawnPos = new Vector2(7, 0);

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
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2 && IsHost)
            StartGameRpc();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void StartGameRpc()
    {
        PopulatePlayerListRpc();

        int chosenColor = Random.Range(0, characterVariants.Length);
        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetAppearanceRpc(chosenColor);
            chosenColor = chosenColor == 0 ? 1 : 0;

            SetColorsRpc(i, chosenColor);
        }

        StartNewRoundRpc();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void PopulatePlayerListRpc()
    {
        foreach (var p in FindObjectsByType(typeof(PlayerMover), FindObjectsSortMode.None))
        {
            PlayerMover playerMover = p.GetComponent<PlayerMover>();
            players.Add(playerMover);
        }
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void SetColorsRpc(int index, int color)
    {
        playerPortraits[index].sprite = characterVariants[color];
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void EndRoundRpc()
    {
        Debug.Log("Ending round");
        RoundHasStarted.Value = false;
        StartNewRoundRpc();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void StartNewRoundRpc()
    {
        FadeInRpc();
        TeleportPlayersRpc();

        RoundHasStarted.Value = true;
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void TeleportPlayersRpc()
    {
        players[0].transform.position = player1SpawnPos;
        players[1].transform.position = player2SpawnPos;
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void FadeInRpc()
    {
        StartCoroutine(FadeCoroutine());
    }

    IEnumerator FadeCoroutine()
    {
        isFading = true;
        fadeOutImage.gameObject.SetActive(true);

        for (float i = 1; i >= 0; i -= Time.deltaTime * 0.33f)
        {
            fadeOutImage.color = new UnityEngine.Color(0, 0, 0, i);
            yield return null;
        }

        fadeOutImage.color = new UnityEngine.Color(0, 0, 0, 0);
        fadeOutImage.gameObject.SetActive(false);
        isFading = false;
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void SendMessageRpc(int portrait, string message)
    {
        playerTextBoxes[portrait].text = message;
        StartCoroutine(DecayText(portrait));
    }

    IEnumerator DecayText(int index)
    {
        yield return new WaitForSeconds(2);
        playerTextBoxes[index].text = "";
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (IsHost && NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
    }
}
