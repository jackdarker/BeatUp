using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Helpers {

    /// <summary>
    /// attach this script to an Gameobject to make it follow its movement.
    /// </summary>
    public class TransformFollow : MonoBehaviour {
        public Transform target;

        private void LateUpdate() {
            transform.position = target.position;
            transform.rotation = target.rotation;
        }
    }

    [DefaultExecutionOrder(9999)]
    public class FixedUpdateFollow : MonoBehaviour {
        public Transform toFollow;

        private void FixedUpdate() {
            transform.position = toFollow.position;
            transform.rotation = toFollow.rotation;
        }
    }
}
