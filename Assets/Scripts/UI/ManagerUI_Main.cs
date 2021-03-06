﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Game.UI {
    public class ManagerUI_Main : MonoBehaviour {

        [SerializeField]
        private Animator _mainMenuAnimator;

        public Animator MainMenuAnimator { get { return _mainMenuAnimator; } }

        #region Buttons Interactions
        //Lance la scène Map Island et détruit la scène actuelle "Menu_demarrer"
        public void LoadGame() {
            //Permet de verifier qu'il y ait bien une sauvegarde
            if (PlayerPrefs.HasKey("xPlayer")) {
                PlayerPrefs.SetInt("load_scene", 1);
                //Chargement de la scene
                SceneManager.LoadScene(PlayerPrefs.GetString("scene"));
            } else {
                Debug.LogWarning("The player should have saved data to be able to click on this button..");
            }
        }

        public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Buttons states.
        /*public void ChangeStateLoadButton(bool state) {
            loadButton.enabled = state;
        }*/
        #endregion

    }
}