using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RupeeRanger.Town
{
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public static VirtualJoystick Instance { get; private set; }

        [Header("UI Components")]
        public RectTransform background;
        public RectTransform handle;

        [Header("Settings")]
        [SerializeField] private float handleLimit = 40f;

        public Vector2 InputVector { get; private set; } = Vector2.zero;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out position))
            {
                position.x = (position.x / background.sizeDelta.x);
                position.y = (position.y / background.sizeDelta.y);

                Vector2 input = new Vector2(position.x * 2f, position.y * 2f);
                InputVector = (input.magnitude > 1f) ? input.normalized : input;

                if (handle != null)
                {
                    handle.anchoredPosition = new Vector2(
                        InputVector.x * (background.sizeDelta.x / 2f) * (handleLimit / 50f),
                        InputVector.y * (background.sizeDelta.y / 2f) * (handleLimit / 50f)
                    );
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            InputVector = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
