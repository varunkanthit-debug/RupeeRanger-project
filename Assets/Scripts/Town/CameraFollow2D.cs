using UnityEngine;

namespace RupeeRanger.Town
{
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target & Speed")]
        public Transform target;
        public float smoothSpeed = 5.0f;
        public Vector2 offset = Vector2.zero;

        [Header("Boundary Limits")]
        public bool useBounds = true;
        public Vector2 minBounds = new Vector2(-22f, -16f);
        public Vector2 maxBounds = new Vector2(22f, 16f);

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z
            );

            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            }

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
