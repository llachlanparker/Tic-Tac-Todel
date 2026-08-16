using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridSquareManager : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _nought;
    [SerializeField] private Image _cross;
    private GridSquareState _currentState = GridSquareState.empty;
    private int _squareId;

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager.instance.GridSquareClicked(_squareId);
    }

    public GridSquareState GetSquareState()
    {
        return _currentState;
    }

    // whether square should show nought or cross
    public void SetSquare(GridSquareState _newState)
    {
        if (_newState == GridSquareState.empty)
        {
            _nought.enabled = false;
            _cross.enabled = false;
        }
        else if (_newState == GridSquareState.x)
        {
            _cross.enabled = true;        
            _nought.enabled = false;
        }
        else if (_newState == GridSquareState.o)
        {
            _cross.enabled = false;        
            _nought.enabled = true;
        }
        _currentState = _newState;
    }

    public void SetSquareId(int id)
    {
        _squareId = id;
    }
}

public enum GridSquareState { empty, x, o };