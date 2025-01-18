using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    [SerializeField] Image fadeOutImage;
    bool isFading;

    [SerializeField] public Image[] playerPortraits;
    [SerializeField] public TextMeshProUGUI[] playerTextBoxes;

    [SerializeField] Sprite[] characterVariants;
    [SerializeField] Sprite[] blockerVariants;

    List<PlayerMover> players = new List<PlayerMover>();

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
        //check number of connected players
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2 && IsHost)
        {
            //start the game
            StartGameRpc();
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void StartGameRpc()
    {
        foreach (var p in FindObjectsByType(typeof(PlayerMover), FindObjectsSortMode.None))
        {
            PlayerMover playerMover = p.GetComponent<PlayerMover>();
            players.Add(playerMover);
        }

        int chosenColor = Random.Range(0, characterVariants.Length);
        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetAppearanceRpc(chosenColor);
            chosenColor = chosenColor == 0 ? 1 : 0;
        }

        //StartCoroutine(FadeIn());
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void SetColorsRpc(int color)
    {
        Debug.Log("Setting colors");

        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetAppearanceRpc(color);
            //check if it is the host player

            int portraitIndex = players[i].IsHost ? 0 : 1;
            playerPortraits[portraitIndex].sprite = characterVariants[color];

            color = color == 0 ? 1 : 0;
        }

    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void StartNewRoundRpc()
    {
        //StartCoroutine(FadeIn());
        Debug.Log("Starting new round");
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void GameOverRpc()
    {
        PlayerMover.MyPlayerInstance.SetCanMove(false);
    }


    private IEnumerator FadeIn()
    {
        fadeOutImage.color = new UnityEngine.Color(0, 0, 0, 1);
        isFading = true;
        while (fadeOutImage.color.a > 0)
        {
            fadeOutImage.color -= new UnityEngine.Color(0, 0, 0, 0.01f);
            yield return new WaitForSeconds(0.01f);
        }
        isFading = false;
    }

    private IEnumerator FadeOut()
    {
        fadeOutImage.color = new UnityEngine.Color(0, 0, 0, 0);
        isFading = true;
        while (fadeOutImage.color.a < 1)
        {
            fadeOutImage.color += new UnityEngine.Color(0, 0, 0, 0.01f);
            yield return new WaitForSeconds(0.005f);
        }
        isFading = false;
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void SetPlayerPortraitsRpc(int playerIndex, int portraitIndex)
    {
        playerPortraits[playerIndex].sprite = characterVariants[portraitIndex];
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
}
