using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Tornado : MonoBehaviour
{
    [Header("Configuración del Tornado Estable (Segunda Versión)")]
    [SerializeField] private float duration = 5f;          // Duración total del tornado
    [SerializeField] private float suctionForce = 25f;     // Fuerza de succión original (VelocityChange)
    [SerializeField] private float upwardForce = 15f;      // Fuerza vertical original para levantarlos
    [SerializeField] private float rotationalForce = 30f;  // Fuerza de órbita circular regulada
    [SerializeField] private float maxFlyHeight = 5f;      // Altura máxima que pueden alcanzar flotando

    private SphereCollider tornadoCollider;

    private void Awake()
    {
        tornadoCollider = GetComponent<SphereCollider>();
        tornadoCollider.isTrigger = true;
    }

    private void Start()
    {
        Debug.Log($"[Tornado] Torbellino original estabilizado. Duración: {duration}s");
        Destroy(gameObject, duration);
    }

    private void OnTriggerStay(Collider col)
    {
        PhysicalDice dice = col.GetComponent<PhysicalDice>();
        Rigidbody rb = col.GetComponent<Rigidbody>();

        if (dice != null && rb != null && !rb.isKinematic)
        {
            // 1. Dirección horizontal hacia el centro
            Vector3 directionToCenter = transform.position - col.transform.position;
            directionToCenter.y = 0f;

            float distance = directionToCenter.magnitude;

            if (distance > 0.1f)
            {
                // 2. SUCCIÓN ORIGINAL: Los arrastra al centro con fuerza directa constante (VelocityChange)
                Vector3 suction = directionToCenter.normalized * suctionForce;
                rb.AddForce(suction, ForceMode.VelocityChange);

                // 3. ÓRBITA CONTROLADA: Solo aplica el giro si el dado no ha superado una velocidad lateral crítica.
                // Esto evita el efecto centrifugadora que los lanzaba hacia fuera.
                Vector3 orbitDirection = Vector3.Cross(directionToCenter.normalized, Vector3.up);

                // Si la velocidad actual en el eje horizontal es moderada, le damos el empujón orbital
                Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                if (horizontalVel.magnitude < 8f)
                {
                    rb.AddForce(orbitDirection * rotationalForce, ForceMode.VelocityChange);
                }
            }

            // 4. VUELO VERTICAL ORIGINAL: Si está por debajo del límite, lo lanzamos hacia arriba
            if (col.transform.position.y < transform.position.y + maxFlyHeight)
            {
                rb.AddForce(Vector3.up * upwardForce, ForceMode.VelocityChange);
            }
            else
            {
                // Si sube demasiado, frenamos su velocidad vertical para que flote estable
                Vector3 vel = rb.linearVelocity;
                if (vel.y > 0)
                {
                    vel.y *= 0.5f;
                    rb.linearVelocity = vel;
                }
            }

            // =========================================================================
            // ELIMINADO EL ADDTORQUE LOCO:
            // Los dados orbitarán de forma limpia manteniendo sus caras legibles sin girar sobre sí mismos.
            // =========================================================================
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[Tornado] El viento cesa. Los dados caen y se procesarán como una nueva tirada.");

        // Justo en el instante en que el tornado se destruye, buscamos todos los dados atrapados
        float radioReal = tornadoCollider != null ? tornadoCollider.radius : 8f;
        Collider[] collidersFinales = Physics.OverlapSphere(transform.position, radioReal * transform.localScale.x);

        foreach (Collider col in collidersFinales)
        {
            PhysicalDice dice = col.GetComponent<PhysicalDice>();
            if (dice != null)
            {
                // Activamos 'hasRolled' mediante Reflection en tu PhysicalDice para que la mesa detecte la caída
                System.Reflection.FieldInfo hasRolledField = typeof(PhysicalDice).GetField("hasRolled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (hasRolledField != null)
                {
                    hasRolledField.SetValue(dice, true);

                    // Escudo de seguridad de 0.4 segundos mientras caen para que no intenten puntuar en el aire antes de tocar el suelo
                    System.Reflection.FieldInfo timerField = typeof(PhysicalDice).GetField("rollSafetyTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (timerField != null)
                    {
                        timerField.SetValue(dice, 0.4f);
                    }
                }
            }
        }
    }
}