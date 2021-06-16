using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public abstract class MovementState
    {
        FPSController controller;

        public MovementState(FPSController controller)
        {
            this.controller = controller;
        }

        public virtual void OnStateEnter(){}
        public virtual void OnStateUpdate(){}
        public virtual void OnStateFixedUpdate()
        {
            ApplyLateralMovement();
            CheckIfGrounded();
        }
        public virtual void OnStateExit(){}

        public virtual void OnGroundedCheckPassed() => controller.ChangeState(controller.groundedStanding);
        public virtual void OnGroundedCheckFailed() => controller.ChangeState(controller.airborneStanding);
        

        public virtual void Look(Vector2 lookVector)
        {
            int invert = controller.invertCameraPitch ? -1 : 1;
            controller.cameraPitch_Current += (lookVector.y * invert * controller.lookSensitivity);
            controller.cameraPitch_Current = Mathf.Clamp(controller.cameraPitch_Current, controller.cameraPitch_Min, controller.cameraPitch_Max);
            controller.playerCamera.transform.localRotation = Quaternion.Euler(Vector3.left * controller.cameraPitch_Current);

            controller.transform.localRotation *= Quaternion.Euler(Vector3.up * lookVector.x * controller.lookSensitivity);
        }

        public void ApplyLateralMovement()
        {
            Vector3 localSpaceLateralVector = controller.transform.TransformDirection(controller.lateralMoveVector);
            controller.playerRB.AddForce(localSpaceLateralVector, ForceMode.VelocityChange);
        }

        public void CheckIfGrounded()
        {
            float radius = 0.95f;
            float verticalOffset = -0.05f;
            Vector3 center = controller.transform.position - (controller.transform.up * verticalOffset);
            if (Physics.CheckSphere(center, radius, controller.groundedCheckLayers, QueryTriggerInteraction.Ignore)) OnGroundedCheckPassed();
            else OnGroundedCheckFailed();
        }
    }
}