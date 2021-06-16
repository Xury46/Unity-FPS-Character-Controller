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

        public LayerMask groundedCheckLayers;

        // Camera Pitch
        public float pitch_Current = 0.0f;
        public float pitch_Max = 90.0f;
        public float pitch_Min = -90.0f;
        public bool pitch_invert = false;
        
        // Camera Yaw
        public float yaw_Current = 0.0f;
        
        public float lookSensitivity = 0.25f;


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
            playerCamera.transform.position = transform.position + transform.up * cameraVerticalOffset; // Make the detatched camera follow the position of the player
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

        public void Look(InputAction.CallbackContext context)
        {
            Vector2 inputLook = context.ReadValue<Vector2>();
            currentState.Look(inputLook);
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