using UnityEngine;
using UnityEngine.UI;

public class WordleRow : MonoBehaviour
{
    //get the tile componenet
    public WordleTile[] tiles { get; private set; }

    // collect letters from the tile and form word
    public string word
    {
        get
        {
            string word = "";

            for (int i = 0; i < tiles.Length; i++)
            {
                word += tiles[i].letter;
            }

            return word;
        }
    }

    private void Awake()
    {
        tiles = GetComponentsInChildren<WordleTile>();
    }
}
