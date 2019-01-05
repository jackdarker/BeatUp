using UnityEngine;
using System.Collections;
using Game.Helpers;
using Game.Effects;

namespace Game.AI {
    //represents the state of the player
    public class PlayerProperties {

        public const string startingPositionKey = "starting position";
        // The key used to retrieve the starting position from the playerSaveData.

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private int lives = 2;

        private float m_Health=1;
        public float Health { get { return m_Health; } }
        public float MaxHealth { get { return maxHealth; } }
        private float m_Energy;

        public void Start() {
            ResetPlayerStats();
        }
        public void ResetPlayerStats() {
            m_Health = maxHealth;
            m_Energy = maxEnergy;
    }

        public void Damage(Damageable.DamageMessage damageMessage) {
            m_Health = Mathf.Clamp((m_Health - damageMessage.amount), 0, maxHealth);
        }
    }
}
