using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject menuObject;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startButtonText;
    [SerializeField] private GameObject movesObject;
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private GameObject winPanelObject;
    [SerializeField] private Button winPanelButton;
    [SerializeField] private GameObject losePanelObject;
    [SerializeField] private Button losePanelButton;
    
    public event Action StartButtonPressed;
    public event Action WinPanelButtonPressed;
    public event Action LosePanelButtonPressed;

    private void Start()
    {
        startButton.onClick.AddListener(()=> StartButtonPressed?.Invoke());
        winPanelButton.onClick.AddListener(()=> WinPanelButtonPressed?.Invoke());
        losePanelButton.onClick.AddListener(()=> LosePanelButtonPressed?.Invoke());
    }
    
    public void SetStartText(string startText)
    {
        startButtonText.text = startText;
    }

    public void SetMovesText(string moves)
    {
        movesText.text = moves;
    }

    public void SetUIState(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.MainMenu:
                ShowStartScreen();
                break;
            case GameManager.GameState.Gameplay:
                ShowGameplayScreen();
                break;
            case GameManager.GameState.GameWin:
                ShowWinPanel();
                break;
            case GameManager.GameState.GameLose:
                ShowLosePanel();
                break;
        }
    }

    private void ShowLosePanel()
    {
        losePanelObject.SetActive(true);
    }

    private void ShowWinPanel()
    {
        winPanelObject.SetActive(true);
    }

    private void ShowStartScreen()
    {
        menuObject.SetActive(true);
        movesObject.SetActive(false);
        losePanelObject.SetActive(false);
        winPanelObject.SetActive(false);
    }
    
    private void ShowGameplayScreen()
    {
        
        menuObject.SetActive(false);
        movesObject.SetActive(true);
        losePanelObject.SetActive(false);
        winPanelObject.SetActive(false);
    }
}
