using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Helpers;
using UnityEngine.Serialization;

namespace Game.Helpers {
    /// <summary>
    /// assign this to a gameobject to make it damageable
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Damageable : MonoBehaviour {


        public struct DamageMessage {
            public MonoBehaviour damager;
            public int amount;
            public Vector3 direction;
            public Vector3 damageSource;
            public bool throwing;

            public bool stopCamera;
        }
        public Collider m_Collider;
        private AIControllerBase m_Pawn;
      //  public int maxHitPoints;
        [Tooltip("Time that this gameObject is invulnerable for, after receiving damage.")]
        public float invulnerabiltyTime;


        [Tooltip("The angle from the which that damageable is hitable. Always in the world XZ plane, with the forward being rotate by hitForwardRoation")]
        [Range(0.0f, 360.0f)]
        public float hitAngle = 360.0f;
        [Tooltip("Allow to rotate the world forward vector of the damageable used to define the hitAngle zone")]
        [Range(0.0f, 360.0f)]
        [FormerlySerializedAs("hitForwardRoation")] //SHAME!
        public float hitForwardRotation = 360.0f;

        public bool isInvulnerable { get; set; }
     //   public int currentHitPoints { get; private set; }

        public UnityEvent OnDeath, OnReceiveDamage, OnHitWhileInvulnerable, OnBecomeVulnerable, OnResetDamage;

        [Tooltip("When this gameObject is damaged, these other gameObjects are notified.")]
        //??[EnforceType(typeof(Message.IMessageReceiver))]
        public List<MonoBehaviour> onDamageMessageReceivers;

        protected float m_timeSinceLastHit = 0.0f;
        

        System.Action schedule;

        void Start() {
            ResetDamage();
           //?? m_Collider = GetComponent<Collider>();
        }
        public void SetPawn(AIControllerBase Pawn) {
            m_Pawn = Pawn;
            ResetDamage();
        }
        void Update() {
            if (isInvulnerable) {
                m_timeSinceLastHit += Time.deltaTime;
                if (m_timeSinceLastHit > invulnerabiltyTime) {
                    m_timeSinceLastHit = 0.0f;
                    isInvulnerable = false;
                    OnBecomeVulnerable.Invoke();
                }
            }
        }

        public void ResetDamage() {
            //currentHitPoints = maxHitPoints;
            isInvulnerable = false;
            m_timeSinceLastHit = 0.0f;
            OnResetDamage.Invoke();
        }

        public void SetColliderState(bool enabled) {
            m_Collider.enabled = enabled;
        }

        public virtual void ApplyDamage(DamageMessage data) {
            //  if (currentHitPoints <= 0) {//ignore damage if already dead. TODO : may have to change that if we want to detect hit on death...

            if (m_Pawn.GetHealth()<=0f) { 
                return;
            }

            if (isInvulnerable) {
                OnHitWhileInvulnerable.Invoke();
                return;
            }

            Vector3 forward = transform.forward;
            forward = Quaternion.AngleAxis(hitForwardRotation, transform.up) * forward;

            //we project the direction to damager to the plane formed by the direction of damage
            Vector3 positionToDamager = data.damageSource - transform.position;
            positionToDamager -= transform.up * Vector3.Dot(transform.up, positionToDamager);

            if (Vector3.Angle(forward, positionToDamager) > hitAngle * 0.5f)
                return;

            isInvulnerable = true;
           // currentHitPoints -= data.amount;
           // if (currentHitPoints <= 0)
           //     schedule += OnDeath.Invoke; //This avoid race condition when objects kill each other.
            //else
                OnReceiveDamage.Invoke();

            //var messageType = currentHitPoints <= 0 ? MessageType.DEAD : MessageType.DAMAGED;

            for (var i = 0; i < onDamageMessageReceivers.Count; ++i) {
                var receiver = onDamageMessageReceivers[i] as IMessageReceiver;
                receiver.OnReceiveMessage(MessageType.DAMAGED, this, data);
            }
        }

        void LateUpdate() {
            if (schedule != null) {
                schedule();
                schedule = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Vector3 forward = transform.forward;
            forward = Quaternion.AngleAxis(hitForwardRotation, transform.up) * forward;

            if (Event.current.type == EventType.Repaint) {
                UnityEditor.Handles.color = Color.blue;
                UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(forward), 1.0f,
                    EventType.Repaint);
            }


            UnityEditor.Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
            forward = Quaternion.AngleAxis(-hitAngle * 0.5f, transform.up) * forward;
            UnityEditor.Handles.DrawSolidArc(transform.position, transform.up, forward, hitAngle, 1.0f);
        }
#endif
    }


}