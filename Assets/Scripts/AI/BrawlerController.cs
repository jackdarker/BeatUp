using UnityEngine;
using System.Collections;
using Game.Helpers;
using Game.Effects;
using UnityEngine.AI;

namespace Game.AI {




    //////   OBSOLETE ////////////




        /// <summary>
        /// A controller for a humanoid attacker
        /// </summary>  
    //this assure it's runned before any behaviour that may use it, as the animator need to be fecthed
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class BrawlerController : AIControllerBase {

        public Animator animator { get { return m_Animator; } }
        public Vector3 externalForce { get { return m_ExternalForce; } }
        public NavMeshAgent navmeshAgent { get { return m_NavMeshAgent; } }
        public bool followNavmeshAgent { get { return m_FollowNavmeshAgent; } }
        public bool grounded { get { return m_Grounded; } }
        public bool interpolateTurning = false;
        public bool applyAnimationRotation = false;

        protected NavMeshAgent m_NavMeshAgent;
        protected bool m_FollowNavmeshAgent;
        protected Animator m_Animator;
        protected bool m_UnderExternalForce;
        protected bool m_ExternalForceAddGravity = true;
        protected Vector3 m_ExternalForce;
        protected bool m_Grounded;
        protected CharacterController m_CharCtrl;                  // 

        const float k_GroundedRayDistance = .8f;

        void Reset() {

        }
        // Called automatically by Unity when the script first exists in the scene.
        void Awake() {
            m_Animator = GetComponent<Animator>();
            m_CharCtrl = GetComponent<CharacterController>();

        }
        // Called automatically by Unity after Awake whenever the script is enabled. 
        void OnEnable() {
            //todo SceneLinkedSMB<PlayerController>.Initialise(m_Animator, this);
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_Animator = GetComponent<Animator>();
            m_Animator.updateMode = AnimatorUpdateMode.AnimatePhysics;

            m_NavMeshAgent.updatePosition = false;
            
            m_FollowNavmeshAgent = true;
            m_PawnStats.ResetPlayerStats();
        }

        // Called automatically by Unity whenever the script is disabled.
        void OnDisable() {
            

        }
        private void FixedUpdate() {
            //stop all movement if player cant move
            animator.speed = PlayerInput.Instance != null && PlayerInput.Instance.HaveControl() ? 1.0f : 0.0f;

            CheckGrounded();

            if (m_UnderExternalForce)
                ForceMovement();
        }
        void ForceMovement() {
            if (m_ExternalForceAddGravity)
                m_ExternalForce += Physics.gravity * Time.deltaTime;

            RaycastHit hit;
            Vector3 movement = m_ExternalForce * Time.deltaTime;
            Vector3 p1 = transform.position + m_CharCtrl.center;

            if (!Physics.SphereCast(p1, m_CharCtrl.height / 2, movement, out hit, movement.sqrMagnitude)) {
                transform.Translate( movement);
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

            if (m_FollowNavmeshAgent) {
                m_NavMeshAgent.speed = (m_Animator.deltaPosition / Time.deltaTime).magnitude;
                transform.position = m_NavMeshAgent.nextPosition;
            } else {
                RaycastHit hit;
                Vector3 p1 = transform.position + m_CharCtrl.center;
                if (!Physics.SphereCast(p1, m_CharCtrl.height / 2, 
                    m_Animator.deltaPosition.normalized, out hit, m_Animator.deltaPosition.sqrMagnitude)) {
                    transform.Translate(m_Animator.deltaPosition);
                }

            }

            if (applyAnimationRotation) {
                transform.forward = m_Animator.deltaRotation * transform.forward;
            }
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

        
    }
}
