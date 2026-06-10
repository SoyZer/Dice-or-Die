using UnityEngine;
public interface IGrabbable
{
    void Grab();
    void Release();
    void Move(Vector3 targetPosition);
}