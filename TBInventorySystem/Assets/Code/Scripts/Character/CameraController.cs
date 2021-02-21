using UnityEngine;

public class CameraController : MonoBehaviour
{
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        euler += new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0f);
        transform.rotation = Quaternion.Euler(euler);
    }
}
