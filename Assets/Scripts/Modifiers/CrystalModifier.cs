using UnityEngine;

public class CrystalModifier : IModifier
{
    public string NameKey => "MOD_CRISTAL_NAME";

    private int cracks = 0;
    private const int MaxCracks = 3;

    public void OnApply(PhysicalDice dice)
    {
        cracks = 0;
        // Visual: Un outline blanco/brillante translúcido para simular cristal
        dice.SetHighlight(true, new Color(0.9f, 0.9f, 1f), 3f);

        string name = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetTranslation(NameKey) : "Cristal";
        Debug.Log($"[Modificador] {dice.gameObject.name} ahora es de {name}. ¡Multiplica x3 pero se puede romper!");
    }

    public void OnUpdate(PhysicalDice dice) { }

    public void OnRoll(PhysicalDice dice)
    {
        // Al lanzarse, hay una probabilidad de 1/3 (33.3%) de agrietarse
        if (Random.Range(0, 3) == 0)
        {
            cracks++;
            Debug.LogWarning($"[Cristal] ¡CRACK! {dice.gameObject.name} se ha agrietado. Grietas: {cracks}/{MaxCracks}");

            // Aquí en el futuro puedes cambiar el Outline a rojo o añadir un sonido de cristal roto
            dice.SetHighlight(true, new Color(1f, 0.5f, 0.5f), 3f + cracks);

            if (cracks >= MaxCracks)
            {
                OnRemove(dice);
                // Destruimos el objeto físico del dado por completo
                Object.Destroy(dice.gameObject);
            }
        }
    }

    public void OnRemove(PhysicalDice dice)
    {
        Debug.LogError($"[Cristal] ¡El dado {dice.gameObject.name} se ha hecho añicos!");
        dice.SetHighlight(false);
        dice.RemoveModifier(this);
    }

    public int ModifyReward(int currentReward)
    {
        // Regla del GDD: Aplica un x3 a todas tus tiradas
        Debug.Log($"[Efecto Cristal] Multiplicador de cristal activo: {currentReward}€ x 3 = {currentReward * 3}€");
        return currentReward * 3;
    }
}