using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class PlayerMover : NetworkBehaviour
{
    [SerializeField] GameObject beamPrefab;
    [SerializeField] GameObject deathParticles;
    [SerializeField] GameObject pointerModel;

    [SerializeField] InputActionReference cursorPosition;
    Vector2 mousePos;
    Vector2 shootDirection;

    float currentRotation = 0;
    bool shootCooldown = false;

    public static PlayerMover MyPlayerInstance;

    AudioSource audioSource;

    private int appearanceIndex = 0;

    public enum SoundType { FIRE, DEATH, WIN, LOSE }
    [SerializeField] private AudioClip[] audioClips;

    int portraitSide = 0;

    float chatCooldown = 0;

    float fireCooldown = 0;
    float blockerCooldown = 0;

    GameObject equippedBlocker;

    Rigidbody2D rb;

    ClientNetworkTransform CNTransform;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        CNTransform = GetComponent<ClientNetworkTransform>();
    }

    private async void Start()
    {
        while (MatchManager.Instance == null)
        {
            await Task.Yield();
        }

        GetComponent<SpriteRenderer>().sprite = FileBank.Instance.GetPlayerSprite(appearanceIndex);

        if (!IsOwner)
        {
            pointerModel.SetActive(false);
            return;
        }

        MyPlayerInstance = this;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void GetAppearanceRpc(int index)
    {
        appearanceIndex = index;
        portraitSide = IsHost ? 1 : 0;


        equippedBlocker = FileBank.Instance.GetBlockerPrefab(appearanceIndex);

        GetComponent<SpriteRenderer>().sprite = FileBank.Instance.GetPlayerSprite(appearanceIndex);
    }

    private void Update()
    {
        if (!MatchManager.Instance || !MatchManager.Instance.RoundHasStarted.Value)
        {
            return;
        }

        if (!IsOwner)
        {
            return;
        }

        mousePos = MovePointer();
        Vector2 direction = mousePos - (Vector2)transform.position;
        shootDirection = direction.normalized;
        currentRotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        pointerModel.transform.rotation = Quaternion.Euler(0, 0, currentRotation);

        if (shootCooldown) return;

        MovePlayer();


        fireCooldown -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Mouse0) && fireCooldown <= 0)
        {
            ShootBeam();
            fireCooldown = 3;
        }

        blockerCooldown -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Mouse1) && blockerCooldown <= 0)
        {
            SpawnBlockerRpc();
            blockerCooldown = 6;
        }

        chatCooldown -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Q) && chatCooldown <= 0)
        {
            int randomIndex = Random.Range(0, FileBank.Instance.negativeMessages.Length);
            MatchManager.Instance.SendMessageRpc(portraitSide, FileBank.Instance.positiveMessages[randomIndex]);
            chatCooldown = 2;
        }

        if (Input.GetKeyDown(KeyCode.E) && chatCooldown <= 0)
        {
            int randomIndex = Random.Range(0, FileBank.Instance.negativeMessages.Length);
            MatchManager.Instance.SendMessageRpc(portraitSide, FileBank.Instance.negativeMessages[randomIndex]);
            chatCooldown = 2;
        }
    }

    private Vector2 MovePointer()
    {
        Vector3 pos = cursorPosition.action.ReadValue<Vector2>();
        pos.z = Camera.main.nearClipPlane;
        return Camera.main.ScreenToWorldPoint(pos);
    }

    private void MovePlayer()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(x, y, 0);
        transform.position += move * Time.deltaTime * 5;
    }

    private void ShootBeam()
    {
        shootCooldown = true;

        RaycastHit2D hit = Physics2D.Raycast(transform.position + new Vector3(shootDirection.x, shootDirection.y, 0), shootDirection, 100);
        Debug.DrawRay(transform.position + new Vector3(shootDirection.x, shootDirection.y, 0), shootDirection * 100, UnityEngine.Color.red, 10);

        Vector2 endPos = hit ? hit.point : (Vector2)transform.position + shootDirection * 100;

        BeamVisualServerRpc(transform.position, endPos);

        if (!hit)
        {
            return;
        }

        if (hit.collider.CompareTag("Player"))
        {
            hit.collider.GetComponent<PlayerMover>().DieRpc();
        }
        else if (hit.collider.TryGetComponent(out Blocker blocker))
        {
            blocker.BlockRpc();
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void BeamVisualServerRpc(Vector3 origin, Vector3 end)
    {
        if (!IsServer) return;

        BeamVisualClientRpc(origin, end);
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    private void BeamVisualClientRpc(Vector3 startPos, Vector2 endPos)
    {
        CameraShakeManager.Instance.CameraShake(1);

        Instantiate(deathParticles, endPos, Quaternion.identity);
        PlaySound(SoundType.FIRE);

        GameObject beam = Instantiate(beamPrefab, Vector3.zero, Quaternion.identity);
        beam.GetComponent<LineRenderer>().SetPosition(0, startPos);
        beam.GetComponent<LineRenderer>().SetPosition(1, endPos);
        StartCoroutine(BeamDecay(beam));
    }

    IEnumerator BeamDecay(GameObject beam)
    {
        yield return new WaitForSeconds(0.25F);
        Destroy(beam);
        shootCooldown = false;
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    public void DieRpc()
    {
        if (!IsOwner) return;

        PlayDeathSoundRpc();
        MatchManager.Instance.EndRoundRpc();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    public void PlayDeathSoundRpc()
    {
        if (!IsOwner) return;
        PlaySound(SoundType.DEATH);
    }

    public void PlaySound(SoundType type)
    {
        audioSource.PlayOneShot(audioClips[(int)type]);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SpawnBlockerRpc()
    {
        Vector2 spawnPos = (Vector2)transform.position + shootDirection * 2;
        GameObject spawnedBlocker = Instantiate(equippedBlocker, spawnPos, Quaternion.identity);
        var networkObject = spawnedBlocker.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void TeleportToSpawnRpc(Vector2 spawnPos)
    {
        if (!IsOwner)
            return;

        transform.position = spawnPos;
        CNTransform.Teleport(spawnPos, Quaternion.identity, Vector3.one * 0.5F);
    }
}
