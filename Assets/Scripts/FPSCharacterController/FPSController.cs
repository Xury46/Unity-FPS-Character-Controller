using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSCharacterController
{
    public class FPSController : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;
        [HideInInspector] public Rigidbody playerRB;

        private GroundedStanding groundedStanding;

        private MovementState currentState;

        private void Awake()
        {
            playerRB = GetComponent<Rigidbody>();

            groundedStanding = new GroundedStanding(this);

            currentState = groundedStanding;
        }

        // Update is called once per frame
        void Update()
        {
            currentState.OnStateUpdate();
        }
        
        public void LateralInput(InputAction.CallbackContext context)
        {
            Vector2 inputMovement = context.ReadValue<Vector2>();
            currentState.LateralMovement(new Vector3(inputMovement.x, 0.0f, inputMovement.y));
        }

        public void ChangeState(MovementState stateToChangeTo)
        {
            currentState.OnStateExit();
            currentState = stateToChangeTo;
            currentState.OnStateEnter();
        }
    }
}