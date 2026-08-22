using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;

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

    // tile states
    [Header("States")]
    public WordleTile.State selectedState;
    public WordleTile.State emptyState;
    public WordleTile.State occupiedState;
    public WordleTile.State correctState;
    public WordleTile.State wrongSpotState;
    public WordleTile.State incorrectState;

    [Header("UI")]
    public TextMeshProUGUI invalidWordText;

    public event Action<bool> OnWordleComplete;

    private void Awake()
    {
        rows = GetComponentsInChildren<WordleRow>();
        LoadData();
        enabled = false;
    }

    private void LoadData()
    {
        var wordlesText = Resources.Load<TextAsset>("wordles");
        _wordles = ParseJsonStringArray(wordlesText.text);

        var nonWordlesText = Resources.Load<TextAsset>("nonwordles");
        _nonWordles = new HashSet<string>(ParseJsonStringArray(nonWordlesText.text));

        _allAccepted = new HashSet<string>(_wordles);
        _allAccepted.UnionWith(_nonWordles);
    }

    public void StartNewWordle()
    {
        rowIndex = 0;
        columnIndex = 0;

        // clear tiles
        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].tiles.Length; c++)
            {
                rows[r].tiles[c].SetLetter('\0');
                rows[r].tiles[c].SetState(emptyState);
            }
        }

        SetRandomWord();
        invalidWordText.gameObject.SetActive(false);
        enabled = true; // re-enable Update()
    }

    // pick a random word to be the solution
    private void SetRandomWord()
    {
        if (_wordles == null || _wordles.Count == 0)
        {
            Debug.LogError("No wordles loaded! Check that wordles.json exists in Resources folder.");
            word = "hello"; // fallback
            return;
        }

        word = _wordles[UnityEngine.Random.Range(0, _wordles.Count)];
    }

    // For JSON like: ["cigar","rebut", etc..]
    private static List<string> ParseJsonStringArray(string json)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        // make sure all words have no spaces
        json = json.ToLower().Trim();
        if (!json.StartsWith("[") || !json.EndsWith("]")) return result;

        // Extract quoted strings
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

    private void SelectionState(WordleRow row, int newColumnIndex)
    {
        newColumnIndex = Mathf.Clamp(newColumnIndex, 0, row.tiles.Length - 1);

        // find current selected tile
        for (int i = 0; i < row.tiles.Length; i++)
        {
            // only update tiles that are currently selected
            if (row.tiles[i].state == selectedState)
            {
                row.tiles[i].SetState(emptyState);
            }
        }
    }

    private void Update()
    {
        if (!UserManager.instance.IsLoggedIn()) return;
        
        WordleRow currentRow = rows[rowIndex];

        // handle backspacing
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            columnIndex = Mathf.Max(columnIndex - 1, 0); // 0 > 1 assign index to zero
            
            currentRow.tiles[columnIndex].SetLetter('\0');
            currentRow.tiles[columnIndex].SetState(emptyState);

            SelectionState(currentRow, columnIndex);

            invalidWordText.gameObject.SetActive(false);
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
                currentRow.tiles[columnIndex].SetState(selectedState);

                if (Keyboard.current[SUPPORTED_KEYS[i]].wasPressedThisFrame)
                {
                    if (columnIndex < currentRow.tiles.Length)
                    {
                        currentRow.tiles[columnIndex].SetLetter((char)('a' + i));
                        currentRow.tiles[columnIndex].SetState(occupiedState);
                        columnIndex++;
                    }

                    break;
                }
            }
        }
    }

    private void SubmitRow(WordleRow row)
    {
        if (!IsValidWord(row.word))
        {
            invalidWordText.gameObject.SetActive(true);
            return;
        }

        // Count how many times each letter appears in the solution
        var counts = new Dictionary<char, int>();
        foreach (char c in word)
        {
            if (!counts.ContainsKey(c)) counts[c] = 0;
            counts[c]++;
        }

        // first pass mark correct
        for (int i = 0; i < row.tiles.Length; i++)
        {
            WordleTile tile = row.tiles[i];

            if (tile.letter == word[i])
            {
                tile.SetState(correctState);
                counts[tile.letter]--;
            }
        }

        // second pass mark wrongSpotState, else incorrect
        for (int i = 0; i < row.tiles.Length; i++)
        {
            WordleTile tile = row.tiles[i];

            // keep correct tiles as correct
            if (tile.state == correctState)
                continue;

            char c = tile.letter;

            // when the answer has one letter, but the guess contains two of that letter, 
            // only SetState one letter to correct or wrongspot
            if (counts.TryGetValue(c, out int remaining) && remaining > 0)
            {
                tile.SetState(wrongSpotState);
                counts[c]--;
            }
            else
            {
                tile.SetState(incorrectState);
            }
        }

        // win, start tic tac toe placement
        if (HasWon(row))
        {
            enabled = false;
            OnWordleComplete?.Invoke(true);
            return;
        }

        // advance to next row
        rowIndex++;
        columnIndex = 0;

        if (rowIndex >= rows.Length)
        {
            enabled = false; // disable script (update wont be called)
            OnWordleComplete?.Invoke(false);
        }
    }

    // validate word is real
    private bool IsValidWord(string word)
    {
        return _allAccepted.Contains(word);
    }

    // win
    private bool HasWon(WordleRow row)
    {
        for (int i = 0; i < row.tiles.Length; i++)
        {
            if (row.tiles[i].state != correctState)
            {
                return false;
            }
        }

        return true;
    }
}