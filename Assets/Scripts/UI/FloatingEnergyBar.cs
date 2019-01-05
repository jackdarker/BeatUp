using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.UI {
    public class FloatingEnergyBar : MonoBehaviour {

        const float stayTime = 3;


        public RectTransform healthSlider;
        public GameObject graphic;

        Transform cam;
        Transform target;
        AI.PlayerProperties pawn;

        float healthPercentOld;
        float lastHealthChangeTime;

        public void Init(Transform Target,AIControllerBase Pawn) {
            this.target = Target;
            this.pawn = Pawn.Stats;

            cam = Camera.main.transform;        //Todo
            graphic.SetActive(true);
            healthPercentOld = GetHealthPercent();
        }

        void LateUpdate() {
            if (target == null) {
                Destroy(gameObject);
                return;
            }
            transform.position = target.position; 
            transform.forward = -1* cam.transform.forward;
            //transform.LookAt(new Vector3(cam.position.x, transform.position.y, cam.position.z), Vector3.down);

            float healthPercent = GetHealthPercent();
            healthSlider.localScale = new Vector3(healthPercent, 1, 1);

            if (!Mathf.Approximately(healthPercent, healthPercentOld)) {
                healthPercentOld = healthPercent;
                lastHealthChangeTime = Time.time;
                graphic.SetActive(true);
            }

            if (graphic.activeSelf) {
                if (Time.time - lastHealthChangeTime > stayTime) {
              //      graphic.SetActive(false);
                }
            }


        }

        float GetHealthPercent() {
            return Mathf.Clamp01(pawn.Health / pawn.MaxHealth);
        }
    }
}