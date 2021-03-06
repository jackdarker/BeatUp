﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Helpers;
using Game.Effects;

namespace Game {
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : AIControllerBase {
        protected static PlayerController s_Instance;
        public static PlayerController instance { get { return s_Instance; } }

        public bool respawning { get { return m_Respawning; } }

        public float idleTimeout = 5f;            // How long before pawn starts considering random idles.
        public bool canAttack;                    // Whether or not pawn can swing her staff.

        //Todo public CameraSettings cameraSettings;            // Reference used to determine the camera's direction.
        public Transform CameraSideView;
        public bool SideView = true;
        public MeleeWeapon meleeWeapon;                  // Reference used to (de)activate weapon when attacking. 
        public RandomAudioPlayer footstepPlayer;         // Random Audio Players used for various situations.
        public RandomAudioPlayer landingPlayer;
        public RandomAudioPlayer emoteLandingPlayer;
        public RandomAudioPlayer emoteDeathPlayer;
        public RandomAudioPlayer emoteAttackPlayer;
        public RandomAudioPlayer emoteJumpPlayer;

        protected AnimatorStateInfo m_CurrentStateInfo;    // Information about the base layer of the animator cached.
        protected AnimatorStateInfo m_NextStateInfo;
        protected bool m_IsAnimatorTransitioning;
        protected AnimatorStateInfo m_PreviousCurrentStateInfo;    // Information about the base layer of the animator from last frame.
        protected AnimatorStateInfo m_PreviousNextStateInfo;
        protected bool m_PreviousIsAnimatorTransitioning;

        protected PlayerInput m_Input;                 // Reference used to determine how pawn should move.
        protected CharacterController m_CharCtrl;      // Reference used to actually move pawn.
      //  protected Animator m_Animator;                 // Reference used to make decisions based on Ellen's current animation and to set parameters.
        protected Material m_CurrentWalkingSurface;    // Reference used to make decisions about audio.
        protected Quaternion m_TargetRotation;         // What rotation Ellen is aiming to have based on input.
        protected float m_AngleDiff;                   // Angle in degrees between Ellen's current rotation and her target rotation.
        protected Collider[] m_OverlapResult = new Collider[8];    // Used to cache colliders that are near Ellen.
        protected bool m_InAttack;                     // Whether Ellen is currently in the middle of a melee attack.
        protected bool m_InCombo;                      // Whether Ellen is currently in the middle of her melee combo.
        protected Damageable m_Damageable;             // Reference used to set invulnerablity and health based on respawning.
        protected Renderer[] m_Renderers;              // References used to make sure Renderers are reset properly. 
        protected Checkpoint m_CurrentCheckpoint;      // Reference used to reset Ellen to the correct position on respawn.
        protected bool m_Respawning;                   // Whether Ellen is currently respawning.
        protected float m_IdleTimer;                   // Used to count up to Ellen considering a random idle.

        

        // Parameters

        
        readonly int m_HashEllenCombo1 = Animator.StringToHash("EllenCombo1");
        readonly int m_HashEllenCombo2 = Animator.StringToHash("EllenCombo2");
        readonly int m_HashEllenCombo3 = Animator.StringToHash("EllenCombo3");
        readonly int m_HashEllenCombo4 = Animator.StringToHash("EllenCombo4");
        readonly int m_HashEllenDeath = Animator.StringToHash("Death");

        // Tags
        readonly int m_HashBlockInput = Animator.StringToHash("BlockInput");
        //public List<MonoBehaviour> onDamageMessageReceivers;
        //public UnityEvent OnDeath, OnReceiveDamage, OnResetDamage;
        
        protected bool IsMoveInput {
            get { return !Mathf.Approximately(m_Input.MoveInput.sqrMagnitude, 0f); }
        }

        public void SetCanAttack(bool canAttack) {
            this.canAttack = canAttack;
        }

        // Called automatically by Unity when the script is first added to a gameobject or is reset from the context menu.
        void Reset() {
            meleeWeapon = GetComponentInChildren<MeleeWeapon>();

            Transform footStepSource = transform.Find("FootstepSource");
            if (footStepSource != null)
                footstepPlayer = footStepSource.GetComponent<RandomAudioPlayer>();

            Transform hurtSource = transform.Find("HurtSource");
            /*if (hurtSource != null)
                hurtAudioPlayer = hurtSource.GetComponent<RandomAudioPlayer>();  ??
                */
            Transform landingSource = transform.Find("LandingSource");
            if (landingSource != null)
                landingPlayer = landingSource.GetComponent<RandomAudioPlayer>();

            
           // cameraSettings = FindObjectOfType<CameraSettings>();    //todo

           // if (cameraSettings != null) {
           //     if (cameraSettings.follow == null)
           //         cameraSettings.follow = transform;

           //     if (cameraSettings.lookAt == null)
           //         cameraSettings.follow = transform.Find("HeadTarget");
           // }
        }

        // Called automatically by Unity when the script first exists in the scene.
        void Awake() {
            m_Input = GetComponent<PlayerInput>();
            m_Animator = GetComponent<Animator>();
            m_CharCtrl = GetComponent<CharacterController>();

            if(meleeWeapon !=null)
                meleeWeapon.SetOwner(gameObject);

            s_Instance = this;
        }

        // Called automatically by Unity after Awake whenever the script is enabled. 
        void OnEnable() {
            //todo SceneLinkedSMB<PlayerController>.Initialise(m_Animator, this);

            m_Damageable = GetComponent<Damageable>();
            m_Damageable.onDamageMessageReceivers.Add(this);
            m_Damageable.SetPawn(this);

            m_Damageable.isInvulnerable = true;

            EquipMeleeWeapon(false);

            m_Renderers = GetComponentsInChildren<Renderer>();
        }

        // Called automatically by Unity whenever the script is disabled.
        void OnDisable() {
            m_Damageable.onDamageMessageReceivers.Remove(this);

            for (int i = 0; i < m_Renderers.Length; ++i) {
                m_Renderers[i].enabled = true;
            }
        }

        // Called automatically by Unity once every Physics step.
        void FixedUpdate() {
            CacheAnimatorState();

            UpdateInputBlocking();

            EquipMeleeWeapon(IsWeaponEquiped());

            m_Animator.SetFloat(m_HashStateTime, Mathf.Repeat(m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f));
            //m_Animator.ResetTrigger(m_HashMeleeAttack);

            if (m_Input.Attack && canAttack)
                m_Animator.SetTrigger(m_HashPunchAttack);
            if (m_Input.Attack2 && canAttack)
                m_Animator.SetTrigger(m_HashKickAttack);

            CalculateForwardMovement();
            CalculateVerticalMovement();

            SetTargetRotation();

            if (IsOrientationUpdated() && IsMoveInput)
                UpdateOrientation();

            PlayAudio();

            TimeoutToIdle();

            m_PreviouslyGrounded = m_IsGrounded;
        }

        // Called at the start of FixedUpdate to record the current state of the base layer of the animator.
        void CacheAnimatorState() {
            m_PreviousCurrentStateInfo = m_CurrentStateInfo;
            m_PreviousNextStateInfo = m_NextStateInfo;
            m_PreviousIsAnimatorTransitioning = m_IsAnimatorTransitioning;

            m_CurrentStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            m_NextStateInfo = m_Animator.GetNextAnimatorStateInfo(0);
            m_IsAnimatorTransitioning = m_Animator.IsInTransition(0);
        }

        // Called after the animator state has been cached to determine whether this script should block user input.
        void UpdateInputBlocking() {
            bool inputBlocked = m_CurrentStateInfo.tagHash == m_HashBlockInput && !m_IsAnimatorTransitioning;
            inputBlocked |= m_NextStateInfo.tagHash == m_HashBlockInput;
            m_Input.playerControllerInputBlocked = inputBlocked;
        }

        // Called after the animator state has been cached to determine whether or not the staff should be active or not.
        bool IsWeaponEquiped() {
            return true; //Todo
            bool equipped = m_NextStateInfo.shortNameHash == m_HashEllenCombo1 || m_CurrentStateInfo.shortNameHash == m_HashEllenCombo1;
            equipped |= m_NextStateInfo.shortNameHash == m_HashEllenCombo2 || m_CurrentStateInfo.shortNameHash == m_HashEllenCombo2;
            equipped |= m_NextStateInfo.shortNameHash == m_HashEllenCombo3 || m_CurrentStateInfo.shortNameHash == m_HashEllenCombo3;
            equipped |= m_NextStateInfo.shortNameHash == m_HashEllenCombo4 || m_CurrentStateInfo.shortNameHash == m_HashEllenCombo4;

            return equipped;
        }

        // Called each physics step with a parameter based on the return value of IsWeaponEquiped.
        void EquipMeleeWeapon(bool equip) {
            if (meleeWeapon != null)
                meleeWeapon.gameObject.SetActive(equip);
            m_InAttack = false;
            m_InCombo = equip;

            if (!equip)
                m_Animator.ResetTrigger(m_HashPunchAttack);
        }

        // Called each physics step.
        protected void CalculateForwardMovement() {
            // Cache the move input and cap it's magnitude at 1.
            Vector2 moveInput = m_Input.MoveInput;
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();
            // Calculate the speed intended by input.
             m_DesiredForwardSpeed = moveInput.magnitude * maxForwardSpeed;
            moveInput = transform.InverseTransformDirection(moveInput);
            m_TurnAmount = Mathf.Atan2(moveInput.y, moveInput.x);
            // Determine change to speed based on whether there is currently any move input.
            float acceleration = IsMoveInput ? k_GroundAcceleration : k_GroundDeceleration;

            // Adjust the forward speed towards the desired speed.
            m_ForwardSpeed = Mathf.MoveTowards(m_ForwardSpeed, m_DesiredForwardSpeed, acceleration * Time.deltaTime);

            // Set the animator parameter to control what animation is being played.
            m_Animator.SetFloat(m_HashForwardSpeed, m_ForwardSpeed);
           // m_Animator.SetFloat(m_HashTurn, m_TurnAmount);
        }

        // Called each physics step.
        protected void CalculateVerticalMovement() {
            // If jump is not currently held and Ellen is on the ground then she is ready to jump.
            if (!m_Input.JumpInput && m_IsGrounded)
                m_ReadyToJump = true;

            if (m_IsGrounded) {
                // When grounded we apply a slight negative vertical speed to make Ellen "stick" to the ground.
                m_VerticalSpeed = -gravity * k_StickingGravityProportion;

                // If jump is held, Ellen is ready to jump and not currently in the middle of a melee combo...
                if (m_Input.JumpInput && m_ReadyToJump && !m_InCombo) {
                    // ... then override the previously set vertical speed and make sure she cannot jump again.
                    m_VerticalSpeed = jumpSpeed;
                    m_IsGrounded = false;
                    m_ReadyToJump = false;
                }
            } else {
                // If Ellen is airborne, the jump button is not held and Ellen is currently moving upwards...
                if (!m_Input.JumpInput && m_VerticalSpeed > 0.0f) {
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

        // Called each physics step to set the rotation Pawn is aiming to have.
        void SetTargetRotation() {
            // Create three variables, move input local to the player, flattened forward direction of the camera and a local target rotation.
            Vector2 moveInput = m_Input.MoveInput;

            Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            //todo   Vector3 forward = Quaternion.Euler(0f, cameraSettings.Current.m_XAxis.Value, 0f) * Vector3.forward;
            Vector3 forward= Vector3.forward;
            forward = Quaternion.FromToRotation(forward, CameraSideView.forward) * Vector3.forward;
            forward.y = 0f;
            forward.Normalize();

            Quaternion targetRotation;

            // If the local movement direction is the opposite of forward then the target rotation should be towards the camera.
            if (Mathf.Approximately(Vector3.Dot(localMovementDirection, Vector3.forward), -1.0f)) {
                targetRotation = Quaternion.LookRotation(-forward);
            } else {
                // Otherwise the rotation should be the offset of the input from the camera's forward.
                Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
                targetRotation = Quaternion.LookRotation(cameraToInputOffset * forward);
            }

            // The desired forward direction of Ellen.
            Vector3 resultingForward = targetRotation * Vector3.forward;

            // If attacking try to orient to close enemies.
            if (m_InAttack) {
                // Find all the enemies in the local area.
                Vector3 centre = transform.position + transform.forward * 2.0f + transform.up;
                Vector3 halfExtents = new Vector3(3.0f, 1.0f, 2.0f);
                int layerMask = 1 << LayerMask.NameToLayer("Enemy");
                int count = Physics.OverlapBoxNonAlloc(centre, halfExtents, m_OverlapResult, targetRotation, layerMask);

                // Go through all the enemies in the local area...
                float closestDot = 0.0f;
                Vector3 closestForward = Vector3.zero;
                int closest = -1;

                for (int i = 0; i < count; ++i) {
                    // ... and for each get a vector from the player to the enemy.
                    Vector3 playerToEnemy = m_OverlapResult[i].transform.position - transform.position;
                    playerToEnemy.y = 0;
                    playerToEnemy.Normalize();

                    // Find the dot product between the direction the player wants to go and the direction to the enemy.
                    // This will be larger the closer to Ellen's desired direction the direction to the enemy is.
                    float d = Vector3.Dot(resultingForward, playerToEnemy);

                    // Store the closest enemy.
                    if (d > k_MinEnemyDotCoeff && d > closestDot) {
                        closestForward = playerToEnemy;
                        closestDot = d;
                        closest = i;
                    }
                }

                // If there is a close enemy...
                if (closest != -1) {
                    // The desired forward is the direction to the closest enemy.
                    resultingForward = closestForward;

                    // We also directly set the rotation, as we want snappy fight and orientation isn't updated in the UpdateOrientation function during an atatck.
                    transform.rotation = Quaternion.LookRotation(resultingForward);
                }
            }

            // Find the difference between the current rotation of the player and the desired rotation of the player in radians.
            float angleCurrent = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan2(resultingForward.x, resultingForward.z) * Mathf.Rad2Deg;

            m_AngleDiff = 0;// Mathf.DeltaAngle(angleCurrent, targetAngle);
            m_TargetRotation = targetRotation;
        }

        // Called each physics step to help determine whether Ellen can turn under player input.
        bool IsOrientationUpdated() {
            bool updateOrientationForLocomotion = !m_IsAnimatorTransitioning && m_CurrentStateInfo.shortNameHash == m_HashLocomotion || m_NextStateInfo.shortNameHash == m_HashLocomotion;
            bool updateOrientationForAirborne = !m_IsAnimatorTransitioning && m_CurrentStateInfo.shortNameHash == m_HashAirborne || m_NextStateInfo.shortNameHash == m_HashAirborne;
            bool updateOrientationForLanding = !m_IsAnimatorTransitioning && m_CurrentStateInfo.shortNameHash == m_HashLanding || m_NextStateInfo.shortNameHash == m_HashLanding;

            return updateOrientationForLocomotion || updateOrientationForAirborne || updateOrientationForLanding || m_InCombo && !m_InAttack;
        }

        // Called each physics step after SetTargetRotation if there is move input and Ellen is in the correct animator state according to IsOrientationUpdated.
        void UpdateOrientation() {
           //?? m_Animator.SetFloat(m_HashAngleDeltaRad, m_AngleDiff * Mathf.Deg2Rad);
            m_Animator.SetFloat(m_HashTurn, m_AngleDiff * Mathf.Deg2Rad);
            Vector3 localInput = new Vector3(m_Input.MoveInput.x, 0f, m_Input.MoveInput.y);
            float groundedTurnSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, m_ForwardSpeed / m_DesiredForwardSpeed);
            float actualTurnSpeed = m_IsGrounded ? groundedTurnSpeed : Vector3.Angle(transform.forward, localInput) * k_InverseOneEighty * k_AirborneTurnSpeedProportion * groundedTurnSpeed;
            m_TargetRotation = Quaternion.RotateTowards(transform.rotation, m_TargetRotation, actualTurnSpeed * Time.deltaTime);

            transform.rotation = m_TargetRotation;
        }

        // Called each physics step to check if audio should be played and if so instruct the relevant random audio player to do so.
        void PlayAudio() {
            return; //todo
            float footfallCurve = m_Animator.GetFloat(m_HashFootFall);

            if (footfallCurve > 0.01f && !footstepPlayer.playing && footstepPlayer.canPlay) {
                footstepPlayer.playing = true;
                footstepPlayer.canPlay = false;
                footstepPlayer.PlayRandomClip(m_CurrentWalkingSurface, m_ForwardSpeed < 4 ? 0 : 1);
            } else if (footstepPlayer.playing) {
                footstepPlayer.playing = false;
            } else if (footfallCurve < 0.01f && !footstepPlayer.canPlay) {
                footstepPlayer.canPlay = true;
            }

            if (m_IsGrounded && !m_PreviouslyGrounded) {
                landingPlayer.PlayRandomClip(m_CurrentWalkingSurface, bankId: m_ForwardSpeed < 4 ? 0 : 1);
                emoteLandingPlayer.PlayRandomClip();
            }

            if (!m_IsGrounded && m_PreviouslyGrounded && m_VerticalSpeed > 0f) {
                emoteJumpPlayer.PlayRandomClip();
            }

            if (m_CurrentStateInfo.shortNameHash == m_HashHurt && m_PreviousCurrentStateInfo.shortNameHash != m_HashHurt) {
                hitAudio.PlayRandomClip();
            }

            if (m_CurrentStateInfo.shortNameHash == m_HashEllenDeath && m_PreviousCurrentStateInfo.shortNameHash != m_HashEllenDeath) {
                emoteDeathPlayer.PlayRandomClip();
            }

            if (m_CurrentStateInfo.shortNameHash == m_HashEllenCombo1 && m_PreviousCurrentStateInfo.shortNameHash != m_HashEllenCombo1 ||
                m_CurrentStateInfo.shortNameHash == m_HashEllenCombo2 && m_PreviousCurrentStateInfo.shortNameHash != m_HashEllenCombo2 ||
                m_CurrentStateInfo.shortNameHash == m_HashEllenCombo3 && m_PreviousCurrentStateInfo.shortNameHash != m_HashEllenCombo3 ||
                m_CurrentStateInfo.shortNameHash == m_HashEllenCombo4 && m_PreviousCurrentStateInfo.shortNameHash != m_HashEllenCombo4) {
                emoteAttackPlayer.PlayRandomClip();
            }
        }

        // Called each physics step to count up to the point where Ellen considers a random idle.
        void TimeoutToIdle() {
            return; //Todo
            bool inputDetected = IsMoveInput || m_Input.Attack || m_Input.JumpInput;
            if (m_IsGrounded && !inputDetected) {
                m_IdleTimer += Time.deltaTime;

                if (m_IdleTimer >= idleTimeout) {
                    m_IdleTimer = 0f;
                    m_Animator.SetTrigger(m_HashTimeoutToIdle);
                }
            } else {
                m_IdleTimer = 0f;
                m_Animator.ResetTrigger(m_HashTimeoutToIdle);
            }

            m_Animator.SetBool(m_HashInputDetected, inputDetected);
        }

        // Called each physics step (so long as the Animator component is set to Animate Physics) after FixedUpdate to override root motion.
        void OnAnimatorMove() {
            Vector3 movement;

            // If Ellen is on the ground...
            if (m_IsGrounded) {
                // ... raycast into the ground...
                RaycastHit hit;
                Ray ray = new Ray(transform.position + Vector3.up * k_GroundedRayDistance * 0.5f, -Vector3.up);
                if (Physics.Raycast(ray, out hit, k_GroundedRayDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
                    // ... and get the movement of the root motion rotated to lie along the plane of the ground.
                   //todo movement = Vector3.ProjectOnPlane(m_Animator.deltaPosition, hit.normal);
                    movement = m_ForwardSpeed * transform.forward * Time.deltaTime;

                    // Also store the current walking surface so the correct audio is played.
                    Renderer groundRenderer = hit.collider.GetComponentInChildren<Renderer>();
                    m_CurrentWalkingSurface = groundRenderer ? groundRenderer.sharedMaterial : null;
                } else {
                    // If no ground is hit just get the movement as the root motion.
                    // Theoretically this should rarely happen as when grounded the ray should always hit.
                    //todo movement = m_Animator.deltaPosition;
                    movement = m_ForwardSpeed * transform.forward * Time.deltaTime;
                    m_CurrentWalkingSurface = null;
                }
            } else {
                // If not grounded the movement is just in the forward direction.
                movement = m_ForwardSpeed * transform.forward * Time.deltaTime;
            }

            // Rotate the transform of the character controller by the animation's root rotation.
            m_CharCtrl.transform.rotation *= m_Animator.deltaRotation;
            
            // Add to the movement with the calculated vertical speed.
            movement += m_VerticalSpeed * Vector3.up * Time.deltaTime;
            // Move the character controller.
            m_CharCtrl.Move(movement);

            // After the movement store whether or not the character controller is grounded.
            m_IsGrounded = m_CharCtrl.isGrounded;

            // If Ellen is not on the ground then send the vertical speed to the animator.
            // This is so the vertical speed is kept when landing so the correct landing animation is played.
            if (!m_IsGrounded)
                m_Animator.SetFloat(m_HashAirborneVerticalSpeed, m_VerticalSpeed);

            // Send whether or not Ellen is on the ground to the animator.
            m_Animator.SetBool(m_HashGrounded, m_IsGrounded);
        }

        // This is called by an animation event when attack animations are played. 
        //You have to create those events and connect them to the funtion in the Rig-Animation-Import panel!
        public void MeleeAttackStart(int throwing = 0) {
            if (meleeWeapon != null)
                meleeWeapon.BeginAttack(throwing != 0);
            m_InAttack = true;
        }

        // This is called by an animation event when when attack animations are finishing.
        public void MeleeAttackEnd() {
            if (meleeWeapon != null)
                meleeWeapon.EndAttack();
            m_InAttack = false;
        }

        // This is called by Checkpoints to make sure Ellen respawns correctly.
        public void SetCheckpoint(Checkpoint checkpoint) {
            if (checkpoint != null)
                m_CurrentCheckpoint = checkpoint;
        }

        // This is usually called by a state machine behaviour on the animator controller but can be called from anywhere.
        public void Respawn() {
            StartCoroutine(RespawnRoutine());
        }

        protected IEnumerator RespawnRoutine() {
            // Wait for the animator to be transitioning from the EllenDeath state.
            //while (m_CurrentStateInfo.shortNameHash != m_HashEllenDeath || !m_IsAnimatorTransitioning) {
           //     yield return null;
           // }

            // Wait for the screen to fade out.
            yield return StartCoroutine(ScreenFader.FadeSceneOut());
            while (ScreenFader.IsFading) {
                yield return null;
            }

            // Enable spawning.
            //todo EllenSpawn spawn = GetComponentInChildren<EllenSpawn>();
            //spawn.enabled = true;

            // If there is a checkpoint, move Ellen to it.
            if (m_CurrentCheckpoint != null) {
                transform.position = m_CurrentCheckpoint.transform.position;
                transform.rotation = m_CurrentCheckpoint.transform.rotation;
            } else {
                Debug.LogError("There is no Checkpoint set, there should always be a checkpoint set. Did you add a checkpoint at the spawn?");
            }

            // Get Ellen's health back.
            m_Damageable.ResetDamage();
            m_PawnStats.ResetPlayerStats();

            // Set the Respawn parameter of the animator.
            m_Animator.SetTrigger(m_HashRespawn);

            // Start the respawn graphic effects.
            // todo spawn.StartEffect();

            // Wait for the screen to fade in.
            // Currently it is not important to yield here but should some changes occur that require waiting until a respawn has finished this will be required.
            yield return StartCoroutine(ScreenFader.FadeSceneIn());
        }

        // Called by a state machine behaviour on Ellen's animator controller.
        public void RespawnFinished() {
            m_Respawning = false;
            m_Damageable.isInvulnerable = false;
            OnDamage.Invoke(GetHealth()); //update HUD
        }

        // Called by OnReceiveMessage.
        public override void Damaged(Damageable.DamageMessage damageMessage) {
            m_PawnStats.Damage(damageMessage);
            if (GetHealth() <= 0) {
                Die(damageMessage);
                return;
            }
            // Set the Hurt parameter of the animator.
            m_Animator.SetTrigger(m_HashHurt);

            // Find the direction of the damage.
            Vector3 forward = damageMessage.damageSource - transform.position;
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

    }
}