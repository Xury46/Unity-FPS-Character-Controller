using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class AirborneStanding : MovementState
    {
        public AirborneStanding(FPSController controller, FPSControllerSettings settings) : base(controller, settings) {}
        
        // public override void OnStateEnter(){}
        // public override void OnStateUpdate(){}
        // public override void OnStateExit(){}

        public override void OnGroundedCheckFailed() {}
        
        public override void Crouch() {}
        
        public override void Stand() {}

        public override void ApplyJump() {}
    }
}