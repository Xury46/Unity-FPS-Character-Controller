using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSCharacterController
{
    [System.Serializable]
    public class FPSControllerSettings
    {
        public class HeightSettings
        {
            public HeightSettings(float cameraHeight, float capsuleHeight)
            {
                this.cameraHeight = cameraHeight;
                this.capsuleHeight = capsuleHeight;
            }

            public HeightSettings(HeightSettings settingsToCopy)
            {
                this.cameraHeight = settingsToCopy.cameraHeight;
                this.capsuleHeight = settingsToCopy.capsuleHeight;
            }
            
            public float cameraHeight;
            public float capsuleHeight;
        }

        [Tooltip("Duration (in seconds) of the transiton between states. Used to blend capsule height, camera height, and move speed.")]
        public float stateTransition_Duration = 1.5f;
        [HideInInspector] public float stateTransition_Progress = 0.0f; // Progress from 0-1

        public HeightSettings height_Crouching = new HeightSettings(0.75f, 1.00f);
        public HeightSettings height_Standing = new HeightSettings(1.75f, 2.0f);
        [HideInInspector] public HeightSettings height_Current;

        public float lateralFriction_Grounded = 5.0f;
        public float lateralFriction_Airborne = 2.5f;
        public float lateralFriction_Current;

        [HideInInspector] public Vector3 lateralMoveVector;

        // Gravity
        [HideInInspector] public Vector3 gravityDirection = Vector3.down;
        [HideInInspector] public float gravityStrength = 20.0f; // 9.81f;
        
        public float moveSpeed_Crouching = 5.0f;
        public float moveSpeed_Walking = 15.0f;
        public float moveSpeed_Running = 25.0f;
        
        [HideInInspector] public float moveSpeed_Current;

        public float jumpForce = 4.0f;
    }

    public class FPSController : MonoBehaviour
    {
        [SerializeField] private FPSControllerSettings settings;
        [SerializeField] private PlayerInput playerInput;
        public Camera playerCamera;
        [HideInInspector] public Rigidbody playerRB;
        public CapsuleCollider capsuleCollider;

        [HideInInspector] public Vector3 localLateralVelocity;
        [HideInInspector] public Vector3 localVerticalVelocity;
        [HideInInspector] public Vector3 lateralForceAddedLastFixedUpdate_World; // World space
        [HideInInspector] public Vector3 totalPredictedLateralVelocity_World;
        [HideInInspector] public Vector3 lateralVelocityDelta_World;
        [HideInInspector] public Vector3 lateralVelocityprojected_World;

        public LayerMask groundedCheckLayers;

        // Camera Pitch
        [HideInInspector] public float pitch_Current = 0.0f;
        public float pitch_Max = 90.0f;
        public float pitch_Min = -90.0f;
        public bool pitch_Invert = false;
        private float pitch_Velocity; // continuously modified by the smooth damp
        private float pitch_Target;
        
        // Camera Yaw
        [HideInInspector] public float yaw_Previous = 0.0f;
        [HideInInspector] public float yaw_Current = 0.0f;
        private float yaw_Velocity; // continuously modified by the smooth damp
        private float yaw_Target;

        public float lookSensitivity = 0.05f;
        public bool smoothMouseInput = true;
        public float lookSmoothing = 0.01f;

        //State machine states
        public GroundedCrouching groundedCrouching;
        public GroundedStanding groundedStanding;
        public GroundedRunning groundedRunning;
        public AirborneCrouching airborneCrouching;
        public AirborneStanding airborneStanding;

        private MovementState currentState;

        private void Awake()
        {
            playerRB = GetComponent<Rigidbody>();
            groundedCrouching = new GroundedCrouching(this, settings);
            groundedStanding = new GroundedStanding(this, settings);
            groundedRunning = new GroundedRunning(this, settings);
            airborneCrouching = new AirborneCrouching(this, settings);
            airborneStanding = new AirborneStanding(this, settings);
            currentState = groundedStanding;

            settings.height_Current = new FPSControllerSettings.HeightSettings(settings.height_Standing);
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            playerCamera.transform.parent = null; // Detatch the camera to avoid stutter
        }

        void Update()
        {
            currentState.OnStateUpdate();
        }

        void FixedUpdate()
        {
            currentState.OnStateFixedUpdate();
        }

        void LateUpdate()
        {
            playerCamera.transform.position = transform.position + transform.up * settings.height_Current.cameraHeight; // Make the detatched camera follow the position of the player
            if (smoothMouseInput) SmoothLook();
            else Look();
        }
        
        public void InputMove(InputAction.CallbackContext context)
        {
            Vector2 inputMovement = context.ReadValue<Vector2>();
            settings.lateralMoveVector = new Vector3(inputMovement.x, 0.0f, inputMovement.y);
            settings.lateralMoveVector = Vector3.ClampMagnitude(settings.lateralMoveVector, 1.0f);
            //settings.lateralMoveVector *= Time.fixedDeltaTime;
        }

        public void InputLook(InputAction.CallbackContext context)
        {
            Vector2 inputLook = context.ReadValue<Vector2>();
            float lookThreshold = 0.01f;
            if (inputLook.sqrMagnitude < lookThreshold) return;
            //inputLook *= lookSensitivity * Time.deltaTime;
            inputLook *= lookSensitivity;

            float invert = pitch_Invert ? 1.0f : -1.0f;
            pitch_Target += inputLook.y * invert;
            pitch_Target = Mathf.Clamp(pitch_Target, pitch_Min, pitch_Max);

            // Keep yaw clamped within -180 and 180 degrees
            yaw_Target += inputLook.x;
            if (yaw_Target <= -180.0f) yaw_Target += 360.0f;
            else if (yaw_Target > 180.0f) yaw_Target -= 360.0f;
        }

        private void SmoothLook()
        {   
            pitch_Current = Mathf.SmoothDamp(pitch_Current, pitch_Target, ref pitch_Velocity, lookSmoothing);

            // Handle yaw smooth damping as it crosses over 360 degrees
            float yaw_Delta = yaw_Current - yaw_Target;
            if (yaw_Delta <= -180.0f) yaw_Current += 360.0f;
            else if (yaw_Delta > 180.0f) yaw_Current -= 360.0f;
            
            yaw_Current = Mathf.SmoothDamp(yaw_Current, yaw_Target, ref yaw_Velocity, lookSmoothing);

            currentState.ApplyCameraLook();
        }

        private void Look()
        {
            pitch_Current = pitch_Target;
            
            yaw_Current = yaw_Target;
            
            currentState.ApplyCameraLook();
        }

        public void InputJump(InputAction.CallbackContext context)
        {
            currentState.ApplyJump();
        }

        public void InputRun(InputAction.CallbackContext context)
        {
            if (context.started) currentState.OnRun();
            else if (context.canceled) currentState.OnWalk();
        }

        public void InputCrouch(InputAction.CallbackContext context)
        {
            if (context.started) currentState.OnCrouch();
            else if (context.canceled) currentState.OnStand();
        }

        public void ChangeState(MovementState stateToChangeTo)
        {
            if (currentState == stateToChangeTo) return;

            currentState.OnStateExit();
            currentState = stateToChangeTo;
            currentState.OnStateEnter();
        }
    }
}