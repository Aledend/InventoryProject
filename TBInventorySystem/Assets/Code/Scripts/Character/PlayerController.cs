using UnityEngine;
using UnityEngine.Assertions;
using InventorySystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInventory m_PlayerInventory = null;
    [SerializeField] private Camera m_Camera = null;

    private GameObject m_HeldItemPreview = null;
    private Rigidbody m_HeldItemRigidBody = null;
    private bool m_HoldingItem = false;
    private int m_HoldingIndex = 0;
    private float m_Speed = 5f;

    [SerializeField] private LayerMask m_PreviewMask;

    private void Awake()
    {
        Assert.IsNotNull(m_PlayerInventory, "PlayerInventory needs to be set.");
        Assert.IsNotNull(m_PlayerInventory, "PlayerController is missing its camera.");
        m_PlayerInventory.OnItemSelect.AddListener(OnItemSwap);
    }

    private void Update()
    {
        HandleMovement();
        HandleHeldItem();
        HandleSpawn();
    }

    public void OnItemSwap(InventorySystem.InventoryItem item, int inventoryIndex)
    {
        if(m_HeldItemPreview)
        {
            Destroy(m_HeldItemPreview);
        }
        
        if (item.Data != null && item.Data.Spawnable)
        {

            if(item.Data.ItemPrefab.GetComponentInChildren<Rigidbody>())
            {
                m_HoldingItem = true;
                m_HeldItemPreview = Instantiate(item.Data.ItemPrefab);
                m_HeldItemRigidBody = m_HeldItemPreview.GetComponentInChildren<Rigidbody>();
                m_HeldItemRigidBody.detectCollisions = false;
                m_HeldItemRigidBody.isKinematic = !m_HeldItemRigidBody.isKinematic;
                m_HoldingIndex = inventoryIndex;
            }
            else
            {
                Debug.LogWarning("Item was marked as spawnable but does not contain a Rigidbody.");
            }
        }
        else
        {
            m_HoldingItem = false;
        }
    }

    private void HandleSpawn()
    {
        if(m_HoldingItem && Input.GetMouseButtonDown(0))
        {
            if (m_PlayerInventory.QuickSlotInventory.TakeItem(m_HoldingIndex, out InventoryItem item))
            {
                m_HeldItemRigidBody.detectCollisions = true;
                m_HeldItemRigidBody.isKinematic = !m_HeldItemRigidBody.isKinematic;
                GameObject.Instantiate(m_HeldItemPreview);
                m_HeldItemRigidBody.isKinematic = !m_HeldItemRigidBody.isKinematic;
                m_HeldItemRigidBody.detectCollisions = false;
                if(item.Amount <= 0)
                {
                    Destroy(m_HeldItemPreview);
                    m_HoldingItem = false;
                    m_HoldingIndex = -1;
                }
            }
        }
    }

    private void HandleHeldItem()
    {
        if(m_HoldingItem)
        {
            if(Physics.Raycast(m_Camera.transform.position, m_Camera.transform.forward,
                out RaycastHit hitInfo, 5f, m_PreviewMask))
            {
                m_HeldItemPreview.SetActive(true);
                
                m_HeldItemPreview.transform.position = hitInfo.point + 
                    hitInfo.normal * m_HeldItemPreview.GetComponentInChildren<Collider>().bounds.extents.y;
            }
            else
            {
                m_HeldItemPreview.SetActive(false);
            }
        }
    }

    private void HandleMovement()
    {
        transform.position += Quaternion.AngleAxis(m_Camera.transform.rotation.eulerAngles.y, Vector3.up) * 
            new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized 
            * Time.deltaTime * m_Speed;
    }
}
