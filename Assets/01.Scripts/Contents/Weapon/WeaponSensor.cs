using UnityEngine;

public class WeaponSensor : MonoBehaviour
{
    private const float LineOfSightMargin = 0.35f;
    private Transform _playerTransform;
    private Camera _mainCamera;
    private float _controlRadius;
    private float _mouseCaptureRadius;
    private float _mouseCaptureMaintainRadius;
    private Vector2 _lastValidMousePos;

    public void Configure(Transform playerTransform, Camera mainCamera, float controlRadius, float mouseCaptureRadius, float mouseCaptureMaintainRadius, float breakMouseSpeed)
    {
        _playerTransform = playerTransform;
        _mainCamera = mainCamera != null ? mainCamera : Camera.main;
        _controlRadius = controlRadius;
        _mouseCaptureRadius = mouseCaptureRadius;
        _mouseCaptureMaintainRadius = mouseCaptureMaintainRadius > 0f ? mouseCaptureMaintainRadius : (_mouseCaptureRadius * 1.5f);
        _lastValidMousePos = _playerTransform != null ? (Vector2)_playerTransform.position : Vector2.zero;
    }

    public Vector2 GetMouseWorldPosition()
    {
        if (_mainCamera == null || InputReader.Instance == null) return _lastValidMousePos;

        Vector2 screenPos = InputReader.Instance.GetMousePosition();
        if (screenPos.x < 0f || screenPos.y < 0f || screenPos.x > Screen.width || screenPos.y > Screen.height)
        {
            return _lastValidMousePos;
        }

        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
        _lastValidMousePos = worldPos;
        return worldPos;
    }

    public bool IsPlayerInRange(Vector3 weaponPosition)
    {
        if (_playerTransform == null) return false;
        return Vector2.Distance(weaponPosition, _playerTransform.position) <= _controlRadius;
    }

    public bool HasLineOfSight(Vector3 weaponPosition, LayerMask wallMask)
    {
        if (_playerTransform == null) return false;

        Vector2 start = _playerTransform.position;
        Vector2 end = weaponPosition;
        Vector2 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f) return true;

        float checkDistance = Mathf.Max(0f, distance - LineOfSightMargin);
        if (checkDistance <= 0f) return true;

        RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, checkDistance, wallMask);
        return hit.collider == null;
    }

    public Vector2 GetClampedTargetPosition(LayerMask wallMask)
    {
        if (_playerTransform == null) return GetMouseWorldPosition();

        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 start = _playerTransform.position;
        Vector2 direction = mousePos - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f) return mousePos;

        if (distance > _controlRadius)
        {
            direction = direction.normalized * _controlRadius;
            distance = _controlRadius;
        }

        RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, distance, wallMask);
        return hit.collider != null ? hit.point : mousePos;
    }

    public bool IsMouseInRange(Vector2 mousePos, bool alreadyControlled)
    {
        if (_playerTransform == null) return false;

        float threshold = alreadyControlled ? _controlRadius * 1.2f : _controlRadius;
        return Vector2.Distance(mousePos, _playerTransform.position) <= threshold;
    }

    public bool IsMouseHovering(Vector3 weaponPosition, Vector2 mousePos)
    {
        return Vector2.Distance(weaponPosition, mousePos) <= _mouseCaptureRadius;
    }

    public bool IsMouseMaintainingControl(Vector3 weaponPosition, Vector2 mousePos)
    {
        return Vector2.Distance(weaponPosition, mousePos) <= _mouseCaptureMaintainRadius;
    }

    public bool ShouldAcquireControl(Vector3 weaponPosition, Vector2 mousePos, LayerMask wallMask)
    {
        return IsPlayerInRange(weaponPosition) && IsMouseInRange(mousePos, false) && HasLineOfSight(weaponPosition, wallMask);
    }

    public Transform GetPlayerTransform()
    {
        return _playerTransform;
    }

    public bool ShouldReleaseControl(Vector3 weaponPosition, Vector2 mousePos, LayerMask wallMask)
    {
        return !IsPlayerInRange(weaponPosition) || !IsMouseInRange(mousePos, true) || !HasLineOfSight(weaponPosition, wallMask);
    }
}