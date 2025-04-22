using System;
using TMPro;
using UnityEngine;

public class Chain : MonoBehaviour
{
    [SerializeField] private TMP_Text numberText;
    public bool IsBroken { get; private set; }
    public Action ChainBroke;
    
    private int _requiredNumber = 20;
    
    private void Start()
    {
        UpdateText();
    }

    public void SetRequiredNumber(int number)
    {
        _requiredNumber = number;
    }

    public void Subtract(int value)
    {
        if (IsBroken) return;

        _requiredNumber -= value;
        UpdateText();

        if (_requiredNumber <= 0)
        {
            Break();
        }
    }

    private void Break()
    {
        IsBroken = true;
        ChainBroke?.Invoke();
        Destroy(gameObject);
    }

    private void UpdateText()
    {
        if (numberText != null)
        {
            numberText.text = Mathf.Max(0, _requiredNumber).ToString();
        }
    }
}