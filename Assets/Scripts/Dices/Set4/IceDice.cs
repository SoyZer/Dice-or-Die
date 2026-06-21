using UnityEngine;

public class IceDice : PhysicalDice
{
    [Header("Configuración del DIce Elemental (Impacto Directo)")]
    [Range(0f, 100f)]
    [SerializeField] private float freezeProbability = 100f;
    [SerializeField] private float minVelocityToFreeze = 0.5f;

    [Header("Recursos del Hielo")]
    [SerializeField] private PhysicsMaterial icePhysicMaterial;
    [SerializeField] private Material iceVisualMaterial;

    [Header("Optimización de Rendimiento")]
    [SerializeField] private LayerMask diceLayer;

    private bool isCooldownActive = false;
    private const float FREEZE_COOLDOWN_DURATION = 0.05f;

    private void OnCollisionEnter(Collision collision)
    {
        if (isCooldownActive) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minVelocityToFreeze) return;

        // BÚSQUEDA INMUNE A JERARQUÍAS COMPLEJAS:
        // Buscamos el script PhysicalDice subiendo por la estructura del objeto chocado hasta dar con la raíz del dado
        PhysicalDice targetPhysical = collision.gameObject.GetComponentInParent<PhysicalDice>();

        // Si no lo encuentra directamente con la función nativa, probamos buscando la interfaz en la raíz del objeto transform
        IEffectable targetEffectable = null;
        if (targetPhysical != null)
        {
            targetEffectable = targetPhysical.GetComponent<IEffectable>();
        }

        // Si encontramos el dado y no somos nosotros mismos
        if (targetEffectable != null && targetPhysical != null && targetPhysical != this)
        {
            // Comprobamos la capa del dado raíz detectado
            if ((diceLayer.value & (1 << targetPhysical.gameObject.layer)) != 0)
            {
                float rollChance = Random.Range(0f, 100f);
                if (rollChance <= freezeProbability)
                {
                    isCooldownActive = true;

                    // Aplicamos el modificador
                    FrozenModifier iceMod = new FrozenModifier(icePhysicMaterial);
                    targetEffectable.ApplyModifier(iceMod);

                    Debug.Log($"[DIce] ¡Impacto directo exitoso! Congelado dado poliedro: {targetPhysical.gameObject.name}");

                    Invoke(nameof(ResetCooldown), FREEZE_COOLDOWN_DURATION);
                }
            }
            else
            {
                // Si entra aquí, es que el dado fue detectado pero su Layer está mal configurada en Unity
                Debug.LogWarning($"[DIce] Se detectó el dado {targetPhysical.gameObject.name}, pero ignorado por no estar en la capa correcta. Capa actual: {LayerMask.LayerToName(targetPhysical.gameObject.layer)}");
            }
        }
    }

    private void ResetCooldown()
    {
        isCooldownActive = false;
    }
}