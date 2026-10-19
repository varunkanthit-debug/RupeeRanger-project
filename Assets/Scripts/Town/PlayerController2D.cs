using System;
using System.Collections;
using UnityEngine;
using RupeeRanger.Core;

namespace RupeeRanger.Town
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 4.5f;

        [Header("References")]
        public SpriteRenderer spriteRenderer;
        public Rigidbody2D rb;

        [Header("Cartoon Walking Dynamics")]
        public float stepFrequency = 11.5f;
        public float hopHeight = 0.09f;
        public float tiltAngle = 6.5f;
        public float squashAmount = 0.06f;

        [Header("Current Interaction")]
        [SerializeField] private TownBuilding currentBuilding;
        public bool IsMoving => movementInput.sqrMagnitude > 0.01f || (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f);

        private Vector2 movementInput = Vector2.zero;
        private Transform visualTransform;
        private Transform shadowTransform;
        private Vector3 baseShadowScale = new Vector3(0.55f, 0.22f, 1f);
        private float walkTimer = 0f;
        private float lastStepSign = 0f;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            // Setup dedicated visual container so visual animation doesn't interfere with physics body
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            Sprite existingSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            int sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 10;
            Color spriteColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            // Create Visual child
            GameObject visualObj = new GameObject("Visual", typeof(SpriteRenderer));
            visualObj.transform.SetParent(transform, false);
            visualObj.transform.localPosition = Vector3.zero;
            visualTransform = visualObj.transform;

            SpriteRenderer newSr = visualObj.GetComponent<SpriteRenderer>();
            newSr.sprite = existingSprite;
            newSr.sortingOrder = sortingOrder;
            newSr.color = spriteColor;

            if (spriteRenderer != null && spriteRenderer.gameObject == gameObject)
            {
                // Disable the root SpriteRenderer component so only visual child renders
                spriteRenderer.enabled = false;
            }
            spriteRenderer = newSr;

            // Setup soft ground shadow beneath feet
            GameObject shadowObj = new GameObject("GroundShadow", typeof(SpriteRenderer));
            shadowObj.transform.SetParent(transform, false);
            shadowObj.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            shadowObj.transform.localScale = baseShadowScale;
            shadowTransform = shadowObj.transform;

            SpriteRenderer shadowSr = shadowObj.GetComponent<SpriteRenderer>();
            shadowSr.sprite = Resources.Load<Sprite>("rounded_box");
            if (shadowSr.sprite == null)
            {
                // Fallback to visual sprite tinted black/transparent if rounded_box not in Resources
                shadowSr.sprite = existingSprite;
            }
            shadowSr.color = new Color(0.08f, 0.12f, 0.18f, 0.42f);
            shadowSr.sortingOrder = sortingOrder - 1;
        }

        private void Start()
        {
            // Load character sprite from GameManager if available
            if (GameManager.Instance != null && GameManager.Instance.selectedCharacter != null && GameManager.Instance.selectedCharacter.sprite != null)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = GameManager.Instance.selectedCharacter.sprite;
                }
            }
        }

        private void Update()
        {
            ReadMovementInput();
            ReadInteractionInput();
            AnimateCartoonMovement();
        }

        private void FixedUpdate()
        {
            Vector2 targetVelocity = movementInput * moveSpeed;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = targetVelocity;
#else
            rb.velocity = targetVelocity;
#endif
        }

        private void ReadMovementInput()
        {
            if (TownHUD.Instance != null && TownHUD.Instance.IsAnyModalOpen)
            {
                movementInput = Vector2.zero;
                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif
                }
                return;
            }

            float x = 0f;
            float y = 0f;

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                x = Input.GetAxisRaw("Horizontal");
                y = Input.GetAxisRaw("Vertical");
            }
            catch {}
#endif

            // Combine with virtual joystick input if present
            if (VirtualJoystick.Instance != null && VirtualJoystick.Instance.InputVector.sqrMagnitude > 0.01f)
            {
                x += VirtualJoystick.Instance.InputVector.x;
                y += VirtualJoystick.Instance.InputVector.y;
            }

            movementInput = new Vector2(x, y);
            if (movementInput.sqrMagnitude > 1f)
            {
                movementInput.Normalize();
            }

            // Facing flip
            if (spriteRenderer != null)
            {
                if (movementInput.x > 0.05f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (movementInput.x < -0.05f)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }

        private void ReadInteractionInput()
        {
            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactPressed = true;
                }
            }
            catch {}
#endif

            if (interactPressed)
            {
                Interact();
            }
        }

        private void AnimateCartoonMovement()
        {
            if (visualTransform == null) return;

            bool isMoving = movementInput.sqrMagnitude > 0.05f;

            if (isMoving)
            {
                walkTimer += Time.deltaTime * stepFrequency;

                // 1. Vertical energetic step hop
                float hop = Mathf.Abs(Mathf.Sin(walkTimer)) * hopHeight;
                visualTransform.localPosition = new Vector3(0f, hop, 0f);

                // 2. Cheerful waddling side-to-side tilt
                float tilt = Mathf.Sin(walkTimer) * tiltAngle;
                // Add subtle forward momentum lean
                float forwardLean = movementInput.x * -2f;
                visualTransform.localRotation = Quaternion.Euler(0f, 0f, tilt + forwardLean);

                // 3. Cartoon squash & stretch (squash on foot plant, stretch on jump)
                float squash = Mathf.Cos(walkTimer * 2f) * squashAmount;
                visualTransform.localScale = new Vector3(1f - squash * 0.6f, 1f + squash, 1f);

                // 4. Ground shadow reacts to height (shrinks slightly when in air)
                if (shadowTransform != null)
                {
                    float shadowFactor = 1f - (hop / hopHeight) * 0.22f;
                    shadowTransform.localScale = new Vector3(baseShadowScale.x * shadowFactor, baseShadowScale.y * shadowFactor, 1f);
                }

                // 5. Footstep step detection for dust puff
                float currentSign = Mathf.Sign(Mathf.Sin(walkTimer));
                if (lastStepSign != 0f && currentSign != lastStepSign)
                {
                    SpawnFootstepDust();
                }
                lastStepSign = currentSign;
            }
            else
            {
                walkTimer = 0f;
                lastStepSign = 0f;

                // Idle: gentle happy breathing bob
                float breath = Mathf.Sin(Time.time * 3f) * 0.02f;
                visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, new Vector3(0f, breath, 0f), Time.deltaTime * 8f);
                visualTransform.localRotation = Quaternion.Lerp(visualTransform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, new Vector3(1f, 1f + breath * 0.5f, 1f), Time.deltaTime * 8f);

                if (shadowTransform != null)
                {
                    shadowTransform.localScale = Vector3.Lerp(shadowTransform.localScale, baseShadowScale, Time.deltaTime * 8f);
                }
            }
        }

        private void SpawnFootstepDust()
        {
            // Create a small subtle cartoon dust puff behind the foot
            GameObject dust = new GameObject("DustPuff", typeof(SpriteRenderer));
            Vector3 footPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), -0.38f, 0f);
            dust.transform.position = footPos;
            dust.transform.localScale = new Vector3(0.12f, 0.08f, 1f);

            SpriteRenderer dsr = dust.GetComponent<SpriteRenderer>();
            dsr.sprite = Resources.Load<Sprite>("soft_orb") ?? (spriteRenderer != null ? spriteRenderer.sprite : null);
            dsr.color = new Color(0.95f, 0.95f, 0.92f, 0.45f);
            dsr.sortingOrder = 8;

            StartCoroutine(AnimateDustRoutine(dust, dsr));
        }

        private IEnumerator AnimateDustRoutine(GameObject dust, SpriteRenderer dsr)
        {
            float elapsed = 0f;
            float duration = 0.22f;
            Vector3 startScale = dust.transform.localScale;
            Vector3 targetScale = startScale * 2.2f;

            while (elapsed < duration && dust != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (dust != null)
                {
                    dust.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                    dust.transform.position += new Vector3(0f, Time.deltaTime * 0.25f, 0f);
                }
                if (dsr != null)
                {
                    Color c = dsr.color;
                    c.a = Mathf.Lerp(0.45f, 0f, t);
                    dsr.color = c;
                }
                yield return null;
            }

            if (dust != null) Destroy(dust);
        }

        public void SetNearbyBuilding(TownBuilding building)
        {
            currentBuilding = building;
        }

        public void ClearNearbyBuilding(TownBuilding building)
        {
            if (currentBuilding == building)
            {
                currentBuilding = null;
            }
        }

        /// <summary>
        /// Public interaction trigger called by [E] key or on-screen touch Interact button.
        /// </summary>
        public void Interact()
        {
            if (currentBuilding != null && currentBuilding.isPlayerInside)
            {
                currentBuilding.Interact();
            }
        }
    }
}
