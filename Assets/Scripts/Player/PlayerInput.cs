using UnityEngine;
using UnityEngine.InputSystem;

namespace Shredsquatch.Player
{
    public class PlayerInput : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset _inputActions;

        // Input action references
        private InputAction _steerAction;
        private InputAction _tuckAction;
        private InputAction _brakeAction;
        private InputAction _jumpAction;
        private InputAction _spinAction;
        private InputAction _grabAction;
        private InputAction _flipAction;
        private InputAction _lookAction;
        private InputAction _pauseAction;

        // Movement
        public float SteerInput { get; private set; }      // -1 to 1
        public bool IsTucking { get; private set; }
        public bool IsBraking { get; private set; }

        // Jump
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }

        // Tricks
        public float SpinInput { get; private set; }       // -1 to 1
        public int GrabInput { get; private set; }         // 0=none, 1-4 for grabs
        public bool FlipForward { get; private set; }
        public bool FlipBackward { get; private set; }

        // Camera
        public Vector2 LookInput { get; private set; }

        // Menu
        public bool PausePressed { get; private set; }

        // Frame state tracking for button events
        private bool _jumpWasPressed;
        private bool _jumpWasReleased;
        private bool _pauseWasPressed;

        private void Awake()
        {
            SetupInputActions();
        }

        private void SetupInputActions()
        {
            // If no InputActionAsset assigned, create default inline actions
            if (_inputActions == null)
            {
                CreateDefaultInputActions();
            }
            else
            {
                // Get actions from asset
                var gameplay = _inputActions.FindActionMap("Gameplay");
                if (gameplay != null)
                {
                    _steerAction = gameplay.FindAction("Steer");
                    _tuckAction = gameplay.FindAction("Tuck");
                    _brakeAction = gameplay.FindAction("Brake");
                    _jumpAction = gameplay.FindAction("Jump");
                    _spinAction = gameplay.FindAction("Spin");
                    _grabAction = gameplay.FindAction("Grab");
                    _flipAction = gameplay.FindAction("Flip");
                    _lookAction = gameplay.FindAction("Look");
                    _pauseAction = gameplay.FindAction("Pause");
                }
            }

            // Subscribe to button events
            if (_jumpAction != null)
            {
                _jumpAction.started += OnJumpStarted;
                _jumpAction.canceled += OnJumpCanceled;
            }

            if (_pauseAction != null)
            {
                _pauseAction.started += OnPauseStarted;
            }
        }

        private void CreateDefaultInputActions()
        {
            // Steer: A/D, Left Stick X
            _steerAction = new InputAction("Steer", InputActionType.Value);
            _steerAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            _steerAction.AddBinding("<Gamepad>/leftStick/x");

            // Tuck: W, Right Trigger
            _tuckAction = new InputAction("Tuck", InputActionType.Button);
            _tuckAction.AddBinding("<Keyboard>/w");
            _tuckAction.AddBinding("<Gamepad>/rightTrigger");

            // Brake: S, Left Trigger
            _brakeAction = new InputAction("Brake", InputActionType.Button);
            _brakeAction.AddBinding("<Keyboard>/s");
            _brakeAction.AddBinding("<Gamepad>/leftTrigger");

            // Jump: Space, A button
            _jumpAction = new InputAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            // Spin: Q/E, Bumpers, Mouse X
            _spinAction = new InputAction("Spin", InputActionType.Value);
            _spinAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q")
                .With("Positive", "<Keyboard>/e");
            _spinAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Gamepad>/leftShoulder")
                .With("Positive", "<Gamepad>/rightShoulder");
            _spinAction.AddBinding("<Mouse>/delta/x")
                .WithProcessor("scale(factor=0.05)")
                .WithProcessor("clamp(min=-1,max=1)");

            // Grab: 1-4 keys, D-Pad (encoded as Vector2)
            _grabAction = new InputAction("Grab", InputActionType.Value, binding: "<Gamepad>/dpad");
            _grabAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/1")
                .With("Down", "<Keyboard>/4")
                .With("Left", "<Keyboard>/3")
                .With("Right", "<Keyboard>/2");

            // Flip: Vertical axis (W/S when airborne, Left Stick Y)
            _flipAction = new InputAction("Flip", InputActionType.Value);
            _flipAction.AddBinding("<Gamepad>/leftStick/y");
            _flipAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/s")
                .With("Positive", "<Keyboard>/w");

            // Look: Mouse, Right Stick
            _lookAction = new InputAction("Look", InputActionType.Value, binding: "<Mouse>/delta");
            _lookAction.AddBinding("<Gamepad>/rightStick");

            // Pause: Escape, Start button
            _pauseAction = new InputAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");

            // Subscribe to button events
            _jumpAction.started += OnJumpStarted;
            _jumpAction.canceled += OnJumpCanceled;
            _pauseAction.started += OnPauseStarted;
        }

        private void OnEnable()
        {
            _steerAction?.Enable();
            _tuckAction?.Enable();
            _brakeAction?.Enable();
            _jumpAction?.Enable();
            _spinAction?.Enable();
            _grabAction?.Enable();
            _flipAction?.Enable();
            _lookAction?.Enable();
            _pauseAction?.Enable();
        }

        private void OnDisable()
        {
            _steerAction?.Disable();
            _tuckAction?.Disable();
            _brakeAction?.Disable();
            _jumpAction?.Disable();
            _spinAction?.Disable();
            _grabAction?.Disable();
            _flipAction?.Disable();
            _lookAction?.Disable();
            _pauseAction?.Disable();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_jumpAction != null)
            {
                _jumpAction.started -= OnJumpStarted;
                _jumpAction.canceled -= OnJumpCanceled;
            }

            if (_pauseAction != null)
            {
                _pauseAction.started -= OnPauseStarted;
            }

            // Dispose inline actions if we created them
            if (_inputActions == null)
            {
                _steerAction?.Dispose();
                _tuckAction?.Dispose();
                _brakeAction?.Dispose();
                _jumpAction?.Dispose();
                _spinAction?.Dispose();
                _grabAction?.Dispose();
                _flipAction?.Dispose();
                _lookAction?.Dispose();
                _pauseAction?.Dispose();
            }
        }

        private void Update()
        {
            ReadMovementInput();
            ReadJumpInput();
            ReadTrickInput();
            ReadCameraInput();
            ReadMenuInput();
        }

        private void LateUpdate()
        {
            // Clear one-frame button states
            _jumpWasPressed = false;
            _jumpWasReleased = false;
            _pauseWasPressed = false;
        }

        // Input action callbacks
        private void OnJumpStarted(InputAction.CallbackContext ctx) => _jumpWasPressed = true;
        private void OnJumpCanceled(InputAction.CallbackContext ctx) => _jumpWasReleased = true;
        private void OnPauseStarted(InputAction.CallbackContext ctx) => _pauseWasPressed = true;

        private void ReadMovementInput()
        {
            SteerInput = _steerAction?.ReadValue<float>() ?? 0f;
            IsTucking = _tuckAction?.IsPressed() ?? false;
            IsBraking = _brakeAction?.IsPressed() ?? false;
        }

        private void ReadJumpInput()
        {
            JumpPressed = _jumpWasPressed;
            JumpHeld = _jumpAction?.IsPressed() ?? false;
            JumpReleased = _jumpWasReleased;
        }

        private void ReadTrickInput()
        {
            // Spin input
            SpinInput = _spinAction?.ReadValue<float>() ?? 0f;

            // Grab input: Convert D-Pad Vector2 to grab index
            Vector2 grabVec = _grabAction?.ReadValue<Vector2>() ?? Vector2.zero;
            GrabInput = 0;
            if (grabVec.y > 0.5f) GrabInput = 1;      // Up = Nose
            else if (grabVec.x > 0.5f) GrabInput = 2; // Right = Indy
            else if (grabVec.x < -0.5f) GrabInput = 3; // Left = Melon
            else if (grabVec.y < -0.5f) GrabInput = 4; // Down = Stalefish

            // Flip input
            float flipValue = _flipAction?.ReadValue<float>() ?? 0f;
            FlipForward = flipValue > 0.5f;
            FlipBackward = flipValue < -0.5f;
        }

        private void ReadCameraInput()
        {
            LookInput = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        private void ReadMenuInput()
        {
            PausePressed = _pauseWasPressed;
        }
    }
}
