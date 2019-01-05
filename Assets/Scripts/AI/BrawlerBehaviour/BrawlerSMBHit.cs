using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.AI {
    public class BrawlerSMBHit : SceneLinkedSMB<BrawlerBehaviour> {
        public override void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            animator.ResetTrigger(BrawlerBehaviour.hashAttack);
        }

        public override void OnSLStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            m_MonoBehaviour.ClearForce();
        }
    }
}