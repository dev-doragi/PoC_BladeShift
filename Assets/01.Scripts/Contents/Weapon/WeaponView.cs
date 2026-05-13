using UnityEngine;

public class WeaponView : MonoBehaviour
{
    [SerializeField] private LineRenderer _trajectoryLine;
    [SerializeField] private LineRenderer _connectionLine;

    private Transform _playerTransform;
    private float _controlRadius;
    private float _mouseCaptureRadius;
    private float _mouseCaptureMaintainRadius;
    private float _slashRadius;

    public void Configure(Transform playerTransform, float controlRadius, float mouseCaptureRadius, float mouseCaptureMaintainRadius, float slashRadius)
    {
        _playerTransform = playerTransform;
        _controlRadius = controlRadius;
        _mouseCaptureRadius = mouseCaptureRadius;
        _mouseCaptureMaintainRadius = mouseCaptureMaintainRadius;
        _slashRadius = slashRadius;
    }

    public void SetRotationZ(float zAngle)
    {
        Vector3 euler = transform.eulerAngles;
        euler.z = zAngle;
        transform.eulerAngles = euler;
    }

    public void ShowTrajectory(Vector3 start, Vector3 end)
    {
        if (_trajectoryLine == null) return;

        Vector3 direction = end - start;
        float maxLength = Mathf.Max(0f, _controlRadius);
        Vector3 clampedEnd = start + Vector3.ClampMagnitude(direction, maxLength);

        _trajectoryLine.enabled = true;
        _trajectoryLine.positionCount = 2;
        _trajectoryLine.SetPosition(0, start);
        _trajectoryLine.SetPosition(1, clampedEnd);
    }

    public void HideTrajectory()
    {
        if (_trajectoryLine != null)
        {
            _trajectoryLine.enabled = false;
        }
    }

    public void RenderConnectionLine(Vector3 playerPos, Vector3 weaponPos, bool isVisible)
    {
        if (_connectionLine == null) return;

        if (!isVisible)
        {
            _connectionLine.enabled = false;
            return;
        }

        _connectionLine.enabled = true;
        _connectionLine.positionCount = 2;
        _connectionLine.SetPosition(0, playerPos);
        _connectionLine.SetPosition(1, weaponPos);
    }

    public void DrawGizmos()
    {
        if (_playerTransform == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(_playerTransform.position, _controlRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _mouseCaptureRadius);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _mouseCaptureMaintainRadius);

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.35f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_slashRadius, _slashRadius * 0.57f, 0.01f));
        Gizmos.matrix = Matrix4x4.identity;
    }
}
