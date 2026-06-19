using UnityEngine;

public class WindDice : PhysicalDice
{
    [Header("Habilidad de Viento")]
    [SerializeField] private GameObject tornadoPrefab; // El prefab visual/lógico del tornado

    private void OnEnable()
    {
        // Nos suscribimos al evento global que ya definiste en PhysicalDice
        PhysicalDice.OnDiceStopped += HandleDiceStopped;
    }

    private void OnDisable()
    {
        PhysicalDice.OnDiceStopped -= HandleDiceStopped;
    }

    private void HandleDiceStopped(PhysicalDice stoppedDice, int result)
    {
        // Comprobamos si el dado que se acaba de detener es precisamente ESTE dado de viento
        if (stoppedDice == this)
        {
            // Evitamos activar la habilidad múltiples veces en un mismo turno/lanzamiento
            if (!hasTriggeredAbilityThisTurn)
            {
                hasTriggeredAbilityThisTurn = true;
                SpawnTornado();
            }
        }
    }

    private void SpawnTornado()
    {
        Debug.Log($"[DWind] ¡Tornado generado en la posición de {gameObject.name}!");

        if (tornadoPrefab != null)
        {
            // Spawnear el tornado justo encima de la posición actual del dado
            Vector3 spawnPosition = transform.position + Vector3.up * 0.2f;
            Instantiate(tornadoPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            // Fallback: Si no tienes prefab visual todavía, creamos un objeto vacío con el script físico
            GameObject temporaryTornado = new GameObject("Tornado_Fisico_Temporal");
            temporaryTornado.transform.position = transform.position;
            temporaryTornado.AddComponent<Tornado>();
        }
    }

    // Al volverlo a agarrar, reseteamos el trigger antibucle para la próxima tirada
    public override void Grab()
    {
        base.Grab();
        hasTriggeredAbilityThisTurn = false;
    }

    public override void Roll(Vector3 force, Vector3 torque)
    {
        base.Roll(force, torque);
        hasTriggeredAbilityThisTurn = false;
    }
}