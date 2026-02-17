using UnityEngine;
using UnityEngine.InputSystem;
using MobileControllerFramework;
using SickOfIt.Core.Addressables;

namespace SickOfIt.Core.Input
{
    /// <summary>
    /// 2D top-down player controller with mobile and PC input support.
    /// Loads projectile prefab via Addressables for WebGL optimization.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class TopDownController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 2f;
        
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private int maxAirJumps = 3;
        [Tooltip("How fast the character falls (higher = faster). Try 3-5 for faster falling.")]
        [SerializeField] private float gravityScale = 1.8f;
        [Tooltip("Distance below collider to check for ground")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [Tooltip("Width of ground check box (smaller than collider)")]
        [SerializeField] private float groundCheckWidth = 0.1f;
        [Tooltip("Layers that count as ground (exclude player layer!)")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [Tooltip("Minimum upward velocity needed to trigger jump animation (prevents false triggers)")]
        [SerializeField] private float jumpDetectVelocityThreshold = 0.3f;
        
        [Header("Projectile Settings")]
        [Tooltip("Addressable address of the projectile prefab (e.g., 'Projectile_Player')")]
        [SerializeField] private string projectileAddress = "Projectile_Player";
        [Tooltip("Optional: Transform where projectiles spawn. If null, spawns at player position.")]
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float fireRate = 0.25f;
        [SerializeField] private float projectileLifetime = 5f;
        
        [Header("Platform Interaction")]
        [Tooltip("Layer to ignore collision with when moving down (e.g., 'Platform' layer)")]
        [SerializeField] private LayerMask platformLayer;
        [Tooltip("How far down the player must press to drop through platforms (-1 = full down)")]
        [SerializeField] private float dropThroughThreshold = -0.5f;
        [Tooltip("Downward force applied when dropping through platforms (helps fall faster)")]
        [SerializeField] private float dropThroughForce = 5f;
        
        [Header("Mobile Input")]
        [Tooltip("Analog stick for movement. Add AnalogTapJump component for tap-to-jump.")]
        [SerializeField] private Analog moveAnalog;
        [Tooltip("Analog stick for aiming/firing. Movement beyond threshold automatically fires.")]
        [SerializeField] private Analog fireAnalog;
        [SerializeField] private Dpad dpad;
        [SerializeField] private ArrowKeys arrowKeys;
        
        [Header("PC Input Actions")]
        [Tooltip("WASD/Arrow keys movement action")]
        [SerializeField] private InputActionReference moveAction;
        [Tooltip("Space key - bind to 'Player/Jump' action")]
        [SerializeField] private InputActionReference jumpAction;
        [Tooltip("Left mouse button - bind to 'Player/Fire' action")]
        [SerializeField] private InputActionReference fireAction;
        
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameInputManager gameInputManager;
        
        [Header("Rotation Settings")]
        [Tooltip("Speed of rotation when moving (smooth rotation to show front/back/sides)")]
        [SerializeField] private float normalRotationSpeed = 900f;
        [Tooltip("Speed of rotation when firing (fast snap to left/right)")]
        [SerializeField] private float firingRotationSpeed = 1800f;
        
        [Header("Shadow Settings")]
        [Tooltip("The shadow sprite transform that follows the character")]
        [SerializeField] private Transform shadowSprite;
        [Tooltip("Maximum height at which shadow is still visible (shadow scale = 0 at this height)")]
        [SerializeField] private float shadowMaxHeight = 10f;
        [Tooltip("Minimum scale of shadow when at max height (0 = invisible)")]
        [SerializeField] private float shadowMinScale = 0.2f;
        [Tooltip("How far down to raycast for ground detection")]
        [SerializeField] private float shadowRaycastDistance = 20f;
        
        #endregion
        
        #region Animator Parameter Hashes
        
        private static readonly int OnGroundHash = Animator.StringToHash("OnGround");
        private static readonly int VelocityXHash = Animator.StringToHash("Velocity_X");
        private static readonly int VelocityZHash = Animator.StringToHash("Velocity_Z");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int FireHash = Animator.StringToHash("Fire");
        
        #endregion
        
        #region Private Fields
        
        private Rigidbody2D rb;
        private Collider2D col;
        
        // Movement
        private Vector2 moveInput;
        private float currentYRotation;
        private float targetYRotation;
        
        // Jump
        private bool isGrounded;
        private int airJumpsRemaining;
        private bool jumpRequested;
        
        // Firing
        private Vector2 fireDirection;
        private bool isFiring;
        private float lastFireTime;
        
        // Addressables
        private GameObject cachedProjectilePrefab;
        private bool isProjectileReady = false;
        
        // Platform collision
        private int playerLayer;
        private bool isIgnoringPlatformCollision = false;
        private bool isDroppingThrough = false;
        private System.Collections.Generic.List<DropThroughPlatform> registeredPlatforms = new System.Collections.Generic.List<DropThroughPlatform>();
        
        // Input tracking
        private bool isUsingMobileInput = false;
        private const float InputDeadzone = 0.1f;
        private const float MobileDropThreshold = -0.4f; // More lenient for analog sticks
        
        // Shadow
        private Vector3 shadowInitialScale;
        private Vector3 shadowInitialLocalPosition;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            playerLayer = gameObject.layer;
            
            if (rb != null)
            {
                rb.gravityScale = gravityScale;
            }
            
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // Initialize rotation to face right by default
            currentYRotation = 0f;
            targetYRotation = 0f;
            
            // Store initial shadow values
            if (shadowSprite != null)
            {
                shadowInitialScale = shadowSprite.localScale;
                shadowInitialLocalPosition = shadowSprite.localPosition;
            }
            
            PreloadProjectilePrefab();
        }
        
        private void PreloadProjectilePrefab()
        {
            if (string.IsNullOrEmpty(projectileAddress))
            {
                Debug.LogError("TopDownController: projectileAddress is not set!");
                return;
            }
            
            AddressablesAssetLoader.Instance.LoadAsset<GameObject>(projectileAddress, (prefab) =>
            {
                if (prefab != null)
                {
                    cachedProjectilePrefab = prefab;
                    isProjectileReady = true;
                }
                else
                {
                    Debug.LogError($"Failed to load projectile prefab: {projectileAddress}");
                }
            });
        }
        
        private void Start()
        {
            if (moveAction?.action != null)
                moveAction.action.Enable();
            if (jumpAction?.action != null)
                jumpAction.action.Enable();
            if (fireAction?.action != null)
                fireAction.action.Enable();

            if (gameInputManager != null)
                gameInputManager.OnJumpPressed += OnJumpRequested;
            
            // Subscribe to analog stick release event for immediate platform re-enable
            if (moveAnalog != null)
            {
                moveAnalog.StickExitEvent.AddListener(OnAnalogReleased);
            }
        }
        
        private void Update()
        {
            ReadMoveInput();
            ReadJumpInput();
            ReadFireInput();
            CheckGrounded();
            HandlePlatformCollision();
            HandleJump();
            HandleFiring();
            UpdateAnimator();
            UpdateShadow();
        }
        
        private void FixedUpdate()
        {
            HandleMovement();
        }
        
        private void OnDisable()
        {
            if (moveAction?.action != null)
                moveAction.action.Disable();
            if (jumpAction?.action != null)
                jumpAction.action.Disable();
            if (fireAction?.action != null)
                fireAction.action.Disable();

            if (gameInputManager != null)
                gameInputManager.OnJumpPressed -= OnJumpRequested;
            
            if (moveAnalog != null)
            {
                moveAnalog.StickExitEvent.RemoveListener(OnAnalogReleased);
            }
        }
        
        #endregion
        
        #region Ground Check
        
        private void CheckGrounded()
        {
            Vector2 boxSize = new Vector2(groundCheckWidth, groundCheckDistance);
            Vector2 boxCenter = (Vector2)transform.position + col.offset + Vector2.down * (col.bounds.extents.y + groundCheckDistance * 0.5f);
            
            bool wasGrounded = isGrounded;
            
            // If ignoring platform collision, exclude platform layer from ground check
            LayerMask checkLayer = isIgnoringPlatformCollision ? (groundLayer & ~platformLayer) : groundLayer;
            isGrounded = Physics2D.OverlapBox(boxCenter, boxSize, 0f, checkLayer);
            
            if (isGrounded && !wasGrounded)
            {
                airJumpsRemaining = maxAirJumps;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (col != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Vector2 boxSize = new Vector2(groundCheckWidth, groundCheckDistance);
                Vector2 boxCenter = (Vector2)transform.position + col.offset + Vector2.down * (col.bounds.extents.y + groundCheckDistance * 0.5f);
                Gizmos.DrawWireCube(boxCenter, boxSize);
            }
        }
        
        #endregion
        
        #region Input Reading
        
        private void ReadMoveInput()
        {
            moveInput = Vector2.zero;
            Vector2 mobileInput = Vector2.zero;
            Vector2 keyboardInput = Vector2.zero;
            bool usingMobileInput = false;
            
            if (moveAnalog != null && moveAnalog.gameObject.activeSelf)
            {
                mobileInput = moveAnalog.GetStickPosition();
                moveInput = mobileInput;
                usingMobileInput = true;
            }
            else if (dpad != null && dpad.gameObject.activeSelf)
            {
                mobileInput = dpad.GetDpadDirection();
                moveInput = mobileInput;
                usingMobileInput = true;
            }
            else if (arrowKeys != null && arrowKeys.gameObject.activeSelf)
            {
                mobileInput = arrowKeys.GetDirection();
                moveInput = mobileInput;
                usingMobileInput = true;
            }
            
            // Always check keyboard/gamepad input and use it if mobile input is idle
            if (moveAction?.action != null && moveAction.action.enabled)
            {
                keyboardInput = moveAction.action.ReadValue<Vector2>();
                if (keyboardInput.magnitude > InputDeadzone && moveInput.magnitude < InputDeadzone)
                {
                    moveInput = keyboardInput;
                    usingMobileInput = false;
                }
            }
            
            // Store whether we're using mobile input for platform threshold adjustment
            isUsingMobileInput = usingMobileInput;
        }
        
        private void ReadJumpInput()
        {
            if (jumpAction?.action != null && jumpAction.action.WasPressedThisFrame())
            {
                jumpRequested = true;
            }
        }
        
        /// <summary>
        /// Called by GameInputManager when any input source requests a jump.
        /// </summary>
        private void OnJumpRequested()
        {
            jumpRequested = true;
        }
        
        private void ReadFireInput()
        {
            fireDirection = Vector2.zero;
            isFiring = false;
            
            if (fireAnalog != null && fireAnalog.gameObject.activeSelf)
            {
                Vector2 analogInput = fireAnalog.GetStickPosition();
                if (analogInput.magnitude > InputDeadzone)
                {
                    fireDirection = analogInput.normalized;
                    isFiring = true;
                }
            }
            
            if (fireAction?.action != null && fireAction.action.IsPressed())
            {
                isFiring = true;
                fireDirection = GetMouseDirectionFromPlayer();
            }
        }
        
        private Vector2 GetMouseDirectionFromPlayer()
        {
            if (mainCamera == null || Mouse.current == null)
                return Vector2.zero;
            
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));
            Vector2 direction = ((Vector2)worldMousePos - (Vector2)transform.position).normalized;
            
            return direction;
        }
        
        #endregion
        
        #region Platform Collision
        
        private void HandlePlatformCollision()
        {
            // Use more lenient threshold for mobile analog sticks
            float threshold = isUsingMobileInput ? MobileDropThreshold : dropThroughThreshold;
            bool pressingDown = moveInput.y < threshold;
            
            // Only change state when transitioning
            if (pressingDown && !isDroppingThrough)
            {
                StartDroppingThrough();
            }
            else if (!pressingDown && isDroppingThrough)
            {
                StopDroppingThrough();
            }
        }
        
        private void StartDroppingThrough()
        {
            isDroppingThrough = true;
            
            // Disable all platform colliders
            foreach (var platform in registeredPlatforms)
            {
                if (platform != null)
                {
                    platform.DisablePlatform();
                }
            }
            
            int platformLayerIndex = GetLayerIndexFromMask(platformLayer);
            if (platformLayerIndex >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, platformLayerIndex, true);
                isIgnoringPlatformCollision = true;
            }
            
            // Apply downward force to help player fall through faster (only if grounded)
            if (isGrounded && rb != null && dropThroughForce > 0)
            {
                rb.AddForce(Vector2.down * dropThroughForce, ForceMode2D.Impulse);
            }
        }
        
        private void StopDroppingThrough()
        {
            isDroppingThrough = false;
            
            // Re-enable all platforms
            foreach (var platform in registeredPlatforms)
            {
                if (platform != null)
                {
                    platform.EnablePlatform();
                }
            }
            
            int platformLayerIndex = GetLayerIndexFromMask(platformLayer);
            if (platformLayerIndex >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, platformLayerIndex, false);
                isIgnoringPlatformCollision = false;
            }
        }
        
        private void OnAnalogReleased()
        {
            // When analog is released, immediately re-enable platforms
            if (isDroppingThrough)
            {
                StopDroppingThrough();
            }
        }
        
        private int GetLayerIndexFromMask(LayerMask mask)
        {
            // Convert LayerMask to layer index
            // LayerMask value is a bitmask, so we need to find which bit is set
            int layerNumber = 0;
            int layer = mask.value;
            
            if (layer == 0)
                return -1; // No layer selected
            
            // Find the first set bit
            while (layer > 1)
            {
                layer = layer >> 1;
                layerNumber++;
            }
            
            return layer == 1 ? layerNumber : -1;
        }
        
        /// <summary>
        /// Called by DropThroughPlatform components to register themselves
        /// </summary>
        public void RegisterPlatform(DropThroughPlatform platform)
        {
            if (!registeredPlatforms.Contains(platform))
            {
                registeredPlatforms.Add(platform);
            }
        }
        
        /// <summary>
        /// Called by DropThroughPlatform components when destroyed
        /// </summary>
        public void UnregisterPlatform(DropThroughPlatform platform)
        {
            registeredPlatforms.Remove(platform);
        }
        
        #endregion
        
        #region Movement & Jump
        
        private void HandleMovement()
        {
            if (rb == null) return;
            
            // Clamp horizontal input to [-1, 1] to prevent diagonal slowdown
            // This preserves analog magnitude control while ensuring diagonals don't reduce horizontal speed
            float horizontalInput = Mathf.Clamp(moveInput.x, -1f, 1f);
            
            Vector2 velocity = rb.linearVelocity;
            velocity.x = horizontalInput * moveSpeed;
            rb.linearVelocity = velocity;
            
            // Determine target rotation and rotation speed
            float rotationSpeed = normalRotationSpeed;
            
            // Priority 1: Firing direction (fast rotation to pure left/right)
            if (isFiring && Mathf.Abs(fireDirection.x) > InputDeadzone)
            {
                rotationSpeed = firingRotationSpeed;
                if (fireDirection.x > 0)
                {
                    targetYRotation = 0f;  // Right (side view)
                }
                else if (fireDirection.x < 0)
                {
                    targetYRotation = 180f; // Left (side view)
                }
            }
            // Priority 2: Movement direction (4 cardinal directions only)
            else if (moveInput.magnitude > InputDeadzone)
            {
                // Use the dominant axis to determine facing
                // Horizontal (left/right) takes priority over vertical
                float absX = Mathf.Abs(moveInput.x);
                float absY = Mathf.Abs(moveInput.y);
                
                if (absX >= absY)
                {
                    // Horizontal movement is dominant - face left or right
                    if (moveInput.x > 0)
                    {
                        targetYRotation = 0f;   // Right (side view)
                    }
                    else
                    {
                        targetYRotation = 180f; // Left (side view)
                    }
                }
                else
                {
                    // Vertical movement is dominant - face front or back
                    if (moveInput.y > 0)
                    {
                        targetYRotation = 270f; // Up/Back (away from camera)
                    }
                    else
                    {
                        targetYRotation = 90f;  // Down/Front (facing camera)
                    }
                }
            }
            // If no input, maintain current facing direction
            
            // Smoothly rotate towards target
            currentYRotation = Mathf.MoveTowardsAngle(currentYRotation, targetYRotation, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
        }
        
        private void HandleJump()
        {
            if (!jumpRequested) return;
            jumpRequested = false;
            
            if (isGrounded)
            {
                Jump();
                airJumpsRemaining = maxAirJumps;
            }
            else if (airJumpsRemaining > 0)
            {
                Jump();
                airJumpsRemaining--;
            }
        }
        
        private void Jump()
        {
            if (rb == null) return;
            
            Vector2 velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
            
            if (animator != null)
            {
                animator.SetTrigger(JumpHash);
            }
        }
        
        #endregion
        
        #region Projectile Firing
        
        private void HandleFiring()
        {
            if (fireDirection.magnitude < InputDeadzone) return;
            if (Time.time - lastFireTime < fireRate) return;
            if (!isProjectileReady) return;
            
            FireProjectile(fireDirection);
        }
        
        private void FireProjectile(Vector2 direction)
        {
            if (cachedProjectilePrefab == null)
            {
                Debug.LogWarning("Projectile prefab not loaded yet!");
                return;
            }
            
            lastFireTime = Time.time;
            
            Vector3 spawnPosition = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
            
            GameObject projectile = Instantiate(cachedProjectilePrefab, spawnPosition, Quaternion.identity);
            
            Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
            if (projectileCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, col);
            }
            
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                projectileRb.linearVelocity = direction * projectileSpeed;
            }
            
            if (animator != null)
            {
                animator.SetTrigger(FireHash);
            }
            
            Destroy(projectile, projectileLifetime);
        }
        
        #endregion
        
        #region Animation
        
        private void UpdateAnimator()
        {
            if (animator == null) return;
            
            animator.SetBool(OnGroundHash, isGrounded);
            animator.SetFloat(VelocityXHash, Mathf.Abs(moveInput.x));
            
            if (rb != null)
            {
                float verticalVelocity = rb.linearVelocity.y;
                animator.SetFloat(VelocityZHash, verticalVelocity);
            }
        }
        
        #endregion
        
        #region Shadow
        
        private void UpdateShadow()
        {
            if (shadowSprite == null) return;
            
            // When grounded, restore initial position and scale
            if (isGrounded)
            {
                shadowSprite.localPosition = shadowInitialLocalPosition;
                shadowSprite.localScale = shadowInitialScale;
                return;
            }
            
            // Raycast down to find ground
            LayerMask checkLayer = isIgnoringPlatformCollision ? (groundLayer & ~platformLayer) : groundLayer;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, shadowRaycastDistance, checkLayer);
            
            if (hit.collider != null)
            {
                // Calculate distance from character to ground
                float distanceToGround = hit.distance;
                
                // Position shadow at ground level (world space)
                Vector3 shadowWorldPosition = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                shadowSprite.position = shadowWorldPosition;
                
                // Calculate scale based on distance using lerp
                // At distance 0, scale = initial scale; at shadowMaxHeight, scale = initial * shadowMinScale
                float heightRatio = Mathf.Clamp01(distanceToGround / shadowMaxHeight);
                float scaleFactor = Mathf.Lerp(1f, shadowMinScale, heightRatio);
                shadowSprite.localScale = shadowInitialScale * scaleFactor;
            }
            else
            {
                // No ground found, make shadow very small or hide it
                shadowSprite.localScale = shadowInitialScale * shadowMinScale;
            }
        }
        
        #endregion
    }
}
