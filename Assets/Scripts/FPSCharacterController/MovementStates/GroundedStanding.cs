using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class GroundedStanding : MovementState
    {
        public GroundedStanding(FPSController controller, FPSControllerSettings settings) : base(controller, settings) {}

        // public override void OnStateEnter(){}
        // public override void OnStateUpdate(){}
        // public override void OnStateExit(){}

        public override void OnGroundedCheckFailed() => controller.ChangeState(controller.airborneStanding);

        public override void OnCrouch() => controller.ChangeState(controller.groundedCrouching);
    }
}