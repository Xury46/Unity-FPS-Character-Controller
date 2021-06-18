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

        public override void OnGroundedCheckPassed() {}

        public override void Stand() {}
    }
}