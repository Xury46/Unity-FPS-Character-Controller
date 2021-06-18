using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSCharacterController
{
    public abstract class MovementState
    {
        protected FPSController controller;
        protected FPSControllerSettings settings;
        protected FPSControllerSettings.HeightSettings height_Target;
        protected float lateralFriction;

        public MovementState(FPSController controller, FPSControllerSettings settings)
        {
            this.controller = controller;
            this.settings = settings;
        }

        public virtual void OnStateEnter(){}

        public virtual void OnStateUpdate()
        {
            if (settings.height_Current.cameraHeight != height_Target.cameraHeight) BlendHeight();
        }

        public virtual void OnStateFixedUpdate()
        {
            //if (controller.lateralMoveVector.magnitude > 0.0f) ApplyLateralMovement();
            //else ApplyLateralFriction();
            ApplyYaw();
            ApplyLateralMovement();
            ApplyLateralFriction();
            ApplyGravity();
            GroundedCheck();
        }

        public virtual void OnStateExit(){}

        public virtual void ApplyCameraLook()
        {
            controller.playerCamera.transform.localRotation = Quaternion.Euler(new Vector3(controller.pitch_Current, controller.yaw_Current, 0.0f));
        }

        public virtual void ApplyYaw()
        {
            controller.playerRB.MoveRotation(Quaternion.Euler(Vector3.up * controller.yaw_Current));
        }

        protected void BlendHeight()
        {
            float blendSpeed = 15.0f * Time.deltaTime;

            settings.height_Current.cameraHeight = Mathf.Lerp(settings.height_Current.cameraHeight, height_Target.cameraHeight, blendSpeed);
            settings.height_Current.capsuleHeight = Mathf.Lerp(settings.height_Current.capsuleHeight, height_Target.capsuleHeight, blendSpeed);

            controller.capsuleCollider.height = settings.height_Current.capsuleHeight;
            controller.capsuleCollider.center = Vector3.up * settings.height_Current.capsuleHeight * 0.5f;
        }

        public void ApplyLateralMovement()
        {
            Vector3 localSpaceLateralVector = controller.transform.TransformDirection(controller.lateralMoveVector);
            controller.playerRB.AddForce(localSpaceLateralVector, ForceMode.VelocityChange);
        }

        public virtual void ApplyLateralFriction()
        {
            Vector3 localSpaceLateralVelocity = controller.transform.TransformDirection(controller.playerRB.velocity);
            localSpaceLateralVelocity = new Vector3(localSpaceLateralVelocity.x , 0.0f, localSpaceLateralVelocity.z);
            localSpaceLateralVelocity = controller.transform.InverseTransformDirection(localSpaceLateralVelocity);
            
            controller.playerRB.AddForce(-localSpaceLateralVelocity * lateralFriction * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }

        public virtual void ApplyJump()
        {
            controller.playerRB.AddForce(controller.transform.up * controller.jumpForce, ForceMode.VelocityChange);
        }

        public virtual void ApplyGravity()
        {
            float gravityStrength = 20.0f; // 9.81f;
            Vector3 gravityVector = -controller.transform.up * gravityStrength;
            
            controller.playerRB.AddForce(gravityVector, ForceMode.Acceleration);
        }

        public void GroundedCheck()
        {
            float radius = 0.45f;
            float verticalOffset = -0.05f;
            verticalOffset += radius * 0.5f;
            Vector3 center = controller.transform.position + (controller.transform.up * verticalOffset);
            if (Physics.CheckSphere(center, radius, controller.groundedCheckLayers, QueryTriggerInteraction.Ignore)) OnGroundedCheckPassed();
            else OnGroundedCheckFailed();
        }

        public virtual void OnGroundedCheckPassed() {}

        public virtual void OnGroundedCheckFailed() {}

        public virtual void OnCrouch() {}
        
        public virtual void OnStand() {}
    }
}