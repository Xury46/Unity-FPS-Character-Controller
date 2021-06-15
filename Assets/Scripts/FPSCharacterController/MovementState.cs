using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public abstract class MovementState
    {
        FPSController controller;

        public MovementState(FPSController controller)
        {
            this.controller = controller;
        }

        public virtual void OnStateEnter(){}
        public virtual void OnStateUpdate(){}
        public virtual void OnStateExit(){}

        public virtual void LateralMovement(Vector3 moveVector)
        {
            controller.playerRB.AddForce(moveVector, ForceMode.Impulse);
        }
    }
}