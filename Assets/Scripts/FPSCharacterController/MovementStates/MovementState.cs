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
                Debug.Log(collision.collider.name + ": " + collision.impulse);

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
            AddLateralFriction();
            
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
            Vector3 lateralMoveVector_World_Request = controller.transform.TransformDirection(settings.lateralMoveVector * settings.moveSpeed_Current);
            Vector3 lateralVelocity_World_Current = controller.transform.TransformDirection(controller.localLateralVelocity);

            float lateralMoveVector_World_Request_Magnitute = lateralMoveVector_World_Request.magnitude; // How fast would we like to go.
            float lateralVelocity_World_Current_Magnitute = lateralVelocity_World_Current.magnitude; // How fast are we currently going.

            // If the current lateral velocity is slower than the requested move speed, top speed should be the request, otherwise it should be the velocity so we don't slow down
            //float TopSpeed = lateralVelocity_World_Current_Magnitute < settings.moveSpeed_Current ? settings.moveSpeed_Current : lateralVelocity_World_Current_Magnitute;

            lateralVelocity_World_Current_Magnitute = Mathf.Clamp(lateralVelocity_World_Current_Magnitute - (settings.lateralFriction_Current * Time.fixedDeltaTime), lateralMoveVector_World_Request_Magnitute, lateralVelocity_World_Current_Magnitute);


            float newMagnitute = Mathf.Max(lateralMoveVector_World_Request_Magnitute, lateralVelocity_World_Current_Magnitute);


            //Vector3 newLateralVel = Vector3.ClampMagnitude(lateralMoveVector_World_Request, TopSpeed);
            Vector3 newDir = lateralMoveVector_World_Request.magnitude <= 0.0001f ? lateralVelocity_World_Current : lateralMoveVector_World_Request;             
            
            
            
            //Vector3 newLateralVel = newDir.normalized * TopSpeed;
            Vector3 newLateralVel = newDir.normalized * newMagnitute;

            //if (TopSpeed > lateralVelocity_World_Current.magnitude)

            velocityToSet_World = newLateralVel;
        }

        public virtual void AddLateralFriction()
        {   
            /*
            //Vector3 frictionToAdd = controller.totalPredictedLateralVelocity_World - movementCancelVector_World;
            Vector3 frictionToAdd = controller.totalPredictedLateralVelocity_World;
            //frictionToAdd *= settings.lateralFriction_Current * Time.fixedDeltaTime;
            controller.playerRB.AddForce(-frictionToAdd, ForceMode.VelocityChange);
            controller.totalPredictedLateralVelocity_World -= frictionToAdd;
            */
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