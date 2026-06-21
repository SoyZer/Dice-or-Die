using UnityEngine;

public class BounceDice : PhysicalDice
{
    [Header("Configuración para Gravedad")]
    [SerializeField] private float verticalBounceForce = 60f;          // Fuerza masiva para vencer la gravedad de -150
    [SerializeField] private float horizontalFrictionMultiplier = 1f;  // Mantiene el avance lateral en el aire
    [SerializeField] private float minVelocityToBounce = 0.1f;

    [Header("Control de Recorrido por la Mesa")]
    [SerializeField] private float horizontalPushForce = 12f;          // Fuerza lateral mínima que tendrá el dado en cada bote

    [Header("Configuración del Premio por Rebote")]
    [SerializeField] private int minMoneyPerBounce = 1;
    [SerializeField] private int maxMoneyPerBounce = 100;

    [Header("Efectos Visuales (Opcional)")]
    [SerializeField] private ParticleSystem bounceParticles;

    private void OnCollisionEnter(Collision collision)
    {
        // LEER VARIABLES PRIVADAS DE LA BASE MEDIANTE REFLECTION
        bool baseHasRolled = false;
        bool baseIsGrabbed = false;

        System.Reflection.FieldInfo hasRolledField = typeof(PhysicalDice).GetField("hasRolled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        System.Reflection.FieldInfo isGrabbedField = typeof(PhysicalDice).GetField("isGrabbed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        if (hasRolledField != null) baseHasRolled = (bool)hasRolledField.GetValue(this);
        if (isGrabbedField != null) baseIsGrabbed = (bool)isGrabbedField.GetValue(this);

        if (!baseHasRolled || baseIsGrabbed) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed > minVelocityToBounce)
        {
            Vector3 collisionNormal = collision.contacts[0].normal;

            // Calculamos el reflejo físico real del choque contra el suelo o paredes
            Vector3 reflectDirection = Vector3.Reflect(collision.relativeVelocity.normalized, collisionNormal);

            Vector3 finalVelocity = Vector3.zero;

            // 1. CALCULAR VELOCIDAD HORIZONTAL BASE
            finalVelocity.x = rb.linearVelocity.x * horizontalFrictionMultiplier;
            finalVelocity.z = rb.linearVelocity.z * horizontalFrictionMultiplier;

            // 2. ANTIBUCLE (EVITA SALTAR EN EL SITIO):
            // Medimos cuánta velocidad horizontal real tiene el dado en este instante
            float currentHorizontalSpeed = new Vector3(finalVelocity.x, 0f, finalVelocity.z).magnitude;

            // Si es muy baja (el dado se iba a quedar saltando en el sitio), le inyectamos movimiento
            if (currentHorizontalSpeed < 2f)
            {
                // Extraemos la dirección horizontal del reflejo del impacto
                Vector3 horizontalReflect = new Vector3(reflectDirection.x, 0f, reflectDirection.z).normalized;

                // Si por algún motivo el reflejo también es completamente vertical (0,0), inventamos una dirección aleatoria por la mesa
                if (horizontalReflect.magnitude < 0.1f)
                {
                    float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                    horizontalReflect = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
                }

                // Forzamos al dado a salir despedido hacia esa dirección horizontal con la fuerza que elijas
                finalVelocity.x = horizontalReflect.x * horizontalPushForce;
                finalVelocity.z = horizontalReflect.z * horizontalPushForce;
            }

            // 3. CALCULAR VELOCIDAD VERTICAL (Tu salto alto que ya funciona perfecto)
            finalVelocity.y = verticalBounceForce;

            // Aplicamos la velocidad final combinada de golpe en el Rigidbody
            rb.linearVelocity = finalVelocity;

            // Un giro rápido para que en el aire se note la rotación antes de que la gravedad lo hunda
            Vector3 extraTorque = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            rb.AddTorque(extraTorque, ForceMode.Impulse);

            // 2. PREMIO ALEATORIO CONFIGURABLE
            int dineroGanado = Random.Range(minMoneyPerBounce, maxMoneyPerBounce + 1);
            Debug.Log($"[DBounce] ¡Salto contra Súper Gravedad! Paga: ${dineroGanado}");

            // 3. EFECTO VISUAL
            if (bounceParticles != null)
            {
                Instantiate(bounceParticles, collision.contacts[0].point, Quaternion.LookRotation(collisionNormal));
            }
        }
    }

    public override void Roll(Vector3 force, Vector3 torque)
    {
        base.Roll(force, torque);

        System.Reflection.FieldInfo hasRolledField = typeof(PhysicalDice).GetField("hasRolled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (hasRolledField != null)
        {
            hasRolledField.SetValue(this, true);
        }
    }
}