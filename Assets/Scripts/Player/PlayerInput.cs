using UnityEngine;

namespace Shredsquatch.Player
{
    public class PlayerInput : MonoBehaviour
    {
        // Movement
        public float SteerInput { get; private set; }      // -1 to 1
        public bool IsTucking { get; private set; }
        public bool IsBraking { get; private set; }

        // Jump
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }

        // Tricks
        public float SpinInput { get; private set; }       // -1 to 1 (Q/E or mouse X)
        public int GrabInput { get; private set; }         // 0=none, 1-4 for grabs
        public bool FlipForward { get; private set; }
        public bool FlipBackward { get; private set; }

        // Camera
        public Vector2 LookInput { get; private set; }

        // Menu
        public bool PausePressed { get; private set; }

        private void Update()
        {
            ReadMovementInput();
            ReadJumpInput();
            ReadTrickInput();
            ReadCameraInput();
            ReadMenuInput();
        }

        private void ReadMovementInput()
        {
            // Keyboard: A/D or controller left stick
            float keyboardSteer = 0f;
            if (Input.GetKey(KeyCode.A)) keyboardSteer -= 1f;
            if (Input.GetKey(KeyCode.D)) keyboardSteer += 1f;

            float controllerSteer = Input.GetAxis("Horizontal");
            SteerInput = Mathf.Abs(keyboardSteer) > 0.1f ? keyboardSteer : controllerSteer;

            // Tuck: W or Right Trigger
            IsTucking = Input.GetKey(KeyCode.W) || Input.GetAxis("RightTrigger") > 0.5f;

            // Brake: S or Left Trigger
            IsBraking = Input.GetKey(KeyCode.S) || Input.GetAxis("LeftTrigger") > 0.5f;
        }

        private void ReadJumpInput()
        {
            // Space or A button (joystick button 0)
            JumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
            JumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.JoystickButton0);
            JumpReleased = Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.JoystickButton0);
        }

        private void ReadTrickInput()
        {
            // Spin: Q/E or bumpers (LB/RB) or mouse X movement
            float keyboardSpin = 0f;
            if (Input.GetKey(KeyCode.Q)) keyboardSpin -= 1f;
            if (Input.GetKey(KeyCode.E)) keyboardSpin += 1f;

            float bumperSpin = 0f;
            if (Input.GetKey(KeyCode.JoystickButton4)) bumperSpin -= 1f; // LB
            if (Input.GetKey(KeyCode.JoystickButton5)) bumperSpin += 1f; // RB

            float mouseX = Input.GetAxis("Mouse X");
            SpinInput = Mathf.Abs(keyboardSpin) > 0.1f ? keyboardSpin :
                        Mathf.Abs(bumperSpin) > 0.1f ? bumperSpin :
                        Mathf.Clamp(mouseX * 0.5f, -1f, 1f);

            // Grab: 1-4 keys or D-Pad
            GrabInput = 0;
            if (Input.GetKey(KeyCode.Alpha1)) GrabInput = 1; // Nose
            else if (Input.GetKey(KeyCode.Alpha2)) GrabInput = 2; // Indy
            else if (Input.GetKey(KeyCode.Alpha3)) GrabInput = 3; // Melon
            else if (Input.GetKey(KeyCode.Alpha4)) GrabInput = 4; // Stalefish
            else
            {
                // D-Pad (varies by platform, using common mappings)
                float dpadV = Input.GetAxis("DPadVertical");
                float dpadH = Input.GetAxis("DPadHorizontal");
                if (dpadV > 0.5f) GrabInput = 1;      // Up = Nose
                else if (dpadH > 0.5f) GrabInput = 2; // Right = Indy
                else if (dpadH < -0.5f) GrabInput = 3; // Left = Melon
                else if (dpadV < -0.5f) GrabInput = 4; // Down = Stalefish
            }

            // Flip: W+Jump or Left Stick Up/Down + Jump
            float verticalInput = Input.GetAxis("Vertical");
            FlipForward = verticalInput > 0.5f;
            FlipBackward = verticalInput < -0.5f;
        }

        private void ReadCameraInput()
        {
            // Mouse or right stick
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float stickX = Input.GetAxis("RightStickHorizontal");
            float stickY = Input.GetAxis("RightStickVertical");

            LookInput = new Vector2(
                Mathf.Abs(mouseX) > 0.01f ? mouseX : stickX,
                Mathf.Abs(mouseY) > 0.01f ? mouseY : stickY
            );
        }

        private void ReadMenuInput()
        {
            PausePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7);
        }
    }
}
