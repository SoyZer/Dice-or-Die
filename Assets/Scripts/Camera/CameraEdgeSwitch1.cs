using UnityEngine;
using UnityEngine.InputSystem;

public class CameraEdgeSwitch : MonoBehaviour
{
    public Transform cameraViewShop;
    public Transform cameraViewTable;
    public Transform cameraViewCasino;

    public float edgeSize = 10f;
    public float moveSpeed = 5f;

    private Transform targetView;

    private void Start()
    {
        targetView = cameraViewTable;
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mousePos.y >= Screen.height && targetView == cameraViewTable)
        {
            targetView = cameraViewShop;
        }
        else if (mousePos.x >= Screen.width - edgeSize && targetView == cameraViewTable)
        {
            targetView = cameraViewCasino;
        }
        else if (mousePos.y <= edgeSize && targetView != cameraViewTable)
        {
            targetView = cameraViewTable;
        }

        if (targetView != null)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetView.position,
                moveSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetView.rotation,
                moveSpeed * Time.deltaTime
            );
        }
    }
}