using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class GroundedRunning : MovementState
    {
        public GroundedRunning(FPSController controller, FPSControllerSettings settings) : base(controller, settings)
        {
            lateralFriction = settings.lateralFriction_Grounded;
            height_Target = settings.height_Standing;
            moveSpeed_Target = settings.moveSpeed_Running;
        }

        // public override void OnStateEnter(){}
        // public override void OnStateUpdate(){}
        // public override void OnStateExit(){}

        public override void OnGroundedCheckFailed() => controller.ChangeState(controller.airborneStanding);

        public override void OnWalk() => controller.ChangeState(controller.groundedStanding);

        public override void OnCrouch() => controller.ChangeState(controller.groundedCrouching);
    }
}