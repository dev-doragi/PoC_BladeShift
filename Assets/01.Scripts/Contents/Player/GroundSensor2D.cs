using UnityEngine;

public class GroundSensor2D : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Vector2 _boxSize = new Vector2(0.8f, 0.2f);
    [SerializeField] private Vector2 _offset = new Vector2(0f, -0.5f);

    public bool IsGrounded { get; private set; }

    private void Update()
    {
        Vector2 origin = (Vector2)transform.position + _offset;
        Collider2D hit = Physics2D.OverlapBox(origin, _boxSize, 0f, _groundLayer);
        IsGrounded = hit != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + _offset, _boxSize);
    }
}
