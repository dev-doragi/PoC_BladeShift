using System;
using System.Collections;
using UnityEngine;

public class WeaponMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Vector2 _currentVelocity;
    private const float DefaultFollowSmoothTime = 0.1f;
    private const float DefaultFollowMaxSpeed = 100f;
    [SerializeField] private float _weaponRadius = 0.3f;
    [SerializeField] private float _skinWidth = 0.05f;

    public float WeaponRadius => _weaponRadius;

    public void CacheRigidbody(Rigidbody2D rb)
    {
        _rb = rb;
    }

    public void FollowMouseHover(Vector2 targetWorldPos, float smoothTime, LayerMask wallMask)
    {
        if (_rb == null) return;

        Vector2 currentPos = _rb.position;
        float useSmoothTime = smoothTime > 0f ? smoothTime : DefaultFollowSmoothTime;
        Vector2 idealNextPos = Vector2.SmoothDamp(currentPos, targetWorldPos, ref _currentVelocity, useSmoothTime, DefaultFollowMaxSpeed, Time.fixedDeltaTime);
        Vector2 frameMove = idealNextPos - currentPos;
        float moveDist = frameMove.magnitude;

        if (moveDist <= 0.0001f)
        {
            _rb.MovePosition(idealNextPos);
            return;
        }

        Vector2 moveDir = frameMove / moveDist;
        RaycastHit2D hit = Physics2D.CircleCast(currentPos, _weaponRadius, moveDir, moveDist, wallMask);

        if (hit.collider != null && hit.distance > 0.001f)
        {
            Vector2 safePos = hit.centroid + (hit.normal * _skinWidth);
            Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
            Vector2 remainingMove = idealNextPos - safePos;
            Vector2 slideMove = tangent * Vector2.Dot(remainingMove, tangent);
            _rb.MovePosition(safePos + slideMove);
            return;
        }

        _rb.MovePosition(idealNextPos);
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

    public void ExecuteThrust(Vector2 direction, float speed, LayerMask wallMask, Func<Vector2, bool> isOutOfRange, Action onThrustEnd)
    {
        StartCoroutine(ThrustRoutine(direction, speed, wallMask, isOutOfRange, onThrustEnd));
    }

    public void ExecuteReturn(Func<Vector2> getTargetPos, float minSpeed, float maxSpeed, float slowRadius, float stopDistance, LayerMask wallMask, Func<Vector2, Vector2, bool> checkIntercept, Action onReturnComplete)
    {
        StartCoroutine(ReturnRoutine(getTargetPos, minSpeed, maxSpeed, slowRadius, stopDistance, wallMask, checkIntercept, onReturnComplete));
    }

    private IEnumerator ThrustRoutine(Vector2 direction, float speed, LayerMask wallMask, Func<Vector2, bool> isOutOfRange, Action onThrustEnd)
    {
        if (_rb == null) yield break;

        Vector2 thrustDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        int wallLayerMask = wallMask.value;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            Vector2 currentPos = _rb.position;

            if (isOutOfRange != null && isOutOfRange(currentPos))
            {
                break;
            }

            float moveDistance = speed * Time.fixedDeltaTime;
            if (moveDistance <= 0f)
            {
                break;
            }

            RaycastHit2D hit = Physics2D.CircleCast(currentPos, _weaponRadius, thrustDirection, moveDistance, wallLayerMask);
            if (hit.collider != null && hit.distance > 0.001f)
            {
                _rb.MovePosition(hit.centroid + (hit.normal * _skinWidth));
                break;
            }

            MoveThrust(thrustDirection * moveDistance);
        }

        onThrustEnd?.Invoke();
    }

    private IEnumerator ReturnRoutine(Func<Vector2> getTargetPos, float minSpeed, float maxSpeed, float slowRadius, float stopDistance, LayerMask wallMask, Func<Vector2, Vector2, bool> checkIntercept, Action onReturnComplete)
    {
        if (_rb == null)
        {
            onReturnComplete?.Invoke();
            yield break;
        }

        int wallLayerMask = wallMask.value;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            Vector2 currentPos = _rb.position;
            Vector2 targetPos = getTargetPos != null ? getTargetPos() : currentPos;
            Vector2 toTarget = targetPos - currentPos;
            float distance = toTarget.magnitude;

            if (distance <= stopDistance || (checkIntercept != null && checkIntercept(currentPos, targetPos)))
            {
                break;
            }

            float speedT = Mathf.Clamp01(distance / Mathf.Max(0.01f, slowRadius));
            float currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, speedT);
            Vector2 moveDirection = distance > 0.0001f ? toTarget / distance : Vector2.zero;
            float moveDistance = currentSpeed * Time.fixedDeltaTime;

            if (moveDistance <= 0f)
            {
                break;
            }

            RaycastHit2D hit = Physics2D.CircleCast(currentPos, _weaponRadius, moveDirection, moveDistance, wallLayerMask);
            if (hit.collider != null && hit.distance > 0.001f)
            {
                _rb.MovePosition(hit.centroid + (hit.normal * _skinWidth));
                break;
            }

            MoveThrust(moveDirection * moveDistance);
        }

        onReturnComplete?.Invoke();
    }

    public void TransferVelocityToPhysics()
    {
        if (_rb == null) return;

        _rb.linearVelocity = _currentVelocity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _weaponRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * _weaponRadius);
    }
}