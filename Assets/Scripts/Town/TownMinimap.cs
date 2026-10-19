using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RupeeRanger.Town
{
    public class TownMinimap : MonoBehaviour
    {
        [Header("Map References")]
        public RectTransform mapArea;
        public RectTransform playerMarker;
        public Transform playerTransform;

        [Header("World Space Bounds")]
        public Vector2 worldMin = new Vector2(-24f, -18f);
        public Vector2 worldMax = new Vector2(24f, 18f);

        [Header("Building Markers")]
        [SerializeField] private List<RectTransform> buildingMarkers = new List<RectTransform>();

        private float blinkTimer = 0f;

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<PlayerController2D>();
                if (player != null) playerTransform = player.transform;
            }
        }

        private void LateUpdate()
        {
            UpdatePlayerMarker();
        }

        private void UpdatePlayerMarker()
        {
            if (playerTransform == null || playerMarker == null || mapArea == null) return;

            Vector2 normPos = WorldToNormalized(playerTransform.position);
            Vector2 markerPos = new Vector2(
                (normPos.x - 0.5f) * mapArea.rect.width,
                (normPos.y - 0.5f) * mapArea.rect.height
            );

            playerMarker.anchoredPosition = markerPos;

            // Subtle blink effect for player marker
            blinkTimer += Time.deltaTime * 5f;
            Image markerImg = playerMarker.GetComponent<Image>();
            if (markerImg != null)
            {
                float alpha = 0.6f + Mathf.PingPong(blinkTimer, 0.4f);
                Color c = markerImg.color;
                c.a = alpha;
                markerImg.color = c;
            }
        }

        public void RegisterBuildingMarker(Vector2 worldPos, Color color, string label)
        {
            if (mapArea == null) return;

            GameObject blipObj = new GameObject($"Blip_{label}", typeof(RectTransform), typeof(Image));
            blipObj.transform.SetParent(mapArea, false);

            RectTransform rt = blipObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(8f, 8f);

            Vector2 normPos = WorldToNormalized(worldPos);
            rt.anchoredPosition = new Vector2(
                (normPos.x - 0.5f) * mapArea.rect.width,
                (normPos.y - 0.5f) * mapArea.rect.height
            );

            Image img = blipObj.GetComponent<Image>();
            img.color = color;

            buildingMarkers.Add(rt);
        }

        private Vector2 WorldToNormalized(Vector2 worldPos)
        {
            float normX = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
            float normY = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.y);
            return new Vector2(Mathf.Clamp01(normX), Mathf.Clamp01(normY));
        }
    }
}
