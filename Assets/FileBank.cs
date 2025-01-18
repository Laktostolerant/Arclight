using UnityEngine;

public class FileBank : MonoBehaviour
{
    public static FileBank Instance;

    public Sprite[] playerSprites;

    public string[] positiveMessages;
    public string[] negativeMessages;

    public GameObject[] blockerPrefabs;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public Sprite GetPlayerSprite(int index)
    {
        return playerSprites[index];
    }

    public string GetPositiveMessage(int index)
    {
        return positiveMessages[index];
    }

    public string GetNegativeMessage(int index)
    {
        return negativeMessages[index];
    }

    public GameObject GetBlockerPrefab(int index)
    {
        return blockerPrefabs[index];
    }
}
