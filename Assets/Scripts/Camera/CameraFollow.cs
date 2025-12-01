using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform for the player or object the camera should follow.")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Offset from the target position.")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Movement")]
    [Tooltip("How quickly the camera moves to follow the target.")]
    public float smoothTime = 0.15f;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothTime);
    }
}