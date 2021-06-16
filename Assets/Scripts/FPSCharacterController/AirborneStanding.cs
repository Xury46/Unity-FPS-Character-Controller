using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public class AirborneStanding : MovementState
    {
        public AirborneStanding(FPSController controller) : base(controller) {}
        
        public override void OnStateEnter()
        {
            Debug.Log("I'm in the air!");
        }

        public override void OnGroundedCheckFailed() {}

        public override void ApplyJump() {}

        /*
        public override void OnStateUpdate(){}
        public override void OnStateExit(){}
        */
    }
}