using UnityEngine;

public class SpriteBank : MonoBehaviour
{
    public static SpriteBank Instance;

    public Sprite[] playerSprites;
    public Sprite[] blockerSprites;

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
    }

    public Sprite GetPlayerSprite(int index)
    {
        return playerSprites[index];
    }

    public Sprite GetBlockerSprite(int index)
    {
        return blockerSprites[index];
    }
}
