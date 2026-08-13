using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class WordleBoard : MonoBehaviour
{
    private static readonly Key[] SUPPORTED_KEYS = new Key[]
    {
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H,
        Key.I, Key.J, Key.K, Key.L, Key.M, Key.N, Key.O, Key.P,
        Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X,
        Key.Y, Key.Z
    };

    private WordleRow[] rows;

    // wordle wordlist
    public IReadOnlyList<string> Wordles => _wordles;
    public IReadOnlyCollection<string> NonWordles => _nonWordles;
    public IReadOnlyCollection<string> AllAcceptedGuesses => _allAccepted;

    private List<string> _wordles;
    private HashSet<string> _nonWordles;
    private HashSet<string> _allAccepted;

    private string word;

    private int rowIndex;
    private int columnIndex;

    private void Awake()
    {
        rows = GetComponentsInChildren<WordleRow>();
    }

    private void Start()
    {
        LoadData();
        SetRandomWord(); // to-do: move to different function
    }

    // pick a random word to be the solution
    private void SetRandomWord()
    {
        word = _wordles[Random.Range(0, _wordles.Count)];
    }

    // I'll be honest 80% of this function was vibecoded 
    private void LoadData()
    {
        var wordlesText = Resources.Load<TextAsset>("wordles");
        var nonWordlesText = Resources.Load<TextAsset>("nonwordles");

        // if resources return null (just in case)
        if (wordlesText == null) throw new System.Exception("Missing Resources/wordles.json (TextAsset)");
        if (nonWordlesText == null) throw new System.Exception("Missing Resources/nonwordles.json (TextAsset)");

        // to-do: delete ts this is just to see the nonwordles data since its hashed and the inspector wont freaking show it 
        Debug.Log($"wordlesText null? {wordlesText == null}");
        Debug.Log($"nonWordlesText null? {nonWordlesText == null}");
        if (nonWordlesText != null)
            Debug.Log("nonWordles preview: " + nonWordlesText.text.Substring(0, Mathf.Min(80, nonWordlesText.text.Length)));
        var parsedNon = ParseJsonStringArray(nonWordlesText.text);
        Debug.Log("Parsed nonwordles count: " + parsedNon.Count);
        _nonWordles = new HashSet<string>(parsedNon, System.StringComparer.OrdinalIgnoreCase);
        Debug.Log("HashSet nonwordles count: " + _nonWordles.Count);

        // parse data into strings
        _wordles = ParseJsonStringArray(wordlesText.text);
        _nonWordles = new HashSet<string>(ParseJsonStringArray(nonWordlesText.text), System.StringComparer.OrdinalIgnoreCase);

        _allAccepted = new HashSet<string>(_wordles, System.StringComparer.OrdinalIgnoreCase);
        _allAccepted.UnionWith(_nonWordles);
    }

    // This was also vibecoded (can you tell idk how json files work in unity? Thats why I'm using csv for my other data ;))
    // For JSON like: ["cigar","rebut","sissy",...]
    private static List<string> ParseJsonStringArray(string json)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        // make sure all words have no spaces
        json = json.Trim();
        if (!json.StartsWith("[") || !json.EndsWith("]")) return result;

        // Extract quoted strings: "...."
        int i = 0;
        while (i < json.Length)
        {
            int quoteStart = json.IndexOf('"', i);
            if (quoteStart < 0) break;

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) break;

            string value = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
            if (!string.IsNullOrWhiteSpace(value))
                result.Add(value.Trim());

            i = quoteEnd + 1;
        }

        return result;
    }

    private void Update()
    {
        WordleRow currentRow = rows[rowIndex];

        // handle backspacing
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            columnIndex = Mathf.Max(columnIndex - 1, 0); // avoid negative columnIndex (0 > 1 assign index to zero)
            currentRow.tiles[columnIndex].SetLetter('\0'); // null
        }

        else if (columnIndex >= currentRow.tiles.Length)
        {
            // submit row
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                SubmitRow(currentRow);
            }
        }
        else
        {
            // go through to array to type
            for (int i = 0; i < SUPPORTED_KEYS.Length; i++)
            {
                if (Keyboard.current[SUPPORTED_KEYS[i]].wasPressedThisFrame)
                {
                    if (columnIndex < currentRow.tiles.Length)
                    {
                        currentRow.tiles[columnIndex].SetLetter((char)('A' + i));
                        columnIndex++;
                    }

                    break;
                }
            }
        }
    }

    // compare word to answer before submit; change colour states
    private void SubmitRow(WordleRow row)
    {
        // to-do: update logic
        for (int i = 0; i < row.tiles.Length; i++)
        {
            WordleTile tile = row.tiles[i];

            // access the char
            if (tile.letter == word[i])
            {
                // correct state
            }
            else if (word.Contains(tile.letter))
            {
                // wrong spot
            }
            else
            {
                // incorrect
            }
        }

        // to-do: fix issue when submitting a row, the rowindex does increase but typing stops working (columnindex wont update either) 
        // not sure what the issue is yet sorry future me
        rowIndex++;
        columnIndex = 0;

        // if exceeded row number (to-do: change to scroll function)
        if (rowIndex >= rows.Length)
        {
            enabled = false;
        }
    }
}