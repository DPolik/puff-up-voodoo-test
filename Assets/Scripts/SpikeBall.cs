using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SpikeBall : MonoBehaviour, IHazard
{
    [SerializeField] private Transform warningBalloon; 
    [SerializeField] private float stuckRadius = 0.1f;
    [SerializeField] private float stuckDurationToExplode = 4f;
    [SerializeField] private float speed = 5f;
    
    private Vector2 _moveDirection;
    private Rigidbody2D _rb;
    private Vector2 _lastPosition;
    private float _stuckTimer = 0f;
    private Vector3 _warningInitialScale;
    private bool _isActive = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (warningBalloon != null)
        {
            _warningInitialScale = warningBalloon.localScale;
            warningBalloon.localScale = Vector3.zero;
        }
    }

    public void Initialize(float speed)
    {
        this.speed = speed;
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void StartMovement()
    {
        _moveDirection = Random.insideUnitCircle.normalized;
        _lastPosition = _rb.position;
        _isActive = true;
    }

    private void FixedUpdate()
    {
        if (!_isActive)
        {
            return;
        }
        _rb.velocity = _moveDirection * speed;

        HandleStuckCheck();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isActive)
        {
            return;
        }
        
        if (collision.gameObject.CompareTag("Balloon"))
        {
            var balloon = collision.gameObject.GetComponent<BalloonController>();
            if (balloon != null && !balloon.IsLaunched)
            {
                balloon.Pop();
                return;
            }
        }
        
        if (collision.contacts.Length > 0)
        {
            var normal = collision.contacts[0].normal;
            _moveDirection = Vector2.Reflect(_moveDirection, normal).normalized;
        }
    }

    private void HandleStuckCheck()
    {
        var distance = Vector2.Distance(_rb.position, _lastPosition);

        if (distance < stuckRadius)
        {
            _stuckTimer += Time.fixedDeltaTime;

            // Inflate warning balloon
            if (warningBalloon != null)
            {
                var inflateAmount = Mathf.Clamp01(_stuckTimer / stuckDurationToExplode);
                warningBalloon.localScale = Vector3.Lerp(Vector3.zero, _warningInitialScale, inflateAmount);
            }

            if (_stuckTimer >= stuckDurationToExplode)
            {
                Disable();
            }
        }
        else
        {
            _stuckTimer -= Time.fixedDeltaTime;
            if (_stuckTimer < 0)
            {
                _stuckTimer = 0;
            }
            
            _lastPosition = _rb.position;

            // Deflate the warning balloon
            if (warningBalloon != null && warningBalloon.localScale != Vector3.zero)
            {
                var inflateAmount = Mathf.Clamp01(_stuckTimer / stuckDurationToExplode);
                warningBalloon.localScale = Vector3.Lerp(Vector3.zero, _warningInitialScale, inflateAmount);
            }
        }
    }
}
