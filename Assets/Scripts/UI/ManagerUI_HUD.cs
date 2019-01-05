using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI {
    /// <summary>
    /// this class creates the Player-HUD components and binds them to players
    /// </summary>
    public class ManagerUI_HUD : MonoBehaviour {

        [Header("HUD")]
        [SerializeField] private CanvasGroup hudCanvas;
        [SerializeField] public EnergyBar energyBar;
        [SerializeField] public EnergyBar healthBar;
        [SerializeField] public FloatingEnergyBar floatingEnergyBarPrefab;
        [SerializeField] public AIControllerBase playerController;

        public static ManagerUI_HUD instance;

        void Awake() {
            instance = this;
            //playerController = GameObject.FindGameObjectWithTag("Player");
            playerController.OnDamage.AddListener(healthBar.SetCurrentValue);
        }

        public void CreateEnemyFloaters(Transform target,AIControllerBase pawn) {
            FloatingEnergyBar newUI= Instantiate(floatingEnergyBarPrefab, transform) as FloatingEnergyBar;
            newUI.Init(target,pawn);
        }

        public void DisplayHUD() {
            hudCanvas.alpha = 1.0f;
        }

        public void HideHUD() {
            hudCanvas.alpha = 0.0f;
        }

    }
}
