using UnityEngine;

public class WeaponMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Vector2 _currentVelocity;
    private const float DefaultFollowSmoothTime = 0.1f;
    private const float DefaultFollowMaxSpeed = 100f;

    public void CacheRigidbody(Rigidbody2D rb)
    {
        _rb = rb;
    }

    public void FollowMouseHover(Vector2 targetWorldPos, float smoothTime)
    {
        if (_rb == null) return;

        float useSmoothTime = smoothTime > 0f ? smoothTime : DefaultFollowSmoothTime;
        Vector2 newPos = Vector2.SmoothDamp(_rb.position, targetWorldPos, ref _currentVelocity, useSmoothTime, DefaultFollowMaxSpeed, Time.deltaTime);
        _rb.MovePosition(newPos);
    }

    public void StopFollow()
    {
        _currentVelocity = Vector2.zero;
    }

    public void MoveThrust(Vector2 moveStep)
    {
        if (_rb == null) return;

        _rb.MovePosition(_rb.position + moveStep);
        float deltaTime = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;
        if (deltaTime > 0f)
        {
            _currentVelocity = moveStep / deltaTime;
        }
    }

    public void TransferVelocityToPhysics()
    {
        if (_rb == null) return;

        _rb.linearVelocity = _currentVelocity;
    }
}
