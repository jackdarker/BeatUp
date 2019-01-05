using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game {
    public class CutScene : MonoBehaviour {
        #region Enums
        public enum InGameCutsceneName : int {
            IntroductionCutscene = 0,
        }
        #endregion

        #region Serialize Fields
        [SerializeField]
        private InGameCutsceneName _cutsceneName;
        public InGameCutsceneName CutsceneName { get { return _cutsceneName; } }

        //[SerializeField]
        //private Dialogue _dialogue;
        //public Dialogue Dialogue { get { return _dialogue; } }

        [SerializeField]
        public Camera defaultCamera;

        [SerializeField]
        public Canvas overlayCanvas;

        [SerializeField]
        public Animator whiteScreenAnimator;

        //[SerializeField]
        //protected List<Phase> cutscenePhases;
        #endregion

        public delegate void CutsceneEnd(InGameCutsceneName name);
        public static event CutsceneEnd OnCutsceneEnd;

        private Camera mainCamera;

        public virtual void Init() {

            mainCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        }
        public virtual void Activate() {
            // Disable the main camera
            if (mainCamera != null) {
                mainCamera.enabled = false;
            }
            // Enable the default camera for the cutscene
            defaultCamera.enabled = true;

            //?? DialogueManager.OnClicked += Display;
            // Call only once!
            //FindObjectOfType<DialogueManager>().InitDialogueUI();

            //Display();
        }
        private void EndCutscene() {
            Finish();
            Disable();
            // Fire the end event
            OnCutsceneEnd(_cutsceneName);
        }

        private void Finish() {
            /*DialogueManager.OnClicked -= Display;

            FindObjectOfType<DialogueManager>().EndDialogue();

            GameObject.FindWithTag("Player").GetComponent<PlayerProperties>().CloseDialogue();*/
        }

        public void Disable() {
            if (mainCamera != null) {
                mainCamera.enabled = true;
            }
            defaultCamera.enabled = false;
        }

        public IEnumerator Interrupt() {
            Finish();

            // Fade the cutscene with a white screen
            whiteScreenAnimator.SetTrigger("QuickFade");

            yield return new WaitForSeconds(1.0f);

            // Escape all the current active phases
           /* for (int phaseIndex = 0; phaseIndex < activePhases.Count; phaseIndex++) {
                Phase activePhase = activePhases[phaseIndex];

                activePhase.Interrupt();
                activePhases.Remove(activePhase);
            }*/

            Disable();

            // Fire the end event
            OnCutsceneEnd(_cutsceneName);
        }
    }
}
