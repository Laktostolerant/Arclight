using Unity.Netcode;
using UnityEngine;

public class Blocker : NetworkBehaviour
{
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void BlockRpc()
    {
        Destroy(gameObject);
    }
}
