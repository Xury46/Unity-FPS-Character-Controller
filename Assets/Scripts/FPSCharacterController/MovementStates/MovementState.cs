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

        Vector3 lateralCollisionImpulses;

        public virtual void OnStateFixedUpdate()
        {
            lateralCollisionImpulses = Vector3.zero;

            foreach (Collision collision in controller.collisionsAppliedLastFixedUpdate)
            {
                Debug.Log(collision.collider.name + ": " + collision.impulse);

                Vector3 localImpulseVector = controller.transform.InverseTransformDirection(collision.impulse);
                localImpulseVector = new Vector3(localImpulseVector.x, 0.0f, localImpulseVector.z);
                localImpulseVector = controller.transform.TransformDirection(localImpulseVector);
                lateralCollisionImpulses += localImpulseVector;
            }

            CalculateLocalVelocityVectors();
            
            ApplyYaw();
            
            ApplyLateralMovement();
            ApplyLateralFriction();
            
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

        Vector3 movementCancelVector_World;
        
        public void ApplyLateralMovement_Air()
        {
            Vector3 lateralVel_World = controller.transform.TransformDirection(controller.localLateralVelocity);
            Vector3 lateralWish_World = controller.transform.TransformDirection(settings.lateralMoveVector * settings.moveSpeed_Current);
            
            Vector3 projectedVelOntoWish_World = Vector3.Project(lateralVel_World, lateralWish_World); // Project the current velocity onto the wish direction.

            Vector3 MovementVecotorToAdd = projectedVelOntoWish_World - lateralVel_World; // Build a vector from the current velocity to the requested velocity.

            controller.playerRB.AddForce(MovementVecotorToAdd, ForceMode.VelocityChange);
        }

/*
        // AIRMOVE STUFF
        void CalculateDrag(Vector3 direction, Vector3 velocity) {
            dotP = Vector3.Dot(velocity.normalized, direction);
            if (dotP < 0) {
                dotP *= -1;
                drag = dotP * dragMulti * (1 - dotP * 0.9f);
                direction = direction + velocity.normalized * 0.1f;
                body.AddForce(direction * drag * Time.deltaTime, ForceMode.Acceleration);
            }
        }
*/

        public void ApplyLateralMovement()
        {
            float lateralVelMag = controller.transform.TransformDirection(controller.localLateralVelocity).magnitude;
            Vector3 lateralMoveVector_World = controller.transform.TransformDirection(settings.lateralMoveVector * settings.moveSpeed_Current);
            float lateralWishMag = lateralMoveVector_World.magnitude;

            float minMoveSpeed = 0.001f;

            if (lateralVelMag < minMoveSpeed && settings.lateralMoveVector == Vector3.zero) controller.playerRB.velocity = Vector3.zero;

            float speedFactor = (settings.moveSpeed_Current - lateralVelMag) / settings.moveSpeed_Current;

            float currentMoveForce = 100.0f;
            
            //controller.playerRB.AddForce(speedFactor * currentMoveForce * lateralMoveVector_World, ForceMode.VelocityChange);
            controller.playerRB.AddForce(speedFactor * currentMoveForce * lateralMoveVector_World, ForceMode.Force);


            /*

            if (projectedVelOntoWish_World.magnitude > settings.moveSpeed_Current)
            {

            }

            */





            /*
            if (controller.lateralVelocityDelta_World == Vector3.zero) Debug.Log("It's a match!");
            else Debug.Log("It's not a match, delta is: " + controller.lateralVelocityDelta_World);
            */

            //Vector3 movementCancelVector_World = controller.lateralForceAddedLastFixedUpdate_World - controller.lateralVelocityDelta_World; // Shortent the cancel vector based on the delta
            //Vector3 movementCancelVector_World = controller.lateralForceAddedLastFixedUpdate_World; // Shorten the cancel vector based on the delta
            
            
            //movementCancelVector_World = -controller.totalPredictedLateralVelocity_World;
            //movementCancelVector_World = -controller.lateralForceAddedLastFixedUpdate_World;
            //Vector3 movementCancelVector_World = -controller.lateralVelocityprojected_World; // THIS WAS ALMOST WORKING
            
            
 //           Vector3 movementCancelVector_World = controller.lateralForceAddedLastFixedUpdate_World + lateralCollisionImpulses; // THIS WAS ALMOST WORKING
            
            
            // TODO LEAVE BEHIND THE REMAINEDER OF THE MAGNITUDE
            //Vector3 movementCancelVector_World = controller.totalPredictedLateralVelocity_World.normalized * (predictedMag - projectedMag);
            //Vector3 movementCancelVector_World = controller.lateralForceAddedLastFixedUpdate_World.normalized * (projectedMag - controller.lateralForceAddedLastFixedUpdate_World.normalized.magnitude);


/*

            //Vector3 movementCancelVector_World = controller.lateralForceAddedLastFixedUpdate_World - controller.lateralVelocityDelta_World; // Shortent the cancel vector based on the delta
            controller.playerRB.AddForce(-movementCancelVector_World, ForceMode.VelocityChange); // Apply opposite force to cancel previous movement.
            //controller.totalPredictedLateralVelocity_World += movementCancelVector_World;

            Vector3 movementForceToAdd = controller.transform.TransformDirection(settings.lateralMoveVector * settings.moveSpeed_Current); // Transform from local to world space;
            controller.playerRB.AddForce(movementForceToAdd, ForceMode.VelocityChange); // Apply opposite force to cancel previous movement, and add new movement force in world space
            //controller.totalPredictedLateralVelocity_World += movementForceToAdd; // Attempt to calculate what the velocity will be after adding the movement force.

            controller.lateralForceAddedLastFixedUpdate_World = movementForceToAdd; // Cache movement force so it can be used to cancel the movement next frame.

*/
        }

        public virtual void ApplyLateralFriction()
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