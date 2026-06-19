using UnityEngine;

public class TableManager : MonoBehaviour
{
    public static TableManager Instance { get; private set; }

    [Header("Economía de la Mesa")]
    [SerializeField] private int totalMoney = 0;

    [Header("Límites de Dados (GDD pág. 36)")]
    [SerializeField] private int currentDiceLimit = 3; // Límite inicial de dados (ej: empiezas con max 3)
    [SerializeField] private int maxPossibleLimit = 25; // El tope absoluto que se puede alcanzar con mejoras

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- GESTIÓN DE ECONOMÍA ---
    public void AddMoney(int amount)
    {
        totalMoney += amount;
        Debug.Log($"[TableManager] ¡Dinero añadido: +{amount}€! Monedero total: {totalMoney}€");
    }

    public int GetTotalMoney()
    {
        return totalMoney;
    }


    // --- GESTIÓN DE LÍMITES DE DADOS ---

    /// <summary>
    /// Devuelve el número máximo de dados permitidos actualmente en la mesa.
    /// </summary>
    public int GetDiceLimit()
    {
        return currentDiceLimit;
    }

    /// <summary>
    /// Aumenta el límite de dados permitidos en la mesa (Mejora de la tienda).
    /// </summary>
    public void IncreaseDiceLimit(int amountToIncrease)
    {
        if (currentDiceLimit < maxPossibleLimit)
        {
            currentDiceLimit += amountToIncrease;
            // Nos aseguramos de no pasarnos del límite máximo absoluto establecido
            currentDiceLimit = Mathf.Clamp(currentDiceLimit, 0, maxPossibleLimit);

            Debug.Log($"[TableManager] ¡Mejora comprada! Nuevo límite de dados en la mesa: {currentDiceLimit}");
        }
        else
        {
            Debug.Log("[TableManager] Ya has alcanzado el límite máximo de dados permitido en el juego.");
        }
    }

    /// <summary>
    /// Comprueba si la mesa está llena según los dados físicos actuales creados.
    /// </summary>
    public bool IsTableFull(int currentDiceCount)
    {
        return currentDiceCount >= currentDiceLimit;
    }
}