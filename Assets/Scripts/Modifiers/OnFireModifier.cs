using UnityEngine;

public class OnFireModifier : IModifier
{
    public string NameKey => "MOD_ON_FIRE_NAME";

    // Cambiamos el tiempo por tiradas
    private int rollsRemaining = 3;
    private int burnIntensity = 0;
    private const int MaxBurnsBeforeBurnt = 3;

    public void OnApply(PhysicalDice dice)
    {
        // Cada vez que el DFlame nos vuelve a golpear, reiniciamos las tiradas a 3
        rollsRemaining = 3;
        burnIntensity++;

        // Visual fuego
        dice.SetHighlight(true, new Color(1f, 0.3f, 0f), 6f);

        string nombreTraducido = LocalizationManager.Instance.GetTranslation(NameKey);
        Debug.Log($"[Modificador] {dice.gameObject.name} está {nombreTraducido}. Tiradas restantes: {rollsRemaining}. Intensidad: {burnIntensity}/{MaxBurnsBeforeBurnt}");

        if (burnIntensity >= MaxBurnsBeforeBurnt)
        {
            TriggerCalcinado(dice);
        }
    }

    public void OnUpdate(PhysicalDice dice)
    {
        // Ya no necesitamos hacer nada frame a frame con el tiempo
    }

    public void OnRoll(PhysicalDice dice)
    {
        if (rollsRemaining <= 0) return; // Si ya se gastó, no seguimos restando

        rollsRemaining--;
        burnIntensity--;
        Debug.Log($"[Modificador] Dice ha gastado una tirada de fuego. Quedan: {rollsRemaining}");

        if (rollsRemaining <= 0)
        {
            // Quita el fuego de forma segura
            dice.RemoveModifier(this);
        }
    }

    public void OnRemove(PhysicalDice dice)
    {
        Debug.Log($"[Modificador] El fuego se ha extinguido de forma natural en {dice.gameObject.name} tras agotarse las tiradas.");
        dice.SetHighlight(false);
        dice.RemoveModifier(this);
    }

    public int ModifyReward(int currentReward)
    {
        return currentReward*2; // El fuego normal no altera el dinero
    }

    private void TriggerCalcinado(PhysicalDice dice)
    {
        string nombreCalcinado = LocalizationManager.Instance.GetTranslation("MOD_BURNT_NAME");
        Debug.LogWarning($"[Modificador] ¡{dice.gameObject.name} se ha calcinado!");

        dice.SetHighlight(false);
        Burnt calcinado = new Burnt();
        dice.ApplyModifier(calcinado);

        // Eliminamos este modificador de fuego inmediatamente para que se quede solo el permanente
        dice.RemoveModifier(this);
    }
}