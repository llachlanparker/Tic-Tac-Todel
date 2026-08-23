using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private GridManager _gridManager;
    [SerializeField] private WordleBoard _wordleBoard;

    private Turn _currentTurn;
    private GridSquareState _playerSquareState;
    private GridSquareState _opponentSquareState;
    private bool _awaitingInput = false;
    private TicTacToeResult _currentGameState;

    // track either solving or placing
    private enum GamePhase { Wordle, TicTacToe }
    private GamePhase _gamePhase;

    private bool _currentTurnWordleSolved = false;
    private string _currentTicTacToeResult = ""; // "p1win", "p2win", or "draw"

    // UI
    [SerializeField] private TextMeshProUGUI _playerTrackerText;
    [SerializeField] private TextMeshProUGUI _opponentTrackerText;
    [SerializeField] private TextMeshProUGUI _currentPlayerNumberText;

    [SerializeField] private GameObject _gameResults;
    [SerializeField] private TextMeshProUGUI _resultText;

    [SerializeField] private GameObject _guide;
    [SerializeField] private GameObject _stats;
    [SerializeField] private GameObject _user;

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

        _wordleBoard.OnWordleComplete += HandleWordleComplete;
    }
    
    private void Start()
    {
        StartNewGame();
    }

    // restart btn
    private void RestartAll()
    {
        StartNewGame();
    }

    // how-to-play
    public void OpenGuide()
    {
        _guide.gameObject.SetActive(true);
    }
    public void CloseGuide()
    {
        _guide.gameObject.SetActive(false);
    }

    // stats
    public void OpenStats()
    {
        _stats.gameObject.SetActive(true);
    }
    public void CloseStats()
    {
        _stats.gameObject.SetActive(false);
    }

    // user
    public void OpenUser()
    {
        _user.gameObject.SetActive(true);
    }
    public void CloseUser()
    {
        _user.gameObject.SetActive(false);
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
        StartWordle();
    }

    private void StartWordle()
    {
        _gamePhase = GamePhase.Wordle;
        _awaitingInput = false; // can't place marks yet
        _wordleBoard.StartNewWordle();
    }

    // start tictactoe
    private void HandleWordleComplete(bool solved)
    {
        _currentTurnWordleSolved = solved;
        
        if (solved)
        {
            _gamePhase = GamePhase.TicTacToe;
            _awaitingInput = true;
        }
        else
        {
            ChangeTurn();
            StartWordle();
        }
    }

    // save scores
    private void SaveGameResult()
    {
        string result;
        if (_currentGameState == TicTacToeResult.playerWin) 
        {
            result = "win";
            _currentTicTacToeResult = "Player 1";
        }
        else if (_currentGameState == TicTacToeResult.opponentWin) 
        {
            result = "loss";
            _currentTicTacToeResult = "Player 2";
        }
        else 
        {
            result = "draw";
            _currentTicTacToeResult = "draw";
        }

        // Send to PlayerScores
        if (PlayerScores.instance != null)
        {
            PlayerScores.instance.AddGameResult(result, _currentTicTacToeResult);
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
            StartWordle();
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
                SaveGameResult();
                _resultText.text = _currentTicTacToeResult.ToString() + " wins!";
                return true;
            }
            else if (winner == _opponentSquareState)
            {
                // player2 has won
                _currentGameState = TicTacToeResult.opponentWin;
                SaveGameResult();
                _resultText.text = _currentTicTacToeResult.ToString() + " wins!";
                return true;
            }
        }
        else
        {
            if (gridFull)
            {
                // draw
                _currentGameState = TicTacToeResult.draw;
                SaveGameResult();
                _resultText.text = "draw!";
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
        winner = _gridManager.CheckForWin(0, 1, 2);
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
        winner = _gridManager.CheckForWin(0, 3, 6);
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
        winner = _gridManager.CheckForWin(0, 4, 8);
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
        // only allow clicks in tictactoe phase
        if (_gamePhase != GamePhase.TicTacToe)
            return;

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

    // reset all gameobjects after logout
    public void CloseAllPanels()
    {
        _guide.SetActive(false);
        _stats.SetActive(false);
        _user.SetActive(false);
        _gameResults.SetActive(false);
        StartNewGame(); 
    }
}

public enum Turn { playerTurn, opponentTurn }
public enum TicTacToeResult { ongoing, draw, playerWin, opponentWin}