using Game.Helpers;
using Game.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Game.AI {
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class BrawlerBehaviour :  AIControllerBase, IMessageReceiver {
        public static readonly int hashInPursuit = Animator.StringToHash("InPursuit");
        public static readonly int hashAttack = Animator.StringToHash("Punch");
        public static readonly int hashHit = Animator.StringToHash("Hurt");
        public static readonly int hashVerticalDot = Animator.StringToHash("VerticalHitDot");
        public static readonly int hashHorizontalDot = Animator.StringToHash("HorizontalHitDot");
        public static readonly int hashThrown = Animator.StringToHash("Thrown");
        public static readonly int hashGrounded = Animator.StringToHash("OnGround");
        public static readonly int hashVerticalVelocity = Animator.StringToHash("AirborneVerticalSpeed");
        public static readonly int hashSpotted = Animator.StringToHash("Spotted");
        public static readonly int hashNearBase = Animator.StringToHash("NearBase");

        public static readonly int hashIdleState = Animator.StringToHash("Idle");

        //public BrawlerController controller { get { return m_Controller; } }
        public Animator animator { get { return m_Animator; } }
        public Vector3 externalForce { get { return m_ExternalForce; } }
        public NavMeshAgent navmeshAgent { get { return m_NavMeshAgent; } }
        public bool followNavmeshAgent { get { return m_FollowNavmeshAgent; } }
        public bool grounded { get { return m_Grounded; } }
        public bool interpolateTurning = false;
        public bool applyAnimationRotation = false;

        protected NavMeshAgent m_NavMeshAgent;
        protected bool m_FollowNavmeshAgent;
    //    protected Animator m_Animator;
        protected bool m_UnderExternalForce;
        protected bool m_ExternalForceAddGravity = true;
        protected Vector3 m_ExternalForce;
        protected bool m_Grounded;
        protected CharacterController m_CharCtrl;

        const float k_GroundedRayDistance = .8f;
        public PlayerController target { get { return m_Target; } }
        public TargetDistributor.TargetFollower followerData { get { return m_FollowerInstance; } }
        public FloatingEnergyBar HUD_Health; 
        public Vector3 originalPosition { get; protected set; }
        [System.NonSerialized]
        public float attackDistance = 1; //Todo distance depends on weapon & animation ?!

        public MeleeWeapon meleeWeapon;
        public Transform healthBarPos;
        public TargetScanner playerScanner;
        [Tooltip("Time in seconde before the pawn stop pursuing the player when the player is out of sight")]
        public float timeToStopPursuit;

        [Header("Audio")]
        public RandomAudioPlayer attackAudio;
        public RandomAudioPlayer frontStepAudio;
        public RandomAudioPlayer backStepAudio;
        public RandomAudioPlayer gruntAudio;
        public RandomAudioPlayer spottedAudio;

        protected float m_TimerSinceLostTarget = 0.0f;
        protected Damageable m_Damageable;
        protected PlayerController m_Target = null;
        //protected BrawlerController m_Controller;
        protected TargetDistributor.TargetFollower m_FollowerInstance = null;

        protected void OnEnable() {
            //m_Controller = GetComponentInChildren<BrawlerController>();
            //todo SceneLinkedSMB<PlayerController>.Initialise(m_Animator, this);
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_Animator = GetComponent<Animator>();
            m_CharCtrl = GetComponent<CharacterController>();
            m_Animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
            m_NavMeshAgent.updatePosition = false;
            m_FollowNavmeshAgent = true; //??
            m_PawnStats.ResetPlayerStats();
            m_Damageable = GetComponent<Damageable>();
            if (m_Damageable != null) {
                m_Damageable.onDamageMessageReceivers.Add(this);
                m_Damageable.isInvulnerable = true;
                m_Damageable.SetPawn(this);
            }
            originalPosition = transform.position;

            meleeWeapon.SetOwner(gameObject);

            //m_Animator.Play(hashIdleState, 0, Random.value);
            
            if(HUD_Health!=null)
                HUD_Health.Init(healthBarPos, this);
            //UI.ManagerUI_HUD.instance.CreateEnemyFloaters(healthBarPos, this);
            SceneLinkedSMB<BrawlerBehaviour>.Initialise(m_Animator, this);
        }

        /// <summary>
        /// Called by animation events.
        /// </summary>
        /// <param name="frontFoot">Has a value of 1 when it's a front foot stepping and 0 when it's a back foot.</param>
        void PlayStep(int frontFoot) {
            if (frontStepAudio != null && frontFoot == 1)
                frontStepAudio.PlayRandomClip();
            else if (backStepAudio != null && frontFoot == 0)
                backStepAudio.PlayRandomClip();
        }

        /// <summary>
        /// Called by animation events.
        /// </summary>
        public void Grunt() {
            if (gruntAudio != null)
                gruntAudio.PlayRandomClip();
        }

        public void Spotted() {
            if (spottedAudio != null)
                spottedAudio.PlayRandomClip();
        }

        protected void OnDisable() {
            if (m_FollowerInstance != null)
                m_FollowerInstance.distributor.UnregisterFollower(m_FollowerInstance);
            if (m_Damageable != null)
                m_Damageable.onDamageMessageReceivers.Remove(this);
        }

        private void FixedUpdate() {
            //CalculateForwardMovement();
            CalculateVerticalMovement();

            Vector3 toBase = originalPosition - transform.position;
            toBase.y = 0;
            animator.speed = PlayerInput.Instance != null && PlayerInput.Instance.HaveControl() ? 1.0f : 0.0f;

            CheckGrounded();
            //m_Animator.SetBool(hashGrounded, grounded);
            if (m_UnderExternalForce)
                ForceMovement();
            //m_Controller.animator.SetBool(hashNearBase, toBase.sqrMagnitude < 0.1 * 0.1f);
        }
        void ForceMovement() {
            if (m_ExternalForceAddGravity)
                m_ExternalForce += Physics.gravity * Time.deltaTime;

            RaycastHit hit;
            Vector3 movement = m_ExternalForce * Time.deltaTime;
            Vector3 p1 = transform.position + m_CharCtrl.center;

            if (!Physics.SphereCast(p1, m_CharCtrl.height / 2, movement, out hit, movement.sqrMagnitude)) {
                transform.Translate(movement);
            }

            m_NavMeshAgent.Warp(transform.position);
        }
        void CheckGrounded() {
            RaycastHit hit;
            Ray ray = new Ray(transform.position + Vector3.up * k_GroundedRayDistance * 0.5f, -Vector3.up);
            m_Grounded = Physics.Raycast(ray, out hit, k_GroundedRayDistance, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
        }
        private void OnAnimatorMove() {
            if (m_UnderExternalForce)
                return;
            Vector3 movement;
            Vector3 PlaneMovement;
            if (m_IsGrounded) {
                // ... raycast into the ground...
                RaycastHit hit;
                Ray ray = new Ray(transform.position + Vector3.up * k_GroundedRayDistance * 0.5f, -Vector3.up);
                if (Physics.Raycast(ray, out hit, k_GroundedRayDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
                    // ... and get the movement of the root motion rotated to lie along the plane of the ground.
                    //todo movement = Vector3.ProjectOnPlane(m_Animator.deltaPosition, hit.normal);
                    movement = m_ForwardSpeed * transform.forward * Time.deltaTime;

                } else {
                    // If no ground is hit just get the movement as the root motion.
                    // Theoretically this should rarely happen as when grounded the ray should always hit.
                    //todo movement = m_Animator.deltaPosition;
                    movement = m_ForwardSpeed * transform.forward * Time.deltaTime;
                }
            } else {
                // If not grounded the movement is just in the forward direction.
                movement = m_ForwardSpeed * transform.forward * Time.deltaTime;
            }
            if (applyAnimationRotation) {  // Rotate the transform of the character controller by the animation's root rotation.
                m_CharCtrl.transform.rotation *= m_Animator.deltaRotation;
            }
            PlaneMovement = movement;
            // Add to the movement with the calculated vertical speed.
            movement += m_VerticalSpeed * Vector3.up * Time.deltaTime;

            if (m_FollowNavmeshAgent) { //movement done by NavMeashAgent
                m_NavMeshAgent.speed = movement.magnitude/Time.deltaTime;// (m_Animator.deltaPosition / Time.deltaTime).magnitude;
                transform.position = m_NavMeshAgent.nextPosition;
            } else {    //
                RaycastHit hit;
                Vector3 p1 = transform.position + m_CharCtrl.center;
                /*if (!Physics.SphereCast(p1, m_CharCtrl.height / 2,
                    //m_Animator.deltaPosition.normalized, out hit, m_Animator.deltaPosition.sqrMagnitude
                    PlaneMovement.normalized, out hit, PlaneMovement.sqrMagnitude
                    ))*/ {
                    //transform.Translate(m_Animator.deltaPosition);
                    m_CharCtrl.Move(movement);
                }

            }

            // After the movement store whether or not the character controller is grounded.
            m_IsGrounded = true;//m_CharCtrl.isGrounded;
            if (!m_IsGrounded)
                m_Animator.SetFloat(m_HashAirborneVerticalSpeed, m_VerticalSpeed);

            m_Animator.SetBool(m_HashGrounded, m_IsGrounded);
        }
        // used to disable position being set by the navmesh agent, for case where we want the animation to move the enemy instead (e.g. Chomper attack)
        public void SetFollowNavmeshAgent(bool follow) {
            if (!follow && m_NavMeshAgent.enabled) {
                m_NavMeshAgent.ResetPath();
            } else if (follow && !m_NavMeshAgent.enabled) {
                m_NavMeshAgent.Warp(transform.position);
            }

            m_FollowNavmeshAgent = follow;
            m_NavMeshAgent.enabled = follow;
        }

        public void AddForce(Vector3 force, bool useGravity = true) {
            if (m_NavMeshAgent.enabled)
                m_NavMeshAgent.ResetPath();

            m_ExternalForce = force;
            m_NavMeshAgent.enabled = false;
            m_UnderExternalForce = true;
            m_ExternalForceAddGravity = useGravity;
        }

        public void ClearForce() {
            m_UnderExternalForce = false;
            m_NavMeshAgent.enabled = true;
        }

        public void SetForward(Vector3 forward) {
            Quaternion targetRotation = Quaternion.LookRotation(forward);

            if (interpolateTurning) {
                targetRotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    m_NavMeshAgent.angularSpeed * Time.deltaTime);
            }

            transform.rotation = targetRotation;
        }

        public void SetTarget(Vector3 position) {
            m_NavMeshAgent.destination = position;
        }
        public void FindTarget() {
            //we ignore height difference if the target was already seen
            PlayerController target = playerScanner.Detect(transform, m_Target == null);

            if (m_Target == null) {
                //we just saw the player for the first time, pick an empty spot to target around them
                if (target != null) {
                    m_Animator.SetTrigger(hashSpotted);
                    m_Target = target;
                    TargetDistributor distributor = target.GetComponentInChildren<TargetDistributor>();
                    if (distributor != null)
                        m_FollowerInstance = distributor.RegisterNewFollower();
                }
            } else {
                //we lost the target. But chomper have a special behaviour : they only loose the player scent if they move past their detection range
                //and they didn't see the player for a given time. Not if they move out of their detectionAngle. So we check that this is the case before removing the target
                if (target == null) {
                    m_TimerSinceLostTarget += Time.deltaTime;

                    if (m_TimerSinceLostTarget >= timeToStopPursuit) {
                        Vector3 toTarget = m_Target.transform.position - transform.position;

                        if (toTarget.sqrMagnitude > playerScanner.detectionRadius * playerScanner.detectionRadius) {
                            if (m_FollowerInstance != null)
                                m_FollowerInstance.distributor.UnregisterFollower(m_FollowerInstance);

                            //the target move out of range, reset the target
                            m_Target = null;
                        }
                    }
                } else {
                    if (target != m_Target) {
                        if (m_FollowerInstance != null)
                            m_FollowerInstance.distributor.UnregisterFollower(m_FollowerInstance);

                        m_Target = target;

                        TargetDistributor distributor = target.GetComponentInChildren<TargetDistributor>();
                        if (distributor != null)
                            m_FollowerInstance = distributor.RegisterNewFollower();
                    }

                    m_TimerSinceLostTarget = 0.0f;
                }
            }
        }

        public void StartPursuit() {
            if (m_FollowerInstance != null) {
                m_FollowerInstance.requireSlot = true;
                RequestTargetPosition();
            }

           //?? m_Controller.animator.SetBool(hashInPursuit, true);
        }

        public void StopPursuit() {
            if (m_FollowerInstance != null) {
                m_FollowerInstance.requireSlot = false;
            }
            CalculateForwardMovement(Vector3.zero);
            //?? m_Controller.animator.SetBool(hashInPursuit, false);
        }

        public void RequestTargetPosition() {
            Vector3 fromTarget = transform.position - m_Target.transform.position;
            fromTarget.y = 0;

            m_FollowerInstance.requiredPoint = m_Target.transform.position + fromTarget.normalized * attackDistance * 0.9f;
        }

        public void WalkBackToBase() {
            if (m_FollowerInstance != null)
                m_FollowerInstance.distributor.UnregisterFollower(m_FollowerInstance);
            m_Target = null;
            StopPursuit();
            SetTarget(originalPosition);
            SetFollowNavmeshAgent(true);
        }

        public void TriggerAttack() {
            m_Animator.SetTrigger(hashAttack);
        }

        // This is called by an animation event when attack animations are played. 
        //You have to create those events and connect them to the funtion in the Rig-Animation-Import panel!
        public void MeleeAttackStart(int throwing = 0) {
             if (meleeWeapon != null)
                 meleeWeapon.BeginAttack(throwing != 0);
             m_InAttack = true;
        }
        bool m_InAttack;
        // This is called by an animation event when when attack animations are finishing.
        public void MeleeAttackEnd() {
            if (meleeWeapon != null)
                meleeWeapon.EndAttack();
            m_InAttack = false;
        }

        public override void Die(Damageable.DamageMessage msg) {
            Vector3 pushForce = transform.position - msg.damageSource;

            pushForce.y = 0;

            transform.forward = -pushForce.normalized;
            AddForce(pushForce.normalized * 7.0f - Physics.gravity * 0.6f);

            m_Animator.SetTrigger(hashHit);
            m_Animator.SetTrigger(hashThrown);

            //We unparent the hit source, as it would destroy it with the gameobject when it get replaced by the ragdol otherwise
            if (deathAudio != null) {
                deathAudio.transform.SetParent(null, true);
                deathAudio.PlayRandomClip();
                GameObject.Destroy(deathAudio, deathAudio.clip == null ? 0.0f : deathAudio.clip.length + 0.5f);
            }
        }

        public override void Damaged(Damageable.DamageMessage msg) {
            //TODO : make that more generic, (e.g. move it to the MeleeWeapon code with a boolean to enable shaking of camera on hit?)
           //?? if (msg.damager.name == "Staff")
           //     CameraShake.Shake(0.06f, 0.1f);

            float verticalDot = Vector3.Dot(Vector3.up, msg.direction);
            float horizontalDot = Vector3.Dot(transform.right, msg.direction);

            Vector3 pushForce = transform.position - msg.damageSource;

            pushForce.y = 0;

            transform.forward = -pushForce.normalized;
            AddForce(pushForce.normalized * 5.5f, false);

            m_Animator.SetFloat(hashVerticalDot, verticalDot);
            m_Animator.SetFloat(hashHorizontalDot, horizontalDot);

            m_Animator.SetTrigger(hashHit);
            if (hitAudio != null)
                hitAudio.PlayRandomClip();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            playerScanner.EditorGizmo(transform);
        }
#endif
    }
}