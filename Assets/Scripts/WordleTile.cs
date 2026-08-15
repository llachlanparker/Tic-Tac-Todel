using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordleTile : MonoBehaviour
{
    [System.Serializable] // show tile state data in inspector

    // tile colours
    public class State
    {
        public Color fillColor;
        public Color outlineColor;
    }

    public State state { get; private set; } // store what state the tile is in
    public char letter { get; private set; } // store what letter is saved in the tile

    private TextMeshProUGUI text; // text within the tile
    private Image fill;
    private Outline outline; // for the state colours

    // get the text component
    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        fill = GetComponent<Image>();
        outline = GetComponent<Outline>();
    }

    // set the letter of the textcomponent to make the word
    public void SetLetter(char letter)
    {
        this.letter = letter;
        text.text = letter.ToString();
    }

    public void SetState(State state)
    {
        this.state = state;
        fill.color = state.fillColor;
        outline.effectColor = state.outlineColor;
    }
}