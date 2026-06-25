using UnityEngine;
using System.Collections;

public class ShopSlot : MonoBehaviour
{
    [Header("Configuración del Slot")]
    public Transform spawnPoint;

    // El precio ahora se actualizará solo según el DiceData actual
    [HideInInspector] public int price;

    private GameObject spawnedDiceInstance;
    private Rigidbody diceRb;
    private bool isPurchased = false;
    private bool isRegenerating = false;

    // 🛠️ Guardamos los datos del dado actual en este slot
    private DiceData currentDiceData;

    // Recibe el ScriptableObject enviado por el ShopManager
    public void DisplayDice(DiceData diceData)
    {
        ClearSlot();
        currentDiceData = diceData;

        if (currentDiceData != null)
        {
            // 🛠️ Seteamos el precio y el prefab leyendo el ScriptableObject
            price = currentDiceData.price;
            SpawnDiceForSale();
        }
    }

    private void SpawnDiceForSale()
    {
        if (currentDiceData == null || currentDiceData.dicePrefab == null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        // Instanciamos usando el prefab que está guardado dentro de las instrucciones del DiceData
        spawnedDiceInstance = Instantiate(currentDiceData.dicePrefab, pos, Quaternion.identity);
        isPurchased = false;

        diceRb = spawnedDiceInstance.GetComponentInChildren<Rigidbody>();
        if (diceRb != null)
        {
            diceRb.isKinematic = false;
            diceRb.useGravity = false;
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        if (spawnedDiceInstance == null || isRegenerating || isPurchased || diceRb == null) return;

        Vector3 targetPos = spawnPoint != null ? spawnPoint.position : transform.position;
        float distance = Vector3.Distance(spawnedDiceInstance.transform.position, targetPos);

        bool canAfford = true;
        int currentMoney = TableManager.Instance.GetTotalMoney();
        if (TableManager.Instance != null && currentMoney < price)
        {
            canAfford = false;
        }

        if (distance > 0.5f)
        {
            if (canAfford)
            {
                ConfirmPurchase();
            }
            else
            {
                Debug.LogWarning($"[Tienda] ¡No tienes dinero para comprar el {currentDiceData.diceName}! Devolviendo de forma segura.");

                // 🛠️ BLINDAJE ANTICAÍDAS:
                // Forzamos la devolución inmediata y congelamos el dado para que no caiga al vacío
                ResetDiceToPedestal(targetPos);
            }
        }
        else
        {
            // Si el dado está en el pedestal en reposo, nos aseguramos de que no tenga gravedad ni inercias
            if (distance < 0.1f)
            {
                diceRb.useGravity = false; // Por seguridad, nos aseguramos de que la gravedad esté apagada en el pedestal
                diceRb.linearVelocity = Vector3.zero;
                diceRb.angularVelocity = Vector3.zero;
                spawnedDiceInstance.transform.position = targetPos;
            }
        }
    }

    private void ResetDiceToPedestal(Vector3 targetPos)
    {
        if (diceRb != null)
        {
            // 🛠️ APAGAMOS LA GRAVEDAD DE INMEDIATO:
            // Evita que si el MouseManager lo suelta en el aire, el motor físico lo tire al vacío
            diceRb.useGravity = false;

            // Frenamos en seco cualquier velocidad o fuerza que el arrastre le hubiera metido
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;
        }

        // Lo devolvemos a la posición exacta del SpawnPoint
        spawnedDiceInstance.transform.position = targetPos;
    }

    private void ConfirmPurchase()
    {
        isPurchased = true;
        Debug.Log($"[Tienda] ¡Comprado: {currentDiceData.diceName} por {price}¢!");

        if (diceRb != null)
        {
            diceRb.useGravity = true;
        }

        if (TableManager.Instance != null)
        {
            TableManager.Instance.AddMoney(-price);
        }

        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        isRegenerating = true;
        yield return new WaitForSeconds(0.75f);

        if (currentDiceData != null && isPurchased)
        {
            SpawnDiceForSale();
        }

        isRegenerating = false;
    }

    public void ClearSlot()
    {
        if (spawnedDiceInstance != null) Destroy(spawnedDiceInstance);
        currentDiceData = null;
        diceRb = null;
        isPurchased = false;
    }
}