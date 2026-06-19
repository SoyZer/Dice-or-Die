using UnityEngine;

public class RewardSystem : MonoBehaviour
{
    private void OnEnable()
    {
        // El contable se sienta a mirar la mesa
        PhysicalDice.OnDiceStopped += ProcessDiceResult;
    }

    private void OnDisable()
    {
        PhysicalDice.OnDiceStopped -= ProcessDiceResult;
    }

    private void ProcessDiceResult(PhysicalDice dice, int resultIgnored)
    {
        // 1. LEER EL RESULTADO REAL DESDE LA CARA BOCA ABAJO
        int realResult = dice.GetFinalResult();

        Debug.Log($"[RewardSystem] {dice.gameObject.name} se ha detenido de forma estable. Cara en el suelo detectada. Resultado: {realResult}");

        VoltDice voltDice = dice as VoltDice;
        if (voltDice != null)
        {
            // Le pasamos el resultado real para que el rayo salte tantas veces como dicte la cara
            voltDice.ActivateElectricChain(realResult);
        }

        // 2. CALCULAR LAS GANANCIAS PASANDO POR TODOS LOS MODIFICADORES ACTIVOS
        int finalReward = realResult;

        // El truco de magia: Da igual qué modificadores tenga el dado, 
        // pasamos el dinero por el filtro de cada uno de ellos de forma automática.
        foreach (IModifier mod in dice.GetActiveModifiers())
        {
            finalReward = mod.ModifyReward(finalReward);
        }

        // 3. INGRESAR EL DINERO FINAL MODIFICADO
        if (TableManager.Instance != null)
        {
            TableManager.Instance.AddMoney(finalReward);
            Debug.Log($"[RewardSystem] Ingresados {finalReward}€ (Resultado original: {realResult}€)");
        }
        else
        {
            Debug.LogWarning("[RewardSystem] No se encuentra el TableManager.");
        }
    }
}