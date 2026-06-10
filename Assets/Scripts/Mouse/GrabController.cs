using UnityEngine;

public class GrabController : MonoBehaviour
{
    public Camera cam;
    public LayerMask grabbableLayer;

    private Rigidbody currentRb;

    private Vector3 lastWorldPos;
    private Vector3 velocity;

    private bool dragging;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryGrab();

        if (Input.GetMouseButton(0) && dragging)
            Drag();

        if (Input.GetMouseButtonUp(0) && dragging)
            Release();
    }

    void TryGrab()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, grabbableLayer))
        {
            currentRb = hit.rigidbody;

            currentRb.linearVelocity = Vector3.zero;
            currentRb.angularVelocity = Vector3.zero;

            currentRb.isKinematic = true;
            dragging = true;

            lastWorldPos = GetMousePointOnTable();
        }
    }

    void Drag()
    {
        Vector3 targetPos = GetMousePointOnTable();

        velocity = (targetPos - lastWorldPos) / Time.deltaTime;
        lastWorldPos = targetPos;

        currentRb.MovePosition(targetPos);
    }

    void Release()
    {
        dragging = false;

        currentRb.isKinematic = false;

        // 🔥 LIMITAR VELOCIDAD (IMPORTANTE)
        velocity = Vector3.ClampMagnitude(velocity, 5f);

        currentRb.linearVelocity = velocity;

        currentRb.angularVelocity = new Vector3(
            velocity.z,
            0f,   // ❌ quitamos Y completamente
            -velocity.x
        ) * 0.2f;

        currentRb = null;
    }

    Vector3 GetMousePointOnTable()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, grabbableLayer))
        {
            return hit.point;
        }

        return lastWorldPos;
    }
}