using UnityEngine;

public class WeaponMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Vector2 _currentVelocity;

    public void CacheRigidbody(Rigidbody2D rb)
    {
        _rb = rb;
    }

    public void FollowMouseHover(Vector2 targetWorldPos, float smoothTime)
    {
        if (_rb == null) return;

        Vector2 newPos = Vector2.SmoothDamp(_rb.position, targetWorldPos, ref _currentVelocity, smoothTime);
        _rb.MovePosition(newPos);
    }

    public void MoveThrust(Vector2 moveStep)
    {
        if (_rb == null) return;

        _rb.MovePosition(_rb.position + moveStep);
        if (Time.deltaTime > 0f)
        {
            _currentVelocity = moveStep / Time.deltaTime;
        }
    }

    public void TransferVelocityToPhysics()
    {
        if (_rb == null) return;

        _rb.linearVelocity = _currentVelocity;
    }
}
