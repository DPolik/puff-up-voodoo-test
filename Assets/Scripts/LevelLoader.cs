using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject spikeBallPrefab;
    [SerializeField] private GameObject wallBottomPrefab;

    [SerializeField] private float levelHeight = 10;
    [SerializeField] private float scaleFactor = 3;

    [SerializeField] private float referenceHeight = 1080;
    [SerializeField] private float referenceOrthoSize = 5f;

    private float baseOrthoSize = 5;

    void Start()
    {
        if (Screen.height <= referenceHeight)
        {
            return;
        }
        
        float pixelsPerUnit = referenceHeight / (2f * referenceOrthoSize);
        baseOrthoSize = Screen.height / (2f * pixelsPerUnit);
        
        Camera.main.orthographicSize = baseOrthoSize;
    }
    
    public Level LoadLevel(string json)
    {
        var levelData = JsonUtility.FromJson<LevelData>(json);
        var level = new Level();
        level.balloonLimit = levelData.balloonLimit;

        var lastPosY = 0f;
        var lastScale = 1f;
        var lastCamSize = baseOrthoSize;
        
        var levelColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

        for (var i = 0; i < levelData.stages.Length; i++)
        {
            var stageData = levelData.stages[i];
            var stageObject = Instantiate(stagePrefab, transform);
            var stage = new Stage();
            stage.stageObject = stageObject;
            
            foreach (var o in stageData.obstacles)
            {
                var pos = o.position.ToVector2();
                var size = o.size.ToVector2();
                var obs = Instantiate(obstaclePrefab, stageObject.transform);
                obs.transform.position = pos;
                obs.transform.localScale = size;
                stage.obstacles.Add(obs);
            }

            foreach (var h in stageData.hazards)
            {
                var spikeObj = Instantiate(spikeBallPrefab, stageObject.transform);
                spikeObj.transform.position = h.position.ToVector2();
                var hazard = spikeObj.GetComponent<IHazard>();
                hazard.Initialize(h.speed);
                stage.hazards.Add(hazard);
            }
            
            level.stages.Add(stage);
            var chain = stageObject.GetComponentInChildren<Chain>();
            chain.SetRequiredNumber(stageData.chainValue);
            chain.ChainBroke = () => stage.ChainBroke?.Invoke();
            
            var wallBottom = Instantiate(wallBottomPrefab, stageObject.transform);
            stage.wallBottomObject = wallBottom;
            wallBottom.GetComponent<SpriteRenderer>().color = levelColor;
            stage.wallBottomObject.SetActive(false);

            foreach (Transform child in stageObject.transform)
            {
                if (child.CompareTag("Wall"))
                {
                    child.GetComponent<SpriteRenderer>().color = levelColor;
                }
            }
            
            if (i == 0)
            {
                stage.camSize = baseOrthoSize;
                stage.wallBottomObject.SetActive(true);
                continue;
            }
            
            var curScale = lastScale * scaleFactor;
            var stagePosY = .5f * (levelHeight * lastScale) + .5f * (levelHeight * curScale) + lastPosY;
            stageObject.transform.position = new Vector3(0, stagePosY, 0);
            stageObject.transform.localScale = new Vector3(curScale, curScale, 1);
            lastScale = curScale;
            lastPosY = stagePosY;
            stage.camSize = lastCamSize * (scaleFactor);
            lastCamSize = stage.camSize;
            
        }

        return level;
    }
}


[System.Serializable]
public class LevelData
{
    public int balloonLimit;
    public StageData[] stages;
}

[System.Serializable]
public class StageData
{
    public int chainValue;
    public ObstacleData[] obstacles = Array.Empty<ObstacleData>();
    public HazardData[] hazards = Array.Empty<HazardData>();
    public float camSize = 5;
}

[System.Serializable]
public class Vector2Data
{
    public float x, y;
    public Vector2 ToVector2() => new Vector2(x, y);
}

[System.Serializable]
public class ObstacleData
{
    public Vector2Data position;
    public Vector2Data size;
}

[System.Serializable]
public class HazardData
{
    public Vector2Data position;
    public Vector2Data direction;
    public float speed;
}

public class Level
{
    public int balloonLimit;
    public List<Stage> stages = new List<Stage>();
}

public class Stage
{
    public GameObject stageObject;
    public GameObject wallBottomObject;
    public Action ChainBroke;
    public List<GameObject> obstacles = new List<GameObject>();
    public List<IHazard> hazards = new List<IHazard>();
    public float camSize = 5;
}