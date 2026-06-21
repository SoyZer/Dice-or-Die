using UnityEngine;

public class FlameDice : PhysicalDice
{
    [Header("Configuración Ígnea (DFlame)")]
    [SerializeField] private float collisionForceThreshold = 0.5f;
    [SerializeField] private ParticleSystem fireImpactParticles;

    [Header("Optimización de Rendimiento")]
    [SerializeField] private LayerMask diceLayer;

    // ESCUDO DE TIEMPO ABSOLUTO (Anti-Crashes)
    private float nextFlameTime = 0f;
    private const float FLAME_COOLDOWN = 0.1f; // Tiempo mínimo entre contagios físicos (100ms)

    // CONTROL VISUAL: Evita saturar la tarjeta gráfica creando partículas idénticas seguidas
    private float nextParticleTime = 0f;
    private const float PARTICLE_COOLDOWN = 0.4f;

    private void OnCollisionEnter(Collision collision)
    {
        // CONTROL INMEDIATO: Si no ha pasado el tiempo de seguridad, descartamos el choque en nanosegundos
        if (Time.time < nextFlameTime) return;

        // 1. Buscamos el dado subiendo por la jerarquía (Inmune a problemas de hijos visuales en D20, D4...)
        PhysicalDice otroDado = collision.gameObject.GetComponentInParent<PhysicalDice>();

        // Si encontramos un dado válido y no somos nosotros mismos
        if (otroDado != null && otroDado != this)
        {
            // 2. FILTRO DE CAPA: Comprobamos si el dado pertenece a la capa 'Dice' configurada
            if ((diceLayer.value & (1 << otroDado.gameObject.layer)) != 0)
            {
                // 3. Verificamos la velocidad del impacto
                float impactSpeed = collision.relativeVelocity.magnitude;
                if (impactSpeed >= collisionForceThreshold)
                {
                    // BLOQUEAMOS EL TIEMPO INMEDIATAMENTE (Evita que otros choques en este mismo frame entren aquí)
                    nextFlameTime = Time.time + FLAME_COOLDOWN;

                    // 4. INSTANCIACIÓN SEGURA: Solo creamos el efecto visual si ha pasado el cooldown de partículas
                    if (fireImpactParticles != null && Time.time >= nextParticleTime)
                    {
                        nextParticleTime = Time.time + PARTICLE_COOLDOWN;

                        ContactPoint puntoContacto = collision.contacts[0];
                        Instantiate(fireImpactParticles, puntoContacto.point, Quaternion.identity);
                    }

                    // 5. Le prendemos fuego aplicando el modificador
                    OnFireModifier fuego = new OnFireModifier();
                    otroDado.ApplyModifier(fuego);

                    Debug.Log($"[DFlame] ¡Fuego contagiado de forma segura a: {otroDado.gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[DFlame] Se detectó el dado {otroDado.gameObject.name}, pero no se quema por no estar en la capa correcta. Capa actual: {LayerMask.LayerToName(otroDado.gameObject.layer)}");
            }
        }
    }
}