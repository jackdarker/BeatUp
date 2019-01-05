﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.AI {
    public class BrawlerSMBPursuit : SceneLinkedSMB<BrawlerBehaviour> {
        bool hasTarget=false;
        public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);

            m_MonoBehaviour.FindTarget();
            

            if (m_MonoBehaviour.navmeshAgent.pathStatus == NavMeshPathStatus.PathPartial
                || m_MonoBehaviour.navmeshAgent.pathStatus == NavMeshPathStatus.PathInvalid) {
                //pathfinding error
                m_MonoBehaviour.StopPursuit();
                hasTarget = false;
                return;
            }

            if (m_MonoBehaviour.target == null || m_MonoBehaviour.target.respawning) {
                //if the target was lost or is respawning, we stop the pursit
                m_MonoBehaviour.StopPursuit();
                hasTarget = false;
            } else {
                m_MonoBehaviour.RequestTargetPosition();
                Vector3 toTarget = m_MonoBehaviour.target.transform.position - m_MonoBehaviour.transform.position;

                if (toTarget.sqrMagnitude < m_MonoBehaviour.attackDistance * m_MonoBehaviour.attackDistance) {
                    m_MonoBehaviour.TriggerAttack();    //Attack if in Range

                } else if (m_MonoBehaviour.followerData.assignedSlot != -1) {
                    Vector3 targetPoint = m_MonoBehaviour.target.transform.position +
                        m_MonoBehaviour.followerData.distributor.GetDirection(m_MonoBehaviour.followerData
                            .assignedSlot) * m_MonoBehaviour.attackDistance * 0.9f;

                    m_MonoBehaviour.SetTarget(targetPoint);
                    m_MonoBehaviour.CalculateForwardMovement(toTarget);
                } else {
                    //not enough targetdistirbutor slots
                    m_MonoBehaviour.StopPursuit();
                    hasTarget = false;
                }
            }
            if (m_MonoBehaviour.target != null && hasTarget == false) {
                m_MonoBehaviour.StartPursuit();
                hasTarget = true;
            }
        }
    }
}