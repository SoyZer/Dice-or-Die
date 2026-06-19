using UnityEngine;

public class DiceFace : MonoBehaviour
{
    [SerializeField] private int faceValue; // El valor que otorga esta cara (ej: 1, 2, 3, 4...)

    public int GetFaceValue()
    {
        return faceValue;
    }
}