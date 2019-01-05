using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game { 

/// <summary>
/// Hides one Gameobject (Character-Mesh) with anohterone (dead bogy)
/// </summary>
public class RagdollManager : MonoBehaviour {

    public GameObject show;
    public GameObject hide;

    public void Setup() {
        RecursiveBoneSearch(transform);
            Animator _anim = GetComponent<Animator>();
            if (_anim != null)
                _anim.enabled = false; 
                //    Destroy(GetComponent<Animator>());  cannot destroy animator if required by controller
        show.SetActive(true);
        hide.SetActive(false);
    }

    void RecursiveBoneSearch(Transform t) {
        foreach (Transform tC in t) {
            DoBone(tC);
            RecursiveBoneSearch(tC);
        }
    }

    void DoBone(Transform t) {
            //this will enable physics on rigidbody components (bones in armature-tree) to do ragdoll
        if (t.GetComponent<Rigidbody>()) {
            t.GetComponent<Rigidbody>().isKinematic = false;    
            t.GetComponent<Collider>().isTrigger = false;   //??

            if (t.GetComponent<FixedJoint>()) { //! set break force in fixed joint component asociated in the bones that should break
                //t.GetComponent<FixedJoint> ().breakForce = 100;

                //t.GetComponent<FixedJoint> ().breakTorque = 100;
            }
        }
    }
}
}
