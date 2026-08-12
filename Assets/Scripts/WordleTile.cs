using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordleTile : MonoBehaviour
{
    private TextMeshProUGUI text; // text within the tile

    public char letter { get; private set; }

    // get the text component
    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    // set the letter of the textcomponent to make the word
    public void SetLetter(char letter)
    {
        this.letter = letter;
        text.text = letter.ToString();
    }
}