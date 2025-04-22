using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class BalloonSpawner : MonoBehaviour
{
    [SerializeField] private GameObject balloonPrefab;

    public Action OnBalloonPopped;

    private BalloonController _currentBalloon;
    private Camera _cam;
    private List<BalloonController> _balloons = new List<BalloonController>();
    
    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleInput(Input.GetMouseButton(0), Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), Input.mousePosition);
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            HandleInput(true, touch.phase == TouchPhase.Began, touch.phase == TouchPhase.Ended, touch.position);
        }
#endif
    }

    private void HandleInput(bool isHeld, bool justPressed, bool justReleased, Vector2 screenPosition)
    {
        var worldPos = _cam.ScreenToWorldPoint(screenPosition);
        worldPos.z = 0;

        if (justPressed)
        {
            if (!GameManager.Instance.CanCreateBalloon())
            {
                GameManager.Instance.CheckForLose();
                return;
            }
            GameManager.Instance.RegisterBalloon();
            var obj = Instantiate(balloonPrefab, worldPos, Quaternion.identity);
            _currentBalloon = obj.GetComponent<BalloonController>();
            _currentBalloon.SetColor(new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value));
            _currentBalloon.Popped += BalloonPopped;
            _balloons.Add(_currentBalloon);
        }

        if (isHeld && _currentBalloon != null)
        {
            _currentBalloon.MoveTo(worldPos);
            _currentBalloon.Inflate();
        }

        if (justReleased && _currentBalloon != null)
        {
            _currentBalloon.Launch();
            _currentBalloon = null;
        }
    }

    private void BalloonPopped(BalloonController balloon)
    {
        if (_balloons.Contains(balloon))
        {
            _balloons.Remove(balloon);
        }

        OnBalloonPopped?.Invoke();
    }

    public void ResetActiveBalloons()
    {
        foreach (var balloon in _balloons)
        {
            balloon.ResetLink();
        }
    }

    public void ClearBalloons()
    {
        foreach (var activeBalloon in _balloons)
        {
            Destroy(activeBalloon.gameObject);
        }
        _balloons.Clear();
    }
}