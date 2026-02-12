using UnityEngine;

namespace Terranova.Camera
{
    /// <summary>
    /// RTS-style camera for viewing the voxel terrain.
    ///
    /// Controls (MS1 – mouse/keyboard, touch comes in MS4):
    ///   Pan:    WASD or Arrow keys (or middle-mouse drag)
    ///   Zoom:   Mouse scroll wheel
    ///   Rotate: Hold middle mouse button + move mouse
    ///
    /// The camera looks down at an angle (like Anno, Settlers, Age of Empires).
    /// It stays above the terrain and clamps to world boundaries.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Camera pan speed in units/second.")]
        [SerializeField] private float _panSpeed = 30f;

        [Tooltip("Speed multiplier when holding Shift.")]
        [SerializeField] private float _fastMultiplier = 2.5f;

        [Header("Zoom")]
        [Tooltip("Closest zoom distance (units above ground).")]
        [SerializeField] private float _minZoom = 10f;

        [Tooltip("Farthest zoom distance.")]
        [SerializeField] private float _maxZoom = 120f;

        [Tooltip("How fast the scroll wheel zooms.")]
        [SerializeField] private float _zoomSpeed = 15f;

        [Tooltip("Smoothing factor for zoom. Higher = smoother but more laggy.")]
        [SerializeField] private float _zoomSmoothing = 8f;

        [Header("Rotation")]
        [Tooltip("Mouse rotation speed (degrees per pixel of mouse movement).")]
        [SerializeField] private float _rotateSpeed = 0.3f;

        [Header("Initial Position")]
        [Tooltip("Starting camera angle (degrees from horizontal). 60 = steep top-down, 30 = more side view.")]
        [SerializeField] private float _defaultPitch = 50f;

        [Tooltip("Starting height above terrain.")]
        [SerializeField] private float _defaultHeight = 60f;

        // Current zoom level (interpolated smoothly)
        private float _currentZoom;
        private float _targetZoom;

        // Camera rig: the script controls a pivot point on the ground;
        // the actual camera is offset from this point.
        private Vector3 _pivotPosition;
        private float _yaw;

        private void Start()
        {
            // Start the camera centered on the world (if WorldManager exists)
            var world = Terranova.Terrain.WorldManager.Instance;
            if (world != null)
            {
                float centerX = world.WorldBlocksX * 0.5f;
                float centerZ = world.WorldBlocksZ * 0.5f;
                _pivotPosition = new Vector3(centerX, 0, centerZ);
            }
            else
            {
                _pivotPosition = new Vector3(64, 0, 64);
            }

            _currentZoom = _defaultHeight;
            _targetZoom = _defaultHeight;
            _yaw = 0f;

            UpdateCameraTransform();
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
            HandleRotation();
            ClampToWorldBounds();
            UpdateCameraTransform();
        }

        /// <summary>
        /// WASD/Arrow keys to pan the camera across the terrain.
        /// Movement direction is relative to the camera's current facing.
        /// </summary>
        private void HandlePan()
        {
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
            float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down

            if (Mathf.Approximately(horizontal, 0) && Mathf.Approximately(vertical, 0))
                return;

            // Calculate movement direction relative to camera's yaw rotation
            Vector3 forward = Quaternion.Euler(0, _yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, _yaw, 0) * Vector3.right;

            Vector3 move = (forward * vertical + right * horizontal).normalized;

            // Speed scales with zoom level (panning feels consistent at all zoom levels)
            float speedFactor = _currentZoom / _defaultHeight;
            float speed = _panSpeed * speedFactor;

            // Hold Shift to move faster
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= _fastMultiplier;

            _pivotPosition += move * speed * Time.deltaTime;
        }

        /// <summary>
        /// Scroll wheel to zoom in/out.
        /// </summary>
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (!Mathf.Approximately(scroll, 0))
            {
                _targetZoom -= scroll * _zoomSpeed * (_targetZoom * 0.3f);
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }

            // Smooth zoom interpolation
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.deltaTime * _zoomSmoothing);
        }

        /// <summary>
        /// Hold middle mouse button + move mouse to rotate the camera around the pivot.
        /// </summary>
        private void HandleRotation()
        {
            if (Input.GetMouseButton(2)) // Middle mouse button
            {
                float mouseX = Input.GetAxis("Mouse X");
                _yaw += mouseX * _rotateSpeed * 100f * Time.deltaTime;
            }
        }

        /// <summary>
        /// Keep the camera pivot within world boundaries.
        /// </summary>
        private void ClampToWorldBounds()
        {
            var world = Terranova.Terrain.WorldManager.Instance;
            if (world == null)
                return;

            float padding = 10f;
            _pivotPosition.x = Mathf.Clamp(_pivotPosition.x, -padding, world.WorldBlocksX + padding);
            _pivotPosition.z = Mathf.Clamp(_pivotPosition.z, -padding, world.WorldBlocksZ + padding);
        }

        /// <summary>
        /// Position the actual camera based on pivot, zoom, pitch, and yaw.
        /// The camera orbits above the pivot point looking down at an angle.
        /// </summary>
        private void UpdateCameraTransform()
        {
            // Calculate camera offset from pivot based on pitch angle and zoom distance
            Quaternion rotation = Quaternion.Euler(_defaultPitch, _yaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -_currentZoom);

            transform.position = _pivotPosition + offset;
            transform.rotation = rotation;
        }
    }
}
