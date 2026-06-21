using UnityEngine;
using System.Collections;

public class FrozenModifier : IModifier
{
    public string NameKey => "MOD_FROZEN";

    private float duration = 5f;
    private PhysicsMaterial icePhysicMaterial;

    // Variables de respaldo para la restauración
    private PhysicsMaterial originalPhysicMaterial;
    private float originalLinearDamping;
    private float originalAngularDamping;

    private Color[] originalColors;
    private bool hasSavedOriginalState = false;

    // Almacenamos la corrutina activa para poder destruirla si nos vuelven a golpear
    private Coroutine meltCoroutine;

    public FrozenModifier(PhysicsMaterial icePhysics)
    {
        this.icePhysicMaterial = icePhysics;
    }

    public void OnApply(PhysicalDice dice)
    {
        Rigidbody rb = dice.GetComponent<Rigidbody>();
        Collider col = dice.GetComponent<Collider>();
        MeshRenderer meshRenderer = dice.GetComponent<MeshRenderer>();

        if (rb == null || col == null) return;

        // 1. Guardamos el estado original ÚNICAMENTE la primera vez de todas
        if (!hasSavedOriginalState)
        {
            originalPhysicMaterial = col.material;
            originalLinearDamping = rb.linearDamping;
            originalAngularDamping = rb.angularDamping;

            if (meshRenderer != null)
            {
                int materialCount = meshRenderer.materials.Length;
                originalColors = new Color[materialCount];

                for (int i = 0; i < materialCount; i++)
                {
                    if (meshRenderer.materials[i].HasProperty("_Color"))
                    {
                        originalColors[i] = meshRenderer.materials[i].color;
                    }
                }
            }
            hasSavedOriginalState = true;
        }

        // 2. Aplicamos físicas de hielo (Fricción cero)
        if (icePhysicMaterial != null) col.material = icePhysicMaterial;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.05f;

        // 3. Pintamos de azul gélido
        if (meshRenderer != null)
        {
            for (int i = 0; i < meshRenderer.materials.Length; i++)
            {
                if (meshRenderer.materials[i].HasProperty("_Color"))
                {
                    meshRenderer.materials[i].color = new Color(0.3f, 0.75f, 1f, 1f);
                }
            }
        }

        // --- CONTROL ANTIBUG DE TIEMPO MULTI-IMPACTO ---
        // Si ya había una cuenta atrás funcionando en este dado, la matamos por completo
        if (meltCoroutine != null)
        {
            dice.StopCoroutine(meltCoroutine);
        }

        // Iniciamos una cuenta atrás limpia desde cero (Resetea los 5 segundos con cada choque)
        meltCoroutine = dice.StartCoroutine(MeltRoutine(dice));
    }

    // Corrutina que cuenta los 5 segundos de forma segura e independiente
    private IEnumerator MeltRoutine(PhysicalDice dice)
    {
        yield return new WaitForSeconds(duration);

        // Al terminar el tiempo, forzamos la eliminación del modificador
        if (dice != null)
        {
            dice.RemoveModifier(this);
        }
    }

    public void OnUpdate(PhysicalDice dice) { }

    public void OnRemove(PhysicalDice dice)
    {
        // Por seguridad, si se elimina el efecto, nos aseguramos de apagar la corrutina
        if (meltCoroutine != null && dice != null)
        {
            dice.StopCoroutine(meltCoroutine);
            meltCoroutine = null;
        }

        Rigidbody rb = dice.GetComponent<Rigidbody>();
        Collider col = dice.GetComponent<Collider>();
        MeshRenderer meshRenderer = dice.GetComponent<MeshRenderer>();

        // Restauramos físicas originales de fábrica
        if (col != null) col.material = originalPhysicMaterial;
        if (rb != null)
        {
            rb.linearDamping = originalLinearDamping;
            rb.angularDamping = originalAngularDamping;
        }

        // Restauramos los colores originales de fábrica
        if (meshRenderer != null && originalColors != null)
        {
            for (int i = 0; i < meshRenderer.materials.Length; i++)
            {
                if (i < originalColors.Length && meshRenderer.materials[i].HasProperty("_Color"))
                {
                    meshRenderer.materials[i].color = originalColors[i];
                }
            }
        }

        // Reseteamos la bandera por si en el futuro se vuelve a congelar
        hasSavedOriginalState = false;
        Debug.Log($"[DIce] ¡Deshielo absoluto completado en {dice.gameObject.name}!");
    }

    public int ModifyReward(int currentReward) => currentReward;
    public void OnRoll(PhysicalDice dice) { }
}