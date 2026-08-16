using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridSquareManager[] _grid;

    public void Awake()
    {
        ResetGrid();
    }

    // empty grid on play
    public void ResetGrid()
    {
        foreach(GridSquareManager gridSquare in _grid)
        {
            gridSquare.SetSquare(GridSquareState.empty);
        }

        for (int i = 0; i < _grid.Length; i++)
        {
            _grid[i].SetSquare(GridSquareState.empty);
            _grid[i].SetSquareId(i);
        }
    }

    public void SpecifySquare(GridSquareState gridSquareState, int square)
    {
        _grid[square].SetSquare(gridSquareState);
    }

    public GridSquareState GetSpecificSquareState(int squareId)
    {
        return _grid[squareId].GetSquareState();
    }

    public bool CheckIfGridFull()
    {
        foreach (GridSquareManager square in _grid)
        {
            if (square.GetSquareState() == GridSquareState.empty)
            {
                return false;
            }
        }
        return true;
    }

    public GridSquareState CheckForWin(int gridSquare1, int gridSquare2, int gridSquare3)
    {
        GridSquareState state1 = _grid[gridSquare1].GetSquareState();
        GridSquareState state2 = _grid[gridSquare2].GetSquareState();
        GridSquareState state3 = _grid[gridSquare3].GetSquareState();

        // states must be equal for win
        if (state1 != GridSquareState.empty)
        {
            if (state1 == state2 && state1 == state3)
            {
                return state1; // returns the same player as on the square states
            }
            else
            {
                return GridSquareState.empty;
            }
        }
        return GridSquareState.empty;
    }
}
