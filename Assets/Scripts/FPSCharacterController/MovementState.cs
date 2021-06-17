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
            //ApplyLook();
            ApplyLateralMovement();
            CheckIfGrounded();
        }
        public virtual void OnStateExit(){}

        public virtual void OnGroundedCheckPassed() => controller.ChangeState(controller.groundedStanding);
        public virtual void OnGroundedCheckFailed() => controller.ChangeState(controller.airborneStanding);
        

        public virtual void ApplyLook()
        {
            controller.playerCamera.transform.localRotation = Quaternion.Euler(new Vector3(controller.pitch_Current, controller.yaw_Current, 0.0f));
            controller.transform.rotation = Quaternion.Euler(Vector3.up * controller.yaw_Current);
        }

        //public void ApplyLook()
        //{
            //controller.playerRB.MoveRotation(Quaternion.Euler(controller.transform.up * controller.yaw_Current));
        //}

        public void ApplyLateralMovement()
        {
            Vector3 localSpaceLateralVector = controller.transform.TransformDirection(controller.lateralMoveVector);
            controller.playerRB.AddForce(localSpaceLateralVector, ForceMode.VelocityChange);
        }

        public virtual void ApplyJump()
        {
            controller.playerRB.AddForce(controller.transform.up * controller.jumpForce, ForceMode.VelocityChange);
        }

        public void CheckIfGrounded()
        {
            float radius = 0.95f;
            float verticalOffset = -0.05f;
            verticalOffset += radius * 0.5f;
            Vector3 center = controller.transform.position + (controller.transform.up * verticalOffset);
            if (Physics.CheckSphere(center, radius, controller.groundedCheckLayers, QueryTriggerInteraction.Ignore)) OnGroundedCheckPassed();
            else OnGroundedCheckFailed();
        }
    }
}