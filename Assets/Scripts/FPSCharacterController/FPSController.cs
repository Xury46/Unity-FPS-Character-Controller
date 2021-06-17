using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSCharacterController
{
    public class FPSController : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;
        public Camera playerCamera;
        private float cameraVerticalOffset;
        [HideInInspector] public Rigidbody playerRB;
        public Transform orientation;

        public LayerMask groundedCheckLayers;

        // Camera Pitch
        public float pitch_Current = 0.0f;
        public float pitch_Max = 90.0f;
        public float pitch_Min = -90.0f;
        public bool pitch_Invert = false;
        private float pitch_Velocity; // continuously modified by the smooth damp
        public float pitch_Target;
        
        // Camera Yaw
        public float yaw_Current = 0.0f;
        private float yaw_Velocity; // continuously modified by the smooth damp
        public float yaw_Target;
        
        public float lookSensitivity = 0.25f;
        public float lookSmoothing = 0.5f;

        // Movement
        public Vector3 lateralMoveVector;
        public float lateralMoveSpeed = 50.0f;

        public float jumpForce = 3.0f;
        

        //State machine states
        public GroundedStanding groundedStanding;
        public AirborneStanding airborneStanding;

        private MovementState currentState;


        private void Awake()
        {
            playerRB = GetComponent<Rigidbody>();
            groundedStanding = new GroundedStanding(this);
            airborneStanding = new AirborneStanding(this);
            currentState = groundedStanding;
        }

        private void Start()
        {            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            cameraVerticalOffset = playerCamera.transform.localPosition.y; // Cache the original camera height
            playerCamera.transform.parent = null; // Detatch the camera to avoid stutter
        }

        void Update()
        {
            SmoothLook();
            playerCamera.transform.position = orientation.position + orientation.up * cameraVerticalOffset; // Make the detatched camera follow the position of the player
            currentState.OnStateUpdate();
        }

        void FixedUpdate()
        {
            currentState.OnStateFixedUpdate();
        }
        
        public void LateralInput(InputAction.CallbackContext context)
        {
            Vector2 inputMovement = context.ReadValue<Vector2>();
            lateralMoveVector = new Vector3(inputMovement.x, 0.0f, inputMovement.y) * lateralMoveSpeed * Time.fixedDeltaTime;
        }

        public void LookInput(InputAction.CallbackContext context)
        {
            Vector2 inputLook = context.ReadValue<Vector2>();
            //inputLook *= Time.deltaTime;

            int invert = pitch_Invert ? 1 : -1;
            pitch_Target += (inputLook.y * lookSensitivity * invert);
            pitch_Target = Mathf.Clamp(pitch_Target, pitch_Min, pitch_Max);

            // Keep yaw clamped within -180 and 180 degrees
            yaw_Target += (inputLook.x * lookSensitivity);
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

            currentState.ApplyLook();      
        }

        public void Jump(InputAction.CallbackContext context)
        {
            currentState.ApplyJump();
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