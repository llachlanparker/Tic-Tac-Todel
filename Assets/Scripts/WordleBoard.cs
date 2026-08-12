using UnityEngine;
using UnityEngine.InputSystem;

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

    private int rowIndex;
    private int columnIndex;

    private void Awake()
    {
        rows = GetComponentsInChildren<WordleRow>();
    }

    private void Update()
    {
        WordleRow currentRow = rows[rowIndex];

        // handle backspacing
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            currentRow.tiles[columnIndex].SetLetter('\0');
            columnIndex--;
        }

        else if (columnIndex >= currentRow.tiles.Length)
        {
            // submit row (out of bounds)
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


}