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

        Vector3 velocityToSet_World;
        Vector3 lateralCollisionImpulses;

        public virtual void OnStateFixedUpdate()
        {
            velocityToSet_World = Vector3.zero;
            
            lateralCollisionImpulses = Vector3.zero;

            foreach (Collision collision in controller.collisionsAppliedLastFixedUpdate)
            {
                //Debug.Log(collision.collider.name + ": " + collision.impulse);

                /*

                Vector3 localImpulseVector = controller.transform.InverseTransformDirection(collision.impulse);
                localImpulseVector = new Vector3(localImpulseVector.x, 0.0f, localImpulseVector.z);
                localImpulseVector = controller.transform.TransformDirection(localImpulseVector);
                lateralCollisionImpulses += localImpulseVector;
                */
            }

            CalculateLocalVelocityVectors();
            
            ApplyYaw();
            
            AddLateralMovement();
            
            ApplyGravity();

            ApplyVelocity();
            
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

        float predictedMag;
        float projectedMag;

        protected void CalculateLocalVelocityVectors()
	    {
            // Calculate how fast the player is moving along its local lateral axes.
		    Vector3 localVelocity = controller.transform.InverseTransformDirection(controller.playerRB.velocity); // Convert the vector to local space
            controller.localLateralVelocity = new Vector3(localVelocity.x, 0.0f, localVelocity.z); // Remove the y component of the local velocity vector;
            controller.localVerticalVelocity = Vector3.up * localVelocity.y; // Remove the x, and z components of the local velocity vector;

            predictedMag = controller.totalPredictedLateralVelocity_World.magnitude;
            
            //controller.lateralVelocityprojected_World = Vector3.Project(controller.totalPredictedLateralVelocity_World, controller.transform.TransformDirection(controller.localLateralVelocity));
            controller.lateralVelocityprojected_World = Vector3.Project(controller.lateralForceAddedLastFixedUpdate_World, controller.transform.TransformDirection(controller.localLateralVelocity));

            projectedMag = controller.lateralVelocityprojected_World.magnitude;

            controller.lateralVelocityDelta_World = controller.totalPredictedLateralVelocity_World - controller.transform.TransformDirection(controller.localLateralVelocity); // What is the difference in lateral velocity between the prediction of last FixedUpdate and the result of the last FixedUpdate;
            
            
            controller.totalPredictedLateralVelocity_World = controller.transform.TransformDirection(controller.localLateralVelocity); // Reset the total velocity prediction to match the lateral velocity at the start of the FixedUpdate
        }

        Vector3 lateralMoveVector_World_Previous;

        public void AddLateralMovement()
        {
            Vector3 lateralMoveVector_World_Request = controller.transform.TransformDirection(settings.lateralMoveVector * settings.moveSpeed_Current); // Get the requested lateral movement vector in world space
            Vector3 lateralVelocity_World_Current = controller.transform.TransformDirection(controller.localLateralVelocity); // Get the current lateral movement velocity vector in world space

            float lateralMoveVector_World_Request_Magnitute = lateralMoveVector_World_Request.magnitude; // How fast is the requested movement vector.
            float lateralVelocity_World_Current_Magnitute = lateralVelocity_World_Current.magnitude; // How fast is the current lateral velocity.

            float minMoveMag = 0.0001f;
            if (lateralMoveVector_World_Request_Magnitute > minMoveMag) settings.moveSpeedRampUpOrDown = 1.0f;
            else settings.moveSpeedRampUpOrDown = -1.0f;

            float rampGoal = (settings.moveSpeedRampUpOrDown + 1.0f) / 2.0f; // Convert from -1 - 1 to 0 - 1         
            if (settings.moveSpeedRampUpRate <= 0.0f) settings.moveSpeedRampUpMultiplier = rampGoal; // Avoid dividing by zero and ramp up to full speed immediately
            else if (settings.moveSpeedRampUpMultiplier != rampGoal) settings.moveSpeedRampUpMultiplier = Mathf.Clamp(settings.moveSpeedRampUpMultiplier + (Time.fixedDeltaTime / settings.moveSpeedRampUpRate) * settings.moveSpeedRampUpOrDown, 0.0f, 1.0f);


/*
            if (lateralMoveVector_World_Request_Magnitute > minMoveMag)
            {
                
                //float dot = Vector3.Dot(lateralMoveVector_World_Request.normalized, lateralVelocity_World_Current.normalized);
                //Debug.Log(dot);
            }
*/


            // Apply friction to the lateral velocity to slow it down over time.
            // Don't go lower than the requested magnitute, and don't go faster than the lateral velocity prior to the friction.
            lateralVelocity_World_Current_Magnitute = Mathf.Clamp(lateralVelocity_World_Current_Magnitute - (settings.lateralFriction_Current * Time.fixedDeltaTime), lateralMoveVector_World_Request_Magnitute * settings.moveSpeedRampUpMultiplier, lateralVelocity_World_Current_Magnitute);

            // The new lateral velocity magnitude will be the greater of the two: Requested speed vs Current Speed (after friction is applied).
            // This will slow down the player without reducing below the requested speed.
            // TODO slow down the move speed if the requested vector is in the opposite direction.
            float newMagnitute = Mathf.Max(lateralMoveVector_World_Request_Magnitute * settings.moveSpeedRampUpMultiplier, lateralVelocity_World_Current_Magnitute);

            // Change direction to the requested vector
            // TODO blend smoothly to change direction instead of snapping instantly
            Vector3 newDir = lateralMoveVector_World_Request.magnitude <= minMoveMag ? lateralVelocity_World_Current : lateralMoveVector_World_Request;

            velocityToSet_World = newDir.normalized * newMagnitute;
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

        private void ApplyVelocity()
        {
            Vector3 verticalVelocity_World_Current = controller.transform.TransformDirection(controller.localVerticalVelocity);
            velocityToSet_World += verticalVelocity_World_Current;

            controller.playerRB.velocity = velocityToSet_World;
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