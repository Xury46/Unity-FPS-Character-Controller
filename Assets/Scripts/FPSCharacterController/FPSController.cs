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
        [HideInInspector] public Rigidbody playerRB;

        public LayerMask groundedCheckLayers;

        // Camera Pitch
        public float cameraPitch_Current = 0.0f;
        public float cameraPitch_Max = 90.0f;
        public float cameraPitch_Min = -90.0f;

        public bool invertCameraPitch = false;
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
        }

        // Update is called once per frame
        void Update()
        {
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