using UnityEngine;
using System.Collections;
using Game.Helpers;
using Game.Effects;

namespace Game.AI {

    /// <summary>
    /// A controller for a stationary practice dummy
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Animator))]
    public class DummyController : AIControllerBase {

        public MeleeWeapon meleeWeapon;                  // Reference used to (de)activate weapon when attacking. 

        protected Collider m_CharCtrl;      // 
        protected bool m_InAttack;                     // Whether Ellen is currently in the middle of a melee attack.
        protected bool m_InCombo;                      // Whether Ellen is currently in the middle of her melee combo.
        protected Damageable m_Damageable;             // Reference used to set invulnerablity and health based on respawning.

        void Reset() {
            meleeWeapon = GetComponentInChildren<MeleeWeapon>();

        }
        // Called automatically by Unity when the script first exists in the scene.
        void Awake() {
            m_Animator = GetComponent<Animator>();
            m_CharCtrl = GetComponent<CharacterController>();

            if (meleeWeapon != null)
                meleeWeapon.SetOwner(gameObject);
        }
        // Called automatically by Unity after Awake whenever the script is enabled. 
        void OnEnable() {
            //todo SceneLinkedSMB<PlayerController>.Initialise(m_Animator, this);
            m_PawnStats.ResetPlayerStats();
            m_Damageable = GetComponent<Damageable>();
            if (m_Damageable != null) {
                m_Damageable.onDamageMessageReceivers.Add(this);
                m_Damageable.isInvulnerable = true;
                m_Damageable.SetPawn(this);
            }
        }

        // Called automatically by Unity whenever the script is disabled.
        void OnDisable() {
            if (m_Damageable != null)
                m_Damageable.onDamageMessageReceivers.Remove(this);

        }
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
    }
}
