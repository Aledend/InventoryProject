using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public class SimpleInputListener : MonoBehaviour
    {
        public event Action Callback;
        public KeyCode BoundKey;

        private void Update()
        {
            if (Input.GetKeyDown(BoundKey))
            {
                Callback.Invoke();
            }
        }
    }
}
