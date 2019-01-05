using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game {
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour {
        public string startingPointName="Start";        // The name that identifies this starting point in the scene.


        private static List<Checkpoint> allStartingPositions = new List<Checkpoint>();
        // This list contains all the StartingPositions that are currently active.


        private void OnEnable() {
            // When this is activated, add it to the list that contains all active StartingPositions.
            allStartingPositions.Add(this);
        }


        private void OnDisable() {
            // When this is deactivated, remove it from the list that contains all the active StartingPositions.
            allStartingPositions.Remove(this);
        }


        public static Transform FindStartingPosition(string pointName) {
            // Go through all the currently active StartingPositions and return the one with the matching name.
            for (int i = 0; i < allStartingPositions.Count; i++) {
                if (allStartingPositions[i].startingPointName == pointName)
                    return allStartingPositions[i].transform;
            }

            // If a matching StartingPosition couldn't be found, return null.
            return null;
        }
        private void Awake() {
            //we make sure the checkpoint is part of the Checkpoint layer, which is set to interact ONLY with the player layer.
            //?? gameObject.layer = LayerMask.NameToLayer("Checkpoint");
        }

        private void OnTriggerEnter(Collider other) {
            PlayerController controller = other.GetComponent<PlayerController>();

            if (controller == null)
                return;

            controller.SetCheckpoint(this);
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.blue * 0.75f;
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawRay(transform.position, transform.forward * 2);
        }
    }
}
