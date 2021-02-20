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
        public UnityEvent<GameObject> ReturnObject = new UnityEvent<GameObject>();

        private void OnEnable()
        {
            if (ReturnObject != null)
                ReturnObject.Invoke(gameObject);
        }

        private void Awake()  
        {
            if(ReturnObject != null)
                ReturnObject.Invoke(gameObject);
        }

        
    }
}