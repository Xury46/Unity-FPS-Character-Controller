using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class AirborneStanding : MovementState
    {
        public AirborneStanding(FPSController controller, FPSControllerSettings settings) : base(controller, settings)
        {
            lateralFriction_Target = settings.lateralFriction_Airborne;
            height_Target = settings.height_Standing;
            moveSpeed_Target = settings.moveSpeed_Walking;
        }

        // public override void OnStateEnter(){}
        // public override void OnStateUpdate(){}
        // public override void OnStateExit(){}

        public override void OnGroundedCheckPassed() => controller.ChangeState(controller.groundedStanding);

        public override void ApplyJump() {} // Can't jump while airborne
    }
}