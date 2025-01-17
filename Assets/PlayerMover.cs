using System.Collections;
using System.Collections.Generic;
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
    public NetworkVariable<int> appearanceIndex = new NetworkVariable<int>(0);
    public NetworkVariable<int> remainingHealth = new NetworkVariable<int>(3);

    [SerializeField] GameObject beamPrefab;
    [SerializeField] GameObject blockerPrefab;
    [SerializeField] GameObject pointerModel;

    [SerializeField] InputActionReference cursorPosition;
    Vector2 mousePos;
    Vector2 shootDirection;

    float currentRotation = 0;
    bool shootCooldown = false;

    [SerializeField] bool canMove = true;

    public static PlayerMover MyPlayerInstance;

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

    }

    private async void Start()
    {
        while(MatchManager.Instance == null) {
            await Task.Yield();
        }

        if (IsOwner)
        {
            if (IsHost)
            {
                appearanceIndex.Value = Random.Range(0, 2);
                MatchManager.Instance.hostVariant.Value = appearanceIndex.Value;
            }
            else
            {
                //the opposite of the host
                appearanceIndex.Value = MatchManager.Instance.hostVariant.Value == 1 ? 0 : 1;
            }
        }

        GetComponent<SpriteRenderer>().sprite = SpriteBank.Instance.GetPlayerSprite(appearanceIndex.Value);

        if (!IsOwner) {
            pointerModel.SetActive(false);
            return;
        }

        MyPlayerInstance = this;

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

        if (Input.GetKeyDown(KeyCode.Mouse0)) {
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

        RaycastHit2D hit = Physics2D.Raycast(transform.position + new Vector3(shootDirection.x, shootDirection.y, 0), shootDirection, 100);
        Debug.DrawRay(transform.position + new Vector3(shootDirection.x, shootDirection.y, 0), shootDirection * 100, Color.red, 10);

        Vector2 endPos = hit ? hit.point : (Vector2)transform.position + shootDirection * 100;

        BeamVisualServerRpc(transform.position, endPos);

        if (!hit)
        {
            Debug.Log("NO HIT");
            return;

        }

        if (hit.collider.CompareTag("Player")) {
            hit.collider.GetComponent<PlayerMover>().DieRpc();
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

        Debug.Log("Player died");
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
