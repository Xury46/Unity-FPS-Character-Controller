using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class GroundedCrouching : MovementState
    {
        public GroundedCrouching(FPSController controller, FPSControllerSettings settings) : base(controller, settings)
        {
            lateralFriction_Target = settings.lateralFriction_Grounded;
            height_Target = settings.height_Crouching;
            moveSpeed_Target = settings.moveSpeed_Crouching;
        }

        // public override void OnStateEnter(){}
        // public override void OnStateUpdate(){}
        // public override void OnStateExit(){}

        public override void OnGroundedCheckFailed() => controller.ChangeState(controller.airborneCrouching);

        public override void OnStand() => controller.ChangeState(controller.groundedStanding);
    }
}