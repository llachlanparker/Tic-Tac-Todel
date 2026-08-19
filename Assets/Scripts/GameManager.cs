using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private GridManager _gridManager;
    private Turn _currentTurn;
    private GridSquareState _playerSquareState;
    private GridSquareState _opponentSquareState;
    private bool _awaitingInput = false;
    private TicTacToeResult _currentGameState;

    // UI
    [SerializeField] private TextMeshProUGUI _playerTrackerText;
    [SerializeField] private TextMeshProUGUI _opponentTrackerText;
    [SerializeField] private TextMeshProUGUI _currentPlayerNumberText;

    [SerializeField] private GameObject _gameResults;
    [SerializeField] private TextMeshProUGUI _resultText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("There are more than one GameManagers in this scene");
        }

        StartNewGame();
    }

    private void RestartAll()
    {
        StartNewGame();
    }

    private void StartNewGame()
    {
        _currentGameState = TicTacToeResult.ongoing;

        _gridManager.ResetGrid(); // empty grid on play

        // player 1 always has first turn
        int firstTurn = 0;
        _currentTurn = (Turn)firstTurn;

        // Assign grid square state to player
        if (firstTurn == 0)
        {
            _playerSquareState = GridSquareState.o;
            _opponentSquareState = GridSquareState.x;
        }
        else
        {
            _playerSquareState = GridSquareState.x;
            _opponentSquareState = GridSquareState.o;
        }

        // Set player tracker UI
        _gameResults.SetActive(false);
        _playerTrackerText.text = _playerSquareState.ToString();
        _opponentTrackerText.text = _opponentSquareState.ToString();

        SetCurrentTurnUI();
        _awaitingInput = true; // wait for player to click on a square
    }

    // save scores
    private void SaveGameResult()
    {
        if (_currentGameState == TicTacToeResult.playerWin)
        {
            PlayerData.instance.AddScores(1, 0, 0);
        }
        else if (_currentGameState == TicTacToeResult.opponentWin)
        {
            PlayerData.instance.AddScores(0, 1, 0);
        }
        else if (_currentGameState == TicTacToeResult.draw)
        {
            PlayerData.instance.AddScores(0, 0, 1);
        }
    }

    private void ProcessTurn(Turn turn, int selectedSquare)
    {
        _awaitingInput = false;

        GridSquareState state = GridSquareState.empty;
        if (turn == Turn.playerTurn)
        {
            state = _playerSquareState;
        }
        else
        {
            state = _opponentSquareState;
        }

        _gridManager.SpecifySquare(state, selectedSquare);

        bool gameEnded = CheckIfGameEnded();
        if (!gameEnded)
        {
            ChangeTurn();
            _awaitingInput = true;
        }
        else
        {
            _gameResults.SetActive(true);
        }
    }

    private bool CheckIfGameEnded()
    {
        bool gridFull = _gridManager.CheckIfGridFull();
        GridSquareState winner = CheckForWin();

        if (winner != GridSquareState.empty)
        {
            if (winner == _playerSquareState)
            {
                // player1 has won
                _currentGameState = TicTacToeResult.playerWin;
                _resultText.text = _playerSquareState.ToString() + " wins!";
                // SaveGameResult();
                return true;
            }
            else if (winner == _opponentSquareState)
            {
                // player2 has won
                _currentGameState = TicTacToeResult.opponentWin;
                _resultText.text = _playerSquareState.ToString() + " wins!";
                // SaveGameResult();
                return true;
            }
        }
        else
        {
            if (gridFull)
            {
                // draw
                _currentGameState = TicTacToeResult.draw;
                _resultText.text = "draw...";
                /// SaveGameResult();
                return true;
            }
            else
            {
                // ongoing
                return false;
            }
        }
        return false;
    }

    private GridSquareState CheckForWin()
    {
        GridSquareState winner = GridSquareState.empty;

        //...
        // check for matching states in rows
        // 0 | 1 | 2
        // 3 | 4 | 5
        // 6 | 7 | 8
        //...

        // horizontal win conditions
        _gridManager.CheckForWin(0, 1, 2);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }
        winner = _gridManager.CheckForWin(3, 4, 5);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }
        winner = _gridManager.CheckForWin(6, 7, 8);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }

        // vertical win conditions
        _gridManager.CheckForWin(0, 3, 6);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }
        winner = _gridManager.CheckForWin(1, 4, 7);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }
        winner = _gridManager.CheckForWin(2, 5, 8);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }

        // diagonal win conditions
        _gridManager.CheckForWin(0, 4, 8);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }
        winner = _gridManager.CheckForWin(2, 4, 6);
        if (winner != GridSquareState.empty)
        {
            return winner;
        }

        return winner;
    }

    public void ChangeTurn()
    {
        if (_currentTurn == Turn.playerTurn)
        {
            _currentTurn = Turn.opponentTurn;
        }
        else
        {
            _currentTurn = Turn.playerTurn;
        }
        SetCurrentTurnUI();
    }

    private void SetCurrentTurnUI()
    {
        if (_currentTurn == Turn.playerTurn)
        {
            _currentPlayerNumberText.text = "Player 1";
        }
        else
        {
            _currentPlayerNumberText.text = "Player 2";
        }
    }

    public void GridSquareClicked(int clickedSquare)
    {
        if (_awaitingInput == false)
        {
            return;
        }

        if (_gridManager.GetSpecificSquareState(clickedSquare) != GridSquareState.empty)
        {
            return;
        }

        ProcessTurn(_currentTurn, clickedSquare);
    }
}

public enum Turn { playerTurn, opponentTurn }
public enum TicTacToeResult { ongoing, draw, playerWin, opponentWin}