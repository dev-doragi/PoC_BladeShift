using UnityEngine;

public class WeaponSensor : MonoBehaviour
{
    private Transform _playerTransform;
    private Camera _mainCamera;
    private float _controlRadius;
    private float _mouseCaptureRadius;
    private float _mouseCaptureMaintainRadius;
    private float _breakMouseSpeed;

    public void Configure(Transform playerTransform, Camera mainCamera, float controlRadius, float mouseCaptureRadius, float mouseCaptureMaintainRadius, float breakMouseSpeed)
    {
        _playerTransform = playerTransform;
        _mainCamera = mainCamera != null ? mainCamera : Camera.main;
        _controlRadius = controlRadius;
        _mouseCaptureRadius = mouseCaptureRadius;
        _mouseCaptureMaintainRadius = mouseCaptureMaintainRadius > 0f ? mouseCaptureMaintainRadius : (_mouseCaptureRadius * 1.5f);
        _breakMouseSpeed = breakMouseSpeed;
    }

    public Vector2 GetMouseWorldPosition()
    {
        if (_mainCamera == null || InputReader.Instance == null) return Vector2.zero;
        return _mainCamera.ScreenToWorldPoint(InputReader.Instance.GetMousePosition());
    }

    public bool IsPlayerInRange(Vector3 weaponPosition)
    {
        if (_playerTransform == null) return false;
        return Vector2.Distance(weaponPosition, _playerTransform.position) <= _controlRadius;
    }

    public bool IsMouseInRange(Vector2 mousePos)
    {
        if (_playerTransform == null) return false;
        return Vector2.Distance(mousePos, _playerTransform.position) <= _controlRadius;
    }

    public bool IsMouseHovering(Vector3 weaponPosition, Vector2 mousePos)
    {
        return Vector2.Distance(weaponPosition, mousePos) <= _mouseCaptureRadius;
    }

    public bool IsMouseMaintainingControl(Vector3 weaponPosition, Vector2 mousePos)
    {
        return Vector2.Distance(weaponPosition, mousePos) <= _mouseCaptureMaintainRadius;
    }

    public bool ShouldAcquireControl(Vector3 weaponPosition, Vector2 mousePos)
    {
        return IsPlayerInRange(weaponPosition) && IsMouseInRange(mousePos);
    }

    public Transform GetPlayerTransform()
    {
        return _playerTransform;
    }

    public bool ShouldReleaseControl(Vector3 weaponPosition, Vector2 mousePos)
    {
        return !ShouldAcquireControl(weaponPosition, mousePos);
    }
}
