using Unity.Cinemachine;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    //CinemachineImpulseListener listener;
    public static CameraShakeManager Instance { get; private set; }

    CinemachineImpulseSource source;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        source = GetComponent<CinemachineImpulseSource>();
    }

    public void CameraShake(float strength)
    {
        source.GenerateImpulseWithForce(strength);

    }
}
