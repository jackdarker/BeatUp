using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.AI {
    public class BrawlerSMBAttack : SceneLinkedSMB<BrawlerBehaviour> {
        protected Vector3 m_AttackPosition;

        public override void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnSLStateEnter(animator, stateInfo, layerIndex);

            m_MonoBehaviour.SetFollowNavmeshAgent(false);

            m_AttackPosition = m_MonoBehaviour.target.transform.position;
            Vector3 toTarget = m_AttackPosition - m_MonoBehaviour.transform.position;
            toTarget.y = 0;

            m_MonoBehaviour.transform.forward = toTarget.normalized;
            m_MonoBehaviour.SetForward(m_MonoBehaviour.transform.forward);

            if (m_MonoBehaviour.attackAudio != null)
                m_MonoBehaviour.attackAudio.PlayRandomClip();
        }

        public override void OnSLStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnSLStateExit(animator, stateInfo, layerIndex);

            if (m_MonoBehaviour.attackAudio != null)
                m_MonoBehaviour.attackAudio.audioSource.Stop();

            m_MonoBehaviour.SetFollowNavmeshAgent(true);
        }
    }
}