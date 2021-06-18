using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class AirborneCrouching : MovementState
    {
        public AirborneCrouching(FPSController controller, FPSControllerSettings settings) : base(controller, settings)
        {
            lateralFriction = settings.lateralFriction_Airborne;
            height_Target = settings.height_Crouching;
        }

        // public override void OnStateEnter(){}
        // public override void OnStateUpdate(){}
        // public override void OnStateExit(){}

        public override void OnGroundedCheckPassed() => controller.ChangeState(controller.groundedCrouching);
        
        public override void OnStand() => controller.ChangeState(controller.airborneStanding);

        public override void ApplyJump() {} // Can't jump while airborne
    }
}