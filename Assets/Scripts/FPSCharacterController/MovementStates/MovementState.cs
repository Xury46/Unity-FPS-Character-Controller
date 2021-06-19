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
        protected float moveSpeed_Target;
        protected float lateralFriction;

        public MovementState(FPSController controller, FPSControllerSettings settings)
        {
            this.controller = controller;
            this.settings = settings;
        }

        public virtual void OnStateEnter()
        {
            settings.stateTransition_Progress = 0.0f; // Reset the state transition progress
        }

        public virtual void OnStateUpdate()
        {
            if (settings.stateTransition_Progress < 1.0f) StateTransition();
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

        private void StateTransition()
        {
            // Progress through the transition with a 0-1 lerp value
            if (settings.stateTransition_Duration <= 0.0f) settings.stateTransition_Progress = 1.0f; // Avoid deviding by zero and skip to the end of the transition.
            settings.stateTransition_Progress = Mathf.Clamp(settings.stateTransition_Progress + (Time.deltaTime / settings.stateTransition_Duration), 0.0f, 1.0f);
            
            // Blend camera height, capsule height and center offset
            settings.height_Current.cameraHeight = Mathf.Lerp(settings.height_Current.cameraHeight, height_Target.cameraHeight, settings.stateTransition_Progress);
            settings.height_Current.capsuleHeight = Mathf.Lerp(settings.height_Current.capsuleHeight, height_Target.capsuleHeight, settings.stateTransition_Progress);

            controller.capsuleCollider.height = settings.height_Current.capsuleHeight;
            controller.capsuleCollider.center = Vector3.up * settings.height_Current.capsuleHeight * 0.5f;

            // Blend MoveSpeed
            settings.moveSpeed_Current = Mathf.Lerp(settings.moveSpeed_Current, moveSpeed_Target, settings.stateTransition_Progress);
        }

        public void ApplyLateralMovement()
        {
            Vector3 localSpaceLateralVector = controller.transform.TransformDirection(settings.lateralMoveVector);
            controller.playerRB.AddForce(localSpaceLateralVector * settings.moveSpeed_Current * Time.fixedDeltaTime, ForceMode.VelocityChange);
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
            controller.playerRB.AddForce(controller.transform.up * settings.jumpForce, ForceMode.VelocityChange);
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