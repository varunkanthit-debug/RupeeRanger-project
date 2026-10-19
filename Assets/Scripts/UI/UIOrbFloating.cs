using UnityEngine;

namespace RupeeRanger.UI
{
    public class UIOrbFloating : MonoBehaviour
    {
        [Header("Floating Movement")]
        public float moveSpeed = 0.8f;
        public float moveDistanceX = 18f;
        public float moveDistanceY = 24f;

        [Header("Pulsing Scale")]
        public float pulseSpeed = 1.2f;
        public float minScale = 0.92f;
        public float maxScale = 1.08f;

        private Vector3 initialPosition;
        private Vector3 initialScale;
        private float randomOffset;

        private void Start()
        {
            initialPosition = transform.localPosition;
            initialScale = transform.localScale;
            randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float time = Time.time * moveSpeed + randomOffset;
            float offsetX = Mathf.Sin(time) * moveDistanceX;
            float offsetY = Mathf.Cos(time * 0.85f) * moveDistanceY;

            transform.localPosition = initialPosition + new Vector3(offsetX, offsetY, 0f);

            float scalePulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed + randomOffset) + 1f) * 0.5f);
            transform.localScale = initialScale * scalePulse;
        }
    }
}
