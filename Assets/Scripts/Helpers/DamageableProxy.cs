using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Helpers;
using UnityEngine.Serialization;

namespace Game.Helpers {

    public class DamageableProxy : Damageable {
        public int ID = 0;
        public Damageable ProxyTarget;


        public override void ApplyDamage(DamageMessage data) {
            ProxyTarget.ApplyDamage(data);
        }
    }
}
