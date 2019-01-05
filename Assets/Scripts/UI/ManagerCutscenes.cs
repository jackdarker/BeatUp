using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.UI {
    /// <summary>
    /// Class that hold all the in-game cutscenes. Also in-charge to setup, launch, interrupt the right cutscnes.
    /// </summary>
    public class InteractionManager : MonoBehaviour, IManager {
        [SerializeField]
        private IDictionary<CutScene.InGameCutsceneName, CutScene> cutscenes;

        //private DialogueManager dialogue;
        private CutScene.InGameCutsceneName activeCutsceneName;

        public delegate void CutsceneHasEnded();
        public static event CutsceneHasEnded OnCutsceneHasEnded;
        // ...

        public void Init() {
            cutscenes = new Dictionary<CutScene.InGameCutsceneName, CutScene>();
            // Find all the cutscenes
            foreach (CutScene cutscene in FindObjectsOfType<CutScene>()) {
                if (!cutscenes.ContainsKey(cutscene.CutsceneName)) {
                    cutscenes.Add(cutscene.CutsceneName, cutscene);
                }
                //Debug.Log("obj:" + cutscene.gameObject + "name: " + cutscene.CutsceneName);
            }
        }

        public void InitMainMenuScene() {
            // Nothing yet ??
        }

        public void InitMainScene() {
            // Nothing yet ??
        }

        public void StartCutscene(CutScene.InGameCutsceneName name) {
            Debug.Log("Info: starting " + name + " cutscene.");

            // Subscribe the escape key so the player can escape the cutscene.
            ManagerInput.SubscribeButtonEvent(ManagerInput.ActionsLabels.Cancel, "Cancel", ManagerInput.EventTypeButton.Down, SkipCutscene);

            // Get the cutscene to play
            CutScene currentCutscene = cutscenes[name];
            if (currentCutscene != null) {
                // Subscribe to the end of the cutscene
                CutScene.OnCutsceneEnd += WhenCutsceneEnds;

                // Init it
                currentCutscene.Init();

                // Activate it
                Debug.Log("Info: activate " + name + " cutscene.");
                activeCutsceneName = name;
                currentCutscene.Activate();
            }
        }

        public void WhenCutsceneEnds(CutScene.InGameCutsceneName name) {
            // Unsubscribe the escape key so the player can escape the cutscene.
            ManagerInput.UnsubscribeButtonEvent(ManagerInput.ActionsLabels.Cancel);
            OnCutsceneHasEnded();
        }

        public void SkipCutscene() {
            StartCoroutine(cutscenes[activeCutsceneName].Interrupt());
        }

        /*
        private GameObject FindCutscene(CutScene.InGameCutsceneName name)
        {
            GameObject result = null;
            bool found = false;
            int i = 0;
            while(!found && i < cutscenes.Count)
            {
                found = (cutscenes[i].cutscenePrefabs.GetComponent<CutScene>().CutsceneName.Equals(name));
                result = found ? cutscenes[i].cutscenePrefabs : result;
                i++;
            }
            return result;
        }*/

        public void DisableCutscene() {
            cutscenes[activeCutsceneName].Disable();
        }

        /* ???
        public static void TriggerSauvegarde(CutScene action)
        {
            dialogue = new Dialogue();
            dialogue.name = "Menu";
            dialogue.sentences = new string[1];
            dialogue.sentences[0] = "Votre partie a bien été sauvegardée.";
            FindObjectOfType<DialogueManager>().StartDialogue(dialogue, action);
        }*/

    }
}
