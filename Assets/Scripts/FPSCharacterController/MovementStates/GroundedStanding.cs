using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class GroundedStanding : MovementState
    {
        public GroundedStanding(FPSController controller) : base(controller) {}

        public override void OnGroundedCheckPassed() {}

        public override void OnStateEnter()
        {
            Debug.Log("I'm on the ground!");
        }

        /*
        public override void OnStateEnter(){}
        public override void OnStateUpdate(){}
        public override void OnStateExit(){}
        */
    }
}