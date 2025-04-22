using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        MainMenu,
        Gameplay,
        GameWin,
        GameLose
    }
    
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BalloonSpawner balloonSpawner;
    [SerializeField] private TextAsset[] levels;
    [SerializeField] private LevelLoader loader;
    [SerializeField] private float camAnimationTime = 3f;
    
    private int _balloonLimit;
    private int _balloonsUsed;
    private int _currentLevelNumber = 1;
    private Level _currentLevel;
    private int _currentStageNumber;
    private GameState _gameState = GameState.MainMenu;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        balloonSpawner.OnBalloonPopped += BalloonPop;
        balloonSpawner.enabled = false;
        _currentLevelNumber = PlayerPrefs.GetInt("Level", 1);
        uiManager.LosePanelButtonPressed += NextLevelTransition;
        uiManager.WinPanelButtonPressed += NextLevelTransition;
        uiManager.StartButtonPressed += NextLevelTransition;
        uiManager.SetStartText($"Play Level {_currentLevelNumber}");
        uiManager.SetUIState(_gameState);
    }

    private void StartStage(Stage stage)
    {
        stage.wallBottomObject.SetActive(true);
        stage.ChainBroke += ChainBreak;
        foreach (var hazard in stage.hazards)
        {
            hazard.StartMovement();
        }
        balloonSpawner.enabled = true;
    }

    private IEnumerator StartLevelPresentation(Level level, Action onComplete = null)
    {
        var cam = Camera.main;
        var initialCamY = level.stages[^1].stageObject.transform.position.y;
        cam.transform.position = new Vector3(cam.transform.position.x, initialCamY, cam.transform.position.z);
        cam.orthographicSize = level.stages[^1].camSize;
        if (level.stages.Count == 1)
        {
            onComplete?.Invoke();
            yield break;
        }
        cam.transform.position = new Vector3(cam.transform.position.x, initialCamY, cam.transform.position.z);
        yield return new WaitForSeconds(1f);
        var startCamY = level.stages[0].stageObject.transform.position.y;
        var camSize = level.stages[0].camSize;
        yield return StartCoroutine(MoveAndScaleCam(cam, startCamY, camSize, camAnimationTime));
        onComplete?.Invoke();
    }

    private IEnumerator MoveAndScaleCam(Camera cam, float camY, float camSize, float time)
    {
        var startSize = cam.orthographicSize;
        var startPos = cam.transform.position.y;
        var pos = cam.transform.position;
        
        var elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time); // ensures t stays between 0 and 1
            // Easing In: slow start, fast end
            var easedT = t * t;
            pos.y = Mathf.Lerp(startPos, camY, easedT);
            cam.orthographicSize = Mathf.Lerp(startSize, camSize, easedT);
            cam.transform.position = pos;
            
            yield return null;
        }
        
        // Snap exactly to target at end
        var final = cam.transform.position;
        final.y = camY;
        cam.transform.position = final;
        cam.orthographicSize = camSize;
    }

    private void NextLevelTransition()
    {
        Time.timeScale = 1;
        balloonSpawner.ClearBalloons();
        balloonSpawner.enabled = false;
        if (_currentLevel != null)
        {
            foreach (var stage in _currentLevel.stages)
            {
                Destroy(stage.stageObject);
            }
            _currentLevel.stages.Clear();
        }

        if (_currentLevelNumber > levels.Length)
        {
            _currentLevelNumber = levels.Length;
        }
        
        _currentLevel = loader.LoadLevel(levels[_currentLevelNumber-1].text);
        _currentStageNumber = 1;
        SetBalloonLimit(_currentLevel.balloonLimit);
        _gameState = GameState.Gameplay;
        uiManager.SetUIState(_gameState);
        StartCoroutine(StartLevelPresentation(_currentLevel, () => StartStage(_currentLevel.stages[0])));
    }
    
    public void CheckForLose()
    {
        if (IsOutOfBalloons())
        {
            Lose();
        }
    }

    private void Win()
    {
        balloonSpawner.enabled = false;
        _currentLevelNumber++;
        if (_currentLevelNumber > levels.Length)
        {
            _currentLevelNumber = 1;
        }
        PlayerPrefs.SetInt("Level", _currentLevelNumber);
        _gameState = GameState.GameWin;
        uiManager.SetUIState(_gameState);
    }

    private void Lose()
    {
        _gameState = GameState.GameLose;
        uiManager.SetUIState(_gameState);
        Time.timeScale = 0;
    }
    
    private void BalloonPop()
    {
        CheckForLose();
    }
    
    private void ChainBreak()
    {
        SetStageComplete(_currentLevel.stages[_currentStageNumber - 1]);
        StartCoroutine(NextStageTransition());
    }

    private IEnumerator NextStageTransition()
    {
        var oldStage = _currentLevel.stages[_currentStageNumber - 1];
        _currentStageNumber++;
        if (_currentStageNumber > _currentLevel.stages.Count)
        {
            Win();
            yield break;
        }
        var newStage = _currentLevel.stages[_currentStageNumber - 1];
        var newCamY = newStage.stageObject.transform.position.y;
        var camSize = newStage.camSize;
        var wallBottomObj = newStage.wallBottomObject;
        wallBottomObj.SetActive(false);
        balloonSpawner.ResetActiveBalloons();
        yield return StartCoroutine(MoveAndScaleCam(Camera.main, newCamY, camSize, camAnimationTime/3f));
        oldStage.stageObject.SetActive(false);
        StartStage(newStage);
    }

    private void SetStageComplete(Stage stage)
    {
        foreach (var obstacle in stage.obstacles)
        {
            obstacle.gameObject.SetActive(false);
        }

        foreach (var hazard in stage.hazards)
        {
            hazard.Disable();
        }
    }

    private void SetBalloonLimit(int limit)
    {
        _balloonLimit = limit;
        uiManager.SetMovesText(limit.ToString());
        _balloonsUsed = 0;
    }

    public bool CanCreateBalloon()
    {
        return _balloonsUsed < _balloonLimit;
    }

    public void RegisterBalloon()
    {
        if (!CanCreateBalloon())
        {
            return;
        }
        
        _balloonsUsed++;
        uiManager.SetMovesText((_balloonLimit - _balloonsUsed).ToString());
    }

    private bool IsOutOfBalloons()
    {
        return _balloonsUsed >= _balloonLimit;
    }
}