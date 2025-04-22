using System;
using TMPro;
using UnityEngine;

public class BalloonController : MonoBehaviour
{
    [SerializeField] private float launchForce = 10f;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private GameObject popEffect;
    [SerializeField] private float growthRate = 10f;     // Units per second
    [SerializeField] private float probeOffset = 0.1f;    // How far beyond the edge to check for obstacles

    public bool IsLaunched => _launched;
    public bool IsLinked => _linked;

    public Action<BalloonController> Popped;

    private Rigidbody2D _rb;
    private float _currentNumber = 1f;
    private bool _popped;
    private Chain _linkedChain;
    private bool _launched;
    private bool _linked;
    private float _currentSize;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        UpdateNumberText();
        _currentSize = transform.localScale.x;
    }

    public void SetColor(Color color)
    {
        GetComponent<SpriteRenderer>().color = color;
    }
    
    public void Inflate()
    {
        TrySmartGrow();
        UpdateNumberText();
    }

    void Update()
    {
        if (_launched)
        {
            _rb.velocity = Vector2.up * launchForce;
        }
    }

    public void Launch()
    {
        _launched = true;
        _rb.isKinematic = false;
        _rb.velocity = Vector2.up * launchForce;
        _rb.gravityScale = 0;
    }

    private void UpdateNumberText()
    {
        numberText.text = Mathf.FloorToInt(_currentNumber).ToString();
    }

    private int GetCurrentNumber()
    {
        return Mathf.FloorToInt(_currentNumber);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Chain"))
        {
            TryLink(collision.collider);
        }
    }

    public void Pop()
    {
        if (_popped)
        {
            return;
        }
        Popped?.Invoke(this);
        Instantiate(popEffect, transform.position, Quaternion.identity);
        _popped = true;
        Destroy(gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Spike"))
        {
            Pop();
        }
        else if (collision.collider.CompareTag("Chain") || collision.collider.CompareTag("Balloon"))
        {
            TryLink(collision.collider);
        }
    }

    private void TryLink(Collider2D other)
    {
        if (_linked || !_launched) return;

        // Directly touched a chain
        var chain = other.GetComponent<Chain>();
        if (chain == null)
        {
            chain = other.GetComponentInParent<Chain>();
        }
        if (chain != null && !chain.IsBroken)
        {
            LinkToChain(chain);
            return;
        }

        // Touched another balloon
        BalloonController otherBalloon = other.GetComponent<BalloonController>();
        if (otherBalloon != null && otherBalloon.IsLinked && otherBalloon._linkedChain != null && !otherBalloon._linkedChain.IsBroken)
        {
            LinkToChain(otherBalloon._linkedChain);
        }
    }

    private void LinkToChain(Chain chain)
    {
        _linked = true;
        _linkedChain = chain;

        chain.Subtract(GetCurrentNumber());
    }

    public void MoveTo(Vector3 worldPos)
    {
        _rb.MovePosition(worldPos);
    }
    
    private void TrySmartGrow()
    {
        var directions = new Vector2[]
        {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1,1).normalized, new Vector2(1,-1).normalized,
            new Vector2(-1,1).normalized, new Vector2(-1,-1).normalized
        };

        Vector2 center = transform.position;
        var freeCount = 0;

        foreach (var dir in directions)
        {
            var probePos = center + dir * (probeOffset);
            var delta = growthRate * Time.deltaTime;
            var nextRadius = (_currentSize + delta) / 2f;

            var hits = Physics2D.OverlapCircleAll(probePos, nextRadius);
            
            if (hits == null || hits.Length == 1 && hits[0].gameObject == gameObject)
            {
                freeCount++;
            }
        }
        
        if (freeCount > 0)
        {
            // Grow slightly in that direction
            var delta = growthRate * Time.deltaTime;
            _currentSize += delta;
            _currentNumber = _currentSize*5;
            transform.localScale = new Vector3(_currentSize, _currentSize, 1f);
        }
    }

    public void ResetLink()
    {
        _linkedChain = null;
        _linked = false;
    }
}
