using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class GroundedCrouching : MovementState
    {
        public GroundedCrouching(FPSController controller, FPSControllerSettings settings) : base(controller, settings)
        {
            height_Target = settings.height_Crouching;
        }

        // public override void OnStateEnter(){}
        // public override void OnStateUpdate(){}
        // public override void OnStateExit(){}

        public override void OnGroundedCheckPassed() {}

        public override void Crouch() {}
    }
}