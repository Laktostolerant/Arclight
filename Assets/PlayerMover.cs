using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMover : NetworkBehaviour
{
    public NetworkVariable<int> remainingHealth = new NetworkVariable<int>(3);

    [SerializeField] GameObject beamPrefab;
    [SerializeField] GameObject pointerModel;

    [SerializeField] InputActionReference cursorPosition;
    Vector2 mousePos;
    Vector2 shootDirection;

    CinemachineImpulseSource impulseSource;

    float currentRotation = 0;
    bool shootCooldown = false;

    bool canMove = true;

    public static PlayerMover MyPlayerInstance;

    //IReadOnlyDictionary<ulong, NetworkClient> PLAYERS = NetworkManager.Singleton.ConnectedClients;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Start()
    {
        if (!IsOwner) {
            pointerModel.SetActive(false);
            return;
        }

        MyPlayerInstance = this;

        impulseSource = GetComponent<CinemachineImpulseSource>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        if (!IsOwner) {
            return;
        }

        mousePos = MovePointer();
        Vector2 direction = mousePos - (Vector2)transform.position;
        shootDirection = direction.normalized;
        currentRotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        pointerModel.transform.rotation = Quaternion.Euler(0, 0, currentRotation);

        if (shootCooldown || !canMove) return;

        MovePlayer();

        if (Input.GetKeyDown(KeyCode.Space)) {
            ShootBeam();
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
        TellServerToSpawnBeamRpc(currentRotation - 90);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void TellServerToSpawnBeamRpc(float rot)
    {
        if (!IsServer) return;

        RaycastHit2D hit = Physics2D.Raycast(transform.position + new Vector3(shootDirection.x, shootDirection.y, 0), shootDirection, 100);
        Debug.DrawRay(transform.position + new Vector3(shootDirection.x, shootDirection.y, 0), shootDirection * 100, Color.red, 10);

        Vector2 endPos = hit ? hit.point : (Vector2)transform.position + shootDirection * 100;
        if (hit && hit.collider.CompareTag("Player"))
        {
            hit.collider.GetComponent<PlayerMover>().DieRpc();
        }
        else
        {

        }

        SpawnBeamRpc(rot, endPos);
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    private void SpawnBeamRpc(float rot, Vector2 endPos)
    {
        CameraShakeManager.Instance.CameraShake(1);
        GameObject beam = Instantiate(beamPrefab, transform.position, Quaternion.Euler(0, 0, rot));
        beam.GetComponent<LineRenderer>().SetPosition(1, new Vector3(0, 100, 0));
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

        SetCanMove(false);
        LoseHealthRpc();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void LoseHealthRpc()
    {
        remainingHealth.Value--;
    }

    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }
}
