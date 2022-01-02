

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Framework
{
    public class Coroutine : MonoBehaviour
    {
        static Coroutine Instance;

        private void Awake()
        {
            Instance = this;
        }

        public static UnityEngine.Coroutine Run(IEnumerator e)
        {
            return Instance.StartCoroutine(e);
        }

        public static void StopRun(IEnumerator e)
        {
            Instance.StopCoroutine(e);
        }
    }
}