using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Item")]
public class ItemData : ScriptableObject
{
    public CategorySystem.Category Category = new CategorySystem.Category();
    public GameObject ItemPrefab = null;
    public Sprite ItemSprite = null;
    public int StackSize = 1;
    public string ItemName = string.Empty;
    public string Description = string.Empty;
}
