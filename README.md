# InventoryProject

This project includes the Inventory System in its entirety along with a 
use case example.


The following instructions concludes the notable steps to be taken to use the inventory system.

Each inventory is defined by a ScriptableObject (InventoryObject)
To generate the inventory UI in the Editor, expose the inventory as a serialized field and
use the interactive buttons displayed in the inspector.

To generate a group of inventories, expose them as an Array.

Items are also defined by a ScriptableObject (ItemData)
For now, manually add items to the scriptableobjects (Via their asset)
until an Item Manager is implemented.


USE EXAMPLE

Select Items in the quickslot inventory with 1-9.
Spawn selected items with left mouse button.
Drag and drop to move items.
Sort inventories by shiftclicking an empty slot
