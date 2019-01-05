using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Game.Helpers;
using Game.Effects;

namespace Game {
    public class AIControllerBase: MonoBehaviour, IMessageReceiver {

        // common Animator-Parameters
        public readonly int m_HashAirborneVerticalSpeed = Animator.StringToHash("AirborneVerticalSpeed");
        public readonly int m_HashForwardSpeed = Animator.StringToHash("Forward");
        public readonly int m_HashTurn = Animator.StringToHash("Turn");    //rotationspeed; -1= rotate left full speed
        public readonly int m_HashAngleDeltaRad = Animator.StringToHash("AngleDeltaRad");  //??
        public readonly int m_HashTimeoutToIdle = Animator.StringToHash("TimeoutToIdle");  //??
        public readonly int m_HashGrounded = Animator.StringToHash("OnGround");    //is touching ground
        public readonly int m_HashInputDetected = Animator.StringToHash("InputDetected");  //??
        public readonly int m_HashPunchAttack = Animator.StringToHash("Punch");    //trigger for combat
        public readonly int m_HashKickAttack = Animator.StringToHash("Kick");    //trigger for combat
        public readonly int m_HashHurt = Animator.StringToHash("Hurt");    //trigger if hit
        public readonly int m_HashDeath = Animator.StringToHash("Death");  //trigger on death
        public readonly int m_HashRespawn = Animator.StringToHash("Respawn");  //??
        public readonly int m_HashHurtFromX = Animator.StringToHash("HurtFromX");  //x-direction of hit
        public readonly int m_HashHurtFromY = Animator.StringToHash("HurtFromY");  //y-direction of hit
        public readonly int m_HashStateTime = Animator.StringToHash("StateTime");  //how long the state is already active
        public readonly int m_HashFootFall = Animator.StringToHash("FootFall");    //??

        // States
        public readonly int m_HashLocomotion = Animator.StringToHash("Grounded"); // Locomotion");
        public readonly int m_HashAirborne = Animator.StringToHash("Airborne");
        public readonly int m_HashLanding = Animator.StringToHash("Landing");    // Also a parameter.

        protected Animator m_Animator;
        public AI.PlayerProperties Stats { get { return m_PawnStats; } }
        protected AI.PlayerProperties m_PawnStats = new AI.PlayerProperties();

        protected bool m_IsGrounded = true;            // Whether or not Ellen is currently standing on the ground.
        protected bool m_PreviouslyGrounded = true;    // Whether or not Ellen was standing on the ground last frame.
        protected bool m_ReadyToJump;                  // Whether or not the input state and Ellen are correct to allow jumping.
        protected float m_DesiredForwardSpeed;         // How fast Ellen aims be going along the ground based on input.
        protected float m_ForwardSpeed;                // How fast Ellen is currently going along the ground.
        protected float m_TurnAmount;
        protected float m_VerticalSpeed;               // How fast Ellen is currently moving up or down.

        public RagdollManager m_RagDoll;

        [Header("Audio")]
        public RandomAudioPlayer hitAudio;
        public RandomAudioPlayer deathAudio;

        //Todo as parameter
        public float maxForwardSpeed = 8f;        // How fast pawn can run.
        public float gravity = 20f;               // How fast pawn accelerates downwards when airborne.
        public float jumpSpeed = 10f;             // How fast pawn takes off when jumping.
        public float minTurnSpeed = 400f;         // How fast pawn turns when moving at maximum speed.
        public float maxTurnSpeed = 1200f;        // How fast pawn turns when stationary.

        // These constants are used to ensure pawn moves and behaves properly.
        protected const float k_AirborneTurnSpeedProportion = 5.4f;
        protected const float k_GroundedRayDistance = 1f;
        protected const float k_JumpAbortSpeed = 10f;
        protected const float k_MinEnemyDotCoeff = 0.2f;
        protected const float k_InverseOneEighty = 1f / 180f;
        protected const float k_StickingGravityProportion = 0.3f;
        protected const float k_GroundAcceleration = 20f;
        protected const float k_GroundDeceleration = 25f;

        [System.Serializable]
        public class DamageEvent : UnityEvent<float> {
        }
        public DamageEvent OnDamage;

        // Called by Pawn's Damageable when she is hurt.
        virtual public void OnReceiveMessage(MessageType type, object sender, object data) {
            switch (type) {
                case MessageType.DAMAGED: {
                        Damageable.DamageMessage damageData = (Damageable.DamageMessage)data;
                        Damaged(damageData);
                    }
                    break;
                case MessageType.DEAD: {
                        Damageable.DamageMessage damageData = (Damageable.DamageMessage)data;
                        Die(damageData);
                    }
                    break;
            }
        }

        public void CalculateForwardMovement(Vector3 move) {
            // Cache the move input and cap it's magnitude at 1.
            Vector2 moveInput = new Vector2(move.x, move.z);
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();
            // Calculate the speed intended by input.
            m_DesiredForwardSpeed = moveInput.magnitude * maxForwardSpeed;
            moveInput = transform.InverseTransformDirection(moveInput);
            m_TurnAmount = Mathf.Atan2(moveInput.y, moveInput.x);
            // Determine change to speed based on whether there is currently any move input.
            float acceleration = k_GroundAcceleration;
            if (Mathf.Approximately(moveInput.sqrMagnitude, 0f))
                acceleration = k_GroundDeceleration;


            // Adjust the forward speed towards the desired speed.
            m_ForwardSpeed = Mathf.MoveTowards(m_ForwardSpeed, m_DesiredForwardSpeed, acceleration * Time.deltaTime);

            // Set the animator parameter to control what animation is being played.
            m_Animator.SetFloat(m_HashForwardSpeed, m_ForwardSpeed);
            // m_Animator.SetFloat(m_HashTurn, m_TurnAmount);
        }
        protected void CalculateVerticalMovement() {

            if (m_IsGrounded) {
                // When grounded we apply a slight negative vertical speed to make Ellen "stick" to the ground.
                m_VerticalSpeed = -gravity * k_StickingGravityProportion;
            } else {
                // If Ellen is airborne, the jump button is not held and Ellen is currently moving upwards...
                if (/*!m_Input.JumpInput && */ m_VerticalSpeed > 0.0f) {
                    // ... decrease Ellen's vertical speed.
                    // This is what causes holding jump to jump higher that tapping jump.
                    m_VerticalSpeed -= k_JumpAbortSpeed * Time.deltaTime;
                }

                // If a jump is approximately peaking, make it absolute.
                if (Mathf.Approximately(m_VerticalSpeed, 0f)) {
                    m_VerticalSpeed = 0f;
                }

                // If Ellen is airborne, apply gravity.
                m_VerticalSpeed -= gravity * Time.deltaTime;
            }
        }


        virtual public void Damaged(Damageable.DamageMessage damageData) {
            m_PawnStats.Damage(damageData);
            if (GetHealth() <= 0) {
                Die(damageData);
                return;
            }
            // Set the Hurt parameter of the animator.
            m_Animator.SetTrigger(m_HashHurt);

            // Find the direction of the damage.
            Vector3 forward = damageData.damageSource - transform.position;
            forward.y = 0f;

            Vector3 localHurt = transform.InverseTransformDirection(forward);
            OnDamage.Invoke(GetHealth());

            // Set the HurtFromX and HurtFromY parameters of the animator based on the direction of the damage.
            m_Animator.SetFloat(m_HashHurtFromX, localHurt.x);
            m_Animator.SetFloat(m_HashHurtFromY, localHurt.z);

            // Shake the camera.
            //todo CameraShake.Shake(CameraShake.k_PlayerHitShakeAmount, CameraShake.k_PlayerHitShakeTime);

            // Play an audio clip of being hurt.
            if (hitAudio != null) {
                hitAudio.PlayRandomClip();
            }

        }
        virtual public void Die(Damageable.DamageMessage damageData) {
            m_Animator.SetTrigger(m_HashDeath);
            m_ForwardSpeed = 0f;
            m_VerticalSpeed = 0f;
            if (m_RagDoll != null) {
                m_RagDoll.Setup();
            }
            //We unparent the hit source, as it would destroy it with the gameobject when it get replaced by the ragdol otherwise
            if (deathAudio != null) {
                deathAudio.transform.SetParent(null, true);
                deathAudio.PlayRandomClip();
                GameObject.Destroy(deathAudio, deathAudio.clip == null ? 0.0f : deathAudio.clip.length + 0.5f);
            }

        }
        public float GetHealth() {
           return m_PawnStats.Health;
        }
    }
}
