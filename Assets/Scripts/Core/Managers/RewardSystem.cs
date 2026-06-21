using System.Collections;
using UnityEngine;

public class RewardSystem : MonoBehaviour
{
    private void OnEnable()
    {
        PhysicalDice.OnDiceStopped += OnDiceStoppedHandler;
    }

    private void OnDisable()
    {
        PhysicalDice.OnDiceStopped -= OnDiceStoppedHandler;
    }

    // Receptor del evento: Desvía el flujo de inmediato a una Corrutina segura
    private void OnDiceStoppedHandler(PhysicalDice dice, int result)
    {
        StartCoroutine(ProcessDiceResultRoutine(dice, result));
    }

    private IEnumerator ProcessDiceResultRoutine(PhysicalDice dice, int result)
    {
        // 1. ESCUDO TEMPORAL MAESTRO: Esperamos al final del frame / siguiente frame.
        // Esto permite que el dado complete su parada física, limpie sus banderas (hasRolled = false)
        // y termine de procesar internamente sus modificadores OnRoll una sola vez.
        yield return null;

        // Seguridad por si destruyeron el dado justo en este lapso
        if (dice == null) yield break;

        // 2. USAR EL RESULTADO YA CALCULADO
        // ¡SÚPER IMPORTANTE! Ya no llamamos a GetFinalResult(). Usamos el 'result' que nos envía el evento.
        // Esto evita que los modificadores como el fuego resten tiradas dobles por error.
        int realResult = result;

        Debug.Log($"[RewardSystem] Procesando de forma segura {dice.gameObject.name}. Resultado: {realResult}");

        // 3. ACTIVAR HABILIDADES ESPECIALES (Como el VoltDice)
        VoltDice voltDice = dice as VoltDice;
        if (voltDice != null)
        {
            voltDice.ActivateElectricChain(realResult);
        }

        // 4. CALCULAR LAS GANANCIAS PASANDO POR TODOS LOS MODIFICADORES ACTIVOS
        int finalReward = realResult;

        // Recorremos la lista de modificadores. Ahora es 100% seguro porque estamos en un frame limpio
        var activeModifiers = dice.GetActiveModifiers();
        if (activeModifiers != null)
        {
            foreach (IModifier mod in activeModifiers)
            {
                if (mod != null)
                {
                    finalReward = mod.ModifyReward(finalReward);
                }
            }
        }

        // 5. INGRESAR EL DINERO FINAL MODIFICADO
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