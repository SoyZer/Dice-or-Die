using UnityEngine;

public class Dice : MonoBehaviour, IGrabbable
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Grab()
    {
        Debug.Log("Dado cogido");
    }

    public void Release()
    {
        Debug.Log("Dado soltado");
    }

    public void Move(Vector3 targetPosition)
    {
        transform.position = targetPosition;
    }
}