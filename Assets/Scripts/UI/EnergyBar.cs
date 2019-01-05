using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Helpers;
namespace Game.UI {
    public class EnergyBar : MonoBehaviour {
        #region Components settings
        [SerializeField] private RectTransform BarSprite;
        [SerializeField] private Color BaseColor=Color.red;
        [SerializeField] private float Ticks=100;
        [SerializeField] private float PixPerTick = 2;
        #endregion

        public void Awake() {
            if (BarSprite != null) {
                BarSprite.GetComponent<Image>().color = BaseColor;
            }

        }
        public float GetCurrentValue() {
            return BarSprite.sizeDelta.x;
        }

        public void SetCurrentValue(float size) {
            float newValue = Mathf.Clamp(size * PixPerTick, 0f,Ticks* PixPerTick); //Todo PlayerProperties.MAX_ENERGY_PLAYER);
            BarSprite.sizeDelta = new Vector2(newValue, BarSprite.sizeDelta.y);
        }

        /*virtual public void OnReceiveMessage(MessageType type, object sender, object data) {
            switch (type) {
                case MessageType.DAMAGED: 
                case MessageType.DEAD: 
                        Damageable.DamageMessage damageData = (Damageable.DamageMessage)data;
                        ModifyCurrentValue(damageData.amount*-1);
                    
                    break;
                default:
                break;
            }
        }*/
    }
}