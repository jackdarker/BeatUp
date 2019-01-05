using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI {
    public class ManagerMenu : MonoBehaviour, IManager {

        #region Enums
        public enum MenuPanel : int {
            MainMenu = 0,
            Options = 1,
            Credits = 2,
            Pause = 3,
            Controls = 4,
        }
        #endregion

        #region Serialize Field
        [Header("UI Prefabs")]
        [SerializeField] public GameObject mainUIPrefab;
        [SerializeField] public GameObject inGameUIPrefab;

        [Header("Others Settings")]
        [SerializeField] private GameObject pause = null;
        [SerializeField] private GameObject help = null;
        #endregion

        private GameObject _currentMainUI;
        private GameObject _currentInGameUI;

        private ManagerUI_Main _mainUIScript;
        private ManagerUI_HUD _InGameUIScript;

        private Canvas _mainUICanvas;

        void IManager.Init() {
            // Create the UI prefab if needed
            RetrieveOrCreateMainUI();
        }

        #region Scenes initialization methods
        void IManager.InitMainMenuScene() {
            // Enable or not the launch game button
            PlayerPrefs.SetInt("load_scene", 0);
            //?? _mainUIScript.ChangeStateLoadButton(PlayerPrefs.HasKey("xPlayer"));


            // Set the canvas's render mode to Screen Space - Camera
            _mainUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _mainUICanvas.worldCamera = Camera.main;

            //??_mainUIScript.Title.SetActive(true);
            // Let know the animator of events
            _mainUIScript.MainMenuAnimator.SetTrigger("Open");
            _mainUIScript.MainMenuAnimator.SetInteger("PanelNumber", (int)MenuPanel.MainMenu);
        }

        void IManager.InitMainScene() {
            // Create the HUD if needed
            RetrieveOrCreatedInGameUI();

            DisplayHUD();

            // 
            //Todo GameObject.FindWithTag("Player").GetComponent<PlayerProperties>().SetMenuManager(this);

            // Set the canvas's render mode to World Space Render
            _mainUICanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Create the Transformation Wheel
            //??   CreateWheelIcons();
        }
        #endregion

        #region MainUI Methods
        public void RetrieveOrCreateMainUI() {
            if (!GameObject.FindGameObjectWithTag(mainUIPrefab.tag)) {
                Debug.Log("Info: Create mainUI in the hierarchy");
                _currentMainUI = Instantiate(mainUIPrefab);
            } else {
                _currentMainUI = GameObject.FindGameObjectWithTag(mainUIPrefab.tag);
            }
            _mainUICanvas = _currentMainUI.GetComponent<Canvas>();
            _mainUIScript = _currentMainUI.GetComponent<ManagerUI_Main>();
        }

        public void TransitionToNextPanelMain(MenuPanel nextPanel) {
            // Enable the trigger for the given panel
            _mainUIScript.MainMenuAnimator.SetInteger("PanelNumber", (int)nextPanel);
            _mainUIScript.MainMenuAnimator.SetTrigger("Close");
        }
        #endregion

        #region InGameUI Methods
        private void RetrieveOrCreatedInGameUI() {
            if (!GameObject.FindGameObjectWithTag(inGameUIPrefab.tag)) {
                Debug.Log("Info: Create InGameUI in the hierarchy");
                _currentInGameUI = Instantiate(inGameUIPrefab);
            } else {
                _currentInGameUI = GameObject.FindGameObjectWithTag(inGameUIPrefab.tag);
            }
            _InGameUIScript = _currentInGameUI.GetComponent<ManagerUI_HUD>();
        }

        public void DisplayHUD() {
            _InGameUIScript.DisplayHUD();
        }

        #endregion

        #region Player HUD methods


        public float GetSizeEnergyBar() {
            return _InGameUIScript.energyBar.GetCurrentValue();
        }

        public void UpdateEnergyBar(float amount) {
            _InGameUIScript.energyBar.SetCurrentValue(amount);
        }

        #endregion
    }
}