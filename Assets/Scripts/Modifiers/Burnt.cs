using UnityEngine;

public class Burnt : IModifier
{
    public string NameKey => "MOD_BURNT_NAME";

    public void OnApply(PhysicalDice dice)
    {
        // Visual: Outline negro/ceniza permanente
        dice.SetHighlight(true, Color.black, 4f);

        // Efecto visual extra: Tintamos la malla del dado de un color oscuro/ceniza
        Renderer rend = dice.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.15f, 0.15f, 0.15f);
        }
    }

    public void OnUpdate(PhysicalDice dice)
    {
        // Al ser permanente, no necesita temporizadores en el Update
    }

    public void OnRoll(PhysicalDice dice)
    {
        // Al ser permanente según tu GDD, no le restamos nada al lanzarse. 
        // Se queda vacío para cumplir con la interfaz.
    }

    public void OnRemove(PhysicalDice dice)
    {
        // Según las reglas de tu juego, este modificador no se puede quitar normalmente
    }

    /// <summary>
    /// Modifica el resultado final del dado según el GDD (Caras ilegibles vs Caras potenciadas)
    /// </summary>
    public int ModifyResult(int originalResult)
    {
        // Lógica de diseño: Si la cara es par (ej: saca un 2, 4, 6 u 8 en el D8), la ceniza tapa el número (0€).
        // Si es impar, el fuego ha purificado el valor y te da un bonus plano de +5€ a la banca.
        if (originalResult % 2 == 0)
        {
            Debug.Log("[Efecto Calcinado] El número de la cara está cubierto de ceniza. Ganancia: 0€");
            return 0;
        }
        else
        {
            int nuevoResultado = originalResult + 5;
            Debug.Log($"[Efecto Calcinado] ¡Cara purificada por el fuego! {originalResult}€ -> {nuevoResultado}€");
            return nuevoResultado;
        }
    }

    public int ModifyReward(int currentReward)
    {
        // Si el resultado es par, la ceniza lo tapa (0€). Si es impar, bono de +5€
        if (currentReward % 2 == 0)
        {
            Debug.Log("[Efecto Calcinado] El número está cubierto de ceniza. Ganancia: 0€");
            return 0;
        }
        else
        {
            return currentReward + 2;
        }
    }
}