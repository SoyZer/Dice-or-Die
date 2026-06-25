using UnityEngine;

[CreateAssetMenu(fileName = "NewDice", menuName = "Shop/Dice Settings")]
public class DiceData : ScriptableObject
{
    [Header("Información General")]
    public string diceName;          // Nombre del dado (ej: "Dado de Fuego")
    public int price = 25;           // Precio en la tienda

    [Header("Visuales y Físicas")]
    public GameObject dicePrefab;    // El modelo 3D real con sus físicas y scripts

    // Opcional: Aquí podrías añadir un Sprite para la interfaz, descripción, etc.
    // public Sprite diceIcon;
    // [TextArea] public string description;
}