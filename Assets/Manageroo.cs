using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Manageroo : MonoBehaviour
{
    
    public void CreateServer()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("PlayScene", LoadSceneMode.Single);
    }

    public void JoinServer()
    {
        NetworkManager.Singleton.StartClient();
    }
}
