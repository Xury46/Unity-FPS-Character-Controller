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
        protected float lateralFriction_Target;

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
            ApplyYaw();
            CalculateLocalVelocityVectors();
            
            ApplyLateralFriction();
            ApplyLateralMovement();
            
            ApplyGravity();
            GroundedCheck();
        }

        public virtual void OnStateExit(){}

        public virtual void ApplyCameraLook()
        {
            Vector3 rotationVector = new Vector3(controller.pitch_Current, controller.yaw_Current, 0.0f);
            controller.playerCamera.transform.localRotation = Quaternion.Euler(rotationVector);
        }

        public virtual void ApplyYaw()
        {
            float yaw_Delta = controller.yaw_Current - controller.yaw_Previous;

            if (yaw_Delta != 0.0f) controller.playerRB.MoveRotation(controller.playerRB.rotation * Quaternion.Euler(Vector3.up * yaw_Delta));
            
            controller.yaw_Previous = controller.yaw_Current; // Cache previous yaw
        }

        private void StateTransition()
        {
            // Progress through the transition with a 0-1 lerp value
            if (settings.stateTransition_Duration <= 0.0f) settings.stateTransition_Progress = 1.0f; // Avoid deviding by zero and skip to the end of the transition.
            else settings.stateTransition_Progress = Mathf.Clamp(settings.stateTransition_Progress + (Time.deltaTime / settings.stateTransition_Duration), 0.0f, 1.0f);
            
            // Blend camera height, capsule height and center offset
            settings.height_Current.cameraHeight = Mathf.Lerp(settings.height_Current.cameraHeight, height_Target.cameraHeight, settings.stateTransition_Progress);
            settings.height_Current.capsuleHeight = Mathf.Lerp(settings.height_Current.capsuleHeight, height_Target.capsuleHeight, settings.stateTransition_Progress);

            controller.capsuleCollider.height = settings.height_Current.capsuleHeight;
            controller.capsuleCollider.center = Vector3.up * settings.height_Current.capsuleHeight * 0.5f;

            // Blend MoveSpeed
            settings.moveSpeed_Current = Mathf.Lerp(settings.moveSpeed_Current, moveSpeed_Target, settings.stateTransition_Progress);
            
            // Blend LateralFriction
            settings.lateralFriction_Current = Mathf.Lerp(settings.lateralFriction_Current, lateralFriction_Target, settings.stateTransition_Progress);
        }

        protected void CalculateLocalVelocityVectors()
	    {
            // Calculate how fast the player is moving along its local lateral axes.
		    Vector3 localVelocity = controller.transform.InverseTransformDirection(controller.playerRB.velocity); // Convert the vector to local space

            controller.lateralVelocity = new Vector3(localVelocity.x, 0.0f, localVelocity.z); // Remove the y component of the local velocity vector;
            controller.verticalVelocity = Vector3.up * localVelocity.y; // Remove the x, and z components of the local velocity vector;

            //lateralVelocity = controller.transform.TransformDirection(lateralVelocity); // Convert the lateral velocity vector back to world space
            //verticalVelocity = controller.transform.TransformDirection(verticalVelocity); // Convert the vertical velocity vector back to world space
        }

        public void ApplyLateralMovement()
        {
            controller.playerRB.AddForce(-controller.movementForceCached, ForceMode.VelocityChange); // Apply opposite force to cancel previous movement in world space
            
            Vector3 movementForceToAdd = controller.transform.TransformDirection(settings.lateralMoveVector * settings.moveSpeed_Current); // Transform from local to world space
            
            controller.playerRB.AddForce(movementForceToAdd, ForceMode.VelocityChange); // Add new movement force in world space
            
            controller.movementForceCached = movementForceToAdd; // Cache movement force so it can be used to cancel the movement next frame.

            
            
            // Shortent the movementCanceling vector if it would make the magnitude longer than before
            // if ((controller.lateralVelocity - cachedMovementForce).magnitude >

            /*
            float accelerationSpeed = 100.0f;

            Vector3 potentialVelocity = controller.lateralVelocity + settings.lateralMoveVector;

            // TODO complete this logic to redirect or shorten the vector
            if (potentialVelocity.magnitude <= settings.moveSpeed_Current)
            {
                controller.playerRB.AddRelativeForce(settings.lateralMoveVector * accelerationSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
            else
            {
                // Change direction
                controller.playerRB.velocity = controller.lateralVelocity.magnitude * controller.transform.TransformDirection(settings.lateralMoveVector.normalized);
            }
            */
        }

        public virtual void ApplyLateralFriction()
        {   
            Vector3 frictionToAdd = controller.transform.TransformDirection(controller.lateralVelocity); // Transform from local to world space
            
            frictionToAdd -= controller.movementForceCached; // Remove the cached movement force from the current lateral velocity so that we only take into account the player's velocity from external sources
            
            controller.playerRB.AddForce(-frictionToAdd * settings.lateralFriction_Current * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }

        public virtual void ApplyJump()
        {
            controller.playerRB.AddForce(controller.transform.up * settings.jumpForce, ForceMode.VelocityChange);
        }

        public virtual void ApplyGravity()
        {
            Vector3 gravityVector = settings.gravityDirection * settings.gravityStrength;            
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

        public virtual void OnRun() {}
        
        public virtual void OnWalk() {}
        
        public virtual void OnCrouch() {}
        
        public virtual void OnStand() {}
    }
}