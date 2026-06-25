using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Componentes de la Tienda")]
    [SerializeField] private List<ShopSlot> shopSlots = new List<ShopSlot>();

    [Header("Inventario de la Tienda")]
    [Tooltip("Arrastra aquí todos los archivos ScriptableObject (DiceData) de tus dados")]
    [SerializeField] private List<DiceData> availableDices = new List<DiceData>();

    private int currentPoolIndex = 0;

    void Start()
    {
        RefreshShopItems();
    }

    void Update()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            if (scrollInput > 0f) currentPoolIndex++;
            else if (scrollInput < 0f) currentPoolIndex--;

            // 🛠️ CONTROL DE TOPE (LÍMITE ESTRICTO DE SCROLL):
            // Evitamos que baje de cero
            if (currentPoolIndex < 0) currentPoolIndex = 0;

            // Calculamos el índice máximo permitido para que no hagas scroll al vacío.
            // Restamos los slots disponibles para que el escaparate se quede lleno al final de la lista.
            int maxIndex = availableDices.Count - shopSlots.Count;
            if (maxIndex < 0) maxIndex = 0; // Por si hay menos dados que slots en total

            if (currentPoolIndex > maxIndex) currentPoolIndex = maxIndex;

            RefreshShopItems();
        }
    }

    public void RefreshShopItems()
    {
        if (shopSlots.Count == 0) return;

        for (int i = 0; i < shopSlots.Count; i++)
        {
            // Calculamos la posición exacta en la lista para este pedestal
            int diceIndex = currentPoolIndex + i;

            // 🛠️ COMPROBACIÓN DE STOCK:
            // Si el índice está dentro de la lista de dados creados, lo mostramos.
            if (diceIndex < availableDices.Count)
            {
                shopSlots[i].DisplayDice(availableDices[diceIndex]);
            }
            else
            {
                // Si ya no quedan más dados únicos en la lista para rellenar este pedestal, 
                // limpiamos el slot para que se quede vacío y ordenado. ¡Cero repeticiones!
                shopSlots[i].ClearSlot();
            }
        }
    }
}