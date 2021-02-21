using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem.Framework
{
    [ExecuteAlways]
    public class InventorySceneReference : MonoBehaviour
    {
        public UnityEvent<InventorySceneReference, GameObject> ReturnObject = new UnityEvent<InventorySceneReference, GameObject>();
        [HideInInspector] public Inventory InventoryReference = null;

        private void OnEnable()
        {
            Return();
        }

        private void Awake()  
        {
            Return();
        }

        private void Return()
        {
            if (ReturnObject != null)
                ReturnObject.Invoke(this, gameObject);

            if(Application.isPlaying)
            {
                Destroy(this);
            }
        }
    }
}