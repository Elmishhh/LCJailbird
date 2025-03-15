using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCJailbird.HelperBehaviour
{
    public class ExplosionHelper : MonoBehaviour
    {
        public static ExplosionHelper _instance;
        public void Awake()
        {
            _instance = this;
            Plugin.Logger.LogMessage("Explosion Helper initialized");
        }

        public void ExplodeDelayed(Vector3 position)
        {
            StartCoroutine(_ExplodeDelayed(position));
        }
        private IEnumerator _ExplodeDelayed(Vector3 position)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Plugin.Logger.LogError("trying to explode player");
            Landmine.SpawnExplosion(position, true, 3, 3, 50);
            Plugin.Logger.LogError("exploded successfully");
        }

        public void OnDestroy()
        {
            GameObject temp = new GameObject("LCJailbird Explosion Helper", typeof(ExplosionHelper));
            GameObject.DontDestroyOnLoad(temp);
        }
    }
}
