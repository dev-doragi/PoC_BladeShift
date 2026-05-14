using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Vector2 _currentVelocity;
    private const float DefaultFollowSmoothTime = 0.1f;
    private const float DefaultFollowMaxSpeed = 100f;
    [SerializeField] private float _weaponRadius = 0.3f;
    [SerializeField] private float _skinWidth = 0.05f;
    [SerializeField] private float _followSmoothTime = 0.1f;
    [SerializeField] private float _spinFollowSmoothTime = 0.4f;
    [SerializeField] private float _minReturnSpeed = 10f;
    [SerializeField] private float _maxReturnSpeed = 30f;
    [SerializeField] private float _returnStopDistance = 0.5f;

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

    public void HandleHoverMovement(Vector2 targetPos, bool isSpinning, LayerMask wallMask)
    {
        float smoothTime = isSpinning ? _spinFollowSmoothTime : _followSmoothTime;
        FollowMouseHover(targetPos, smoothTime, wallMask);
    }

    public void ApplySpinRotation(float spinSpeed)
    {
        if (_rb == null) return;

        _rb.MoveRotation(_rb.rotation + (spinSpeed * Time.fixedDeltaTime));
    }

    public void ExecutePinFlight(Vector2 direction, float speed, LayerMask enemyMask, LayerMask wallMask, Func<Transform, bool> onCheckTarget, Action<Transform> onPinned)
    {
        StartCoroutine(PinFlightRoutine(direction, speed, enemyMask, wallMask, onCheckTarget, onPinned));
    }

    public void ExecuteOrbitFinisher(Vector2 pivot, float radius, float duration, Action onReleasePoint, Action onComplete)
    {
        StartCoroutine(OrbitRoutine(pivot, radius, duration, onReleasePoint, onComplete));
    }

    public void StopFollow()
    {
        _currentVelocity = Vector2.zero;
    }

    public void ExecuteReturn(Func<Vector2> getTargetPos, float controlRadius, Func<Vector2, Vector2, bool> checkIntercept, Action<bool> onReturnComplete)
    {
        StartCoroutine(ReturnRoutine(getTargetPos, _minReturnSpeed, _maxReturnSpeed, controlRadius, _returnStopDistance, checkIntercept, onReturnComplete));
    }

    private IEnumerator PinFlightRoutine(Vector2 direction, float speed, LayerMask enemyMask, LayerMask wallMask, Func<Transform, bool> onCheckTarget, Action<Transform> onPinned)
    {
        if (_rb == null) yield break;

        Vector2 flightDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        int enemyLayerMask = enemyMask.value;
        int wallLayerMask = wallMask.value;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            Vector2 currentPos = _rb.position;

            if (onCheckTarget != null)
            {
                Collider2D[] targets = Physics2D.OverlapCircleAll(currentPos, _weaponRadius, enemyLayerMask);
                foreach (Collider2D target in targets)
                {
                    if (target == null) continue;
                    if (onCheckTarget(target.transform))
                    {
                        yield break;
                    }
                }
            }

            float moveDistance = speed * Time.fixedDeltaTime;
            if (moveDistance <= 0f)
            {
                break;
            }

            RaycastHit2D wallHit = Physics2D.Raycast(currentPos, flightDirection, moveDistance, wallLayerMask);
            bool hasWallHit = wallHit.collider != null;
            float actualMoveDist = hasWallHit ? wallHit.distance : moveDistance;

            if (onCheckTarget != null)
            {
                var sweepTargets = new List<(Transform target, float distance)>();
                var uniqueTargets = new HashSet<Transform>();

                Collider2D[] overlapTargets = Physics2D.OverlapCircleAll(currentPos, _weaponRadius, enemyLayerMask);
                for (int i = 0; i < overlapTargets.Length; i++)
                {
                    Collider2D target = overlapTargets[i];
                    if (target == null) continue;

                    Transform targetTransform = target.transform;
                    if (!uniqueTargets.Add(targetTransform)) continue;
                    sweepTargets.Add((targetTransform, 0f));
                }

                if (actualMoveDist > 0.0001f)
                {
                    RaycastHit2D[] castHits = Physics2D.CircleCastAll(currentPos, _weaponRadius, flightDirection, actualMoveDist, enemyLayerMask);
                    for (int i = 0; i < castHits.Length; i++)
                    {
                        Collider2D hitCollider = castHits[i].collider;
                        if (hitCollider == null) continue;

                        Transform targetTransform = hitCollider.transform;
                        if (!uniqueTargets.Add(targetTransform)) continue;
                        sweepTargets.Add((targetTransform, castHits[i].distance));
                    }
                }

                if (sweepTargets.Count > 1)
                {
                    sweepTargets.Sort((a, b) => a.distance.CompareTo(b.distance));
                }

                for (int i = 0; i < sweepTargets.Count; i++)
                {
                    if (onCheckTarget(sweepTargets[i].target))
                    {
                        yield break;
                    }
                }
            }

            if (hasWallHit)
            {
                _rb.MovePosition(wallHit.point);
                onPinned?.Invoke(wallHit.transform);
                yield break;
            }

            _rb.MovePosition(currentPos + (flightDirection * moveDistance));
        }
    }

    private IEnumerator ReturnRoutine(Func<Vector2> getTargetPos, float minSpeed, float maxSpeed, float slowRadius, float stopDistance, Func<Vector2, Vector2, bool> checkIntercept, Action<bool> onReturnComplete)
    {
        if (_rb == null)
        {
            onReturnComplete?.Invoke(false);
            yield break;
        }

        while (true)
        {
            yield return new WaitForFixedUpdate();

            Vector2 currentPos = _rb.position;
            Vector2 targetPos = getTargetPos != null ? getTargetPos() : currentPos;
            Vector2 toTarget = targetPos - currentPos;
            float distance = toTarget.magnitude;

            if (distance <= stopDistance || (checkIntercept != null && checkIntercept(currentPos, targetPos)))
            {
                onReturnComplete?.Invoke(true);
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

            _rb.MovePosition(currentPos + (moveDirection * moveDistance));
        }

        onReturnComplete?.Invoke(true);
    }

    private IEnumerator OrbitRoutine(Vector2 pivot, float radius, float duration, Action onReleasePoint, Action onComplete)
    {
        if (_rb == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector2 startPos = _rb.position;
        float startAngle = Mathf.Atan2(startPos.y - pivot.y, startPos.x - pivot.x) * Mathf.Rad2Deg;
        float elapsed = 0f;
        bool hasReleased = false;

        while (elapsed < duration)
        {
            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float curve = 1f - Mathf.Pow(1f - t, 3f);
            float currentAngle = startAngle + (360f * curve);
            Vector2 offset = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * radius;

            _rb.MovePosition(pivot + offset);
            _rb.MoveRotation(currentAngle + 90f);

            if (!hasReleased && t > 0.3f)
            {
                hasReleased = true;
                onReleasePoint?.Invoke();
            }
        }

        if (!hasReleased)
        {
            onReleasePoint?.Invoke();
        }

        onComplete?.Invoke();
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
