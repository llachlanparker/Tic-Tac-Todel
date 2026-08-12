using UnityEngine;
using UnityEngine.UI;

public class WordleRow : MonoBehaviour
{
    //get the tile componenet
    public WordleTile[] tiles { get; private set; }

    private void Awake()
    {
        tiles = GetComponentsInChildren<WordleTile>();
    }
}
