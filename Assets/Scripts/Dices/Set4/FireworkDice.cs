using UnityEngine;
using System.Collections;

public class FireworkDice : PhysicalDice
{
    [Header("Configuración del Cohete (DFirework)")]
    [Tooltip("Cuánto tiempo dura el combustible del cohete volando por la mesa")]
    [SerializeField] private float flightDuration = 2.5f;
    [Tooltip("Cada cuántos segundos genera dinero en pleno vuelo")]
    [SerializeField] private float moneyGenerationInterval = 0.1f;
    [Tooltip("Cantidad de dinero que suma en cada intervalo")]
    [SerializeField] private int moneyPerTick = 1;

    [Header("Físicas de Caos con Inercia")]
    [Tooltip("Fuerza constante de empuje del motor (Súbelo para más velocidad)")]
    [SerializeField] private float propulsionForce = 60f;
    [Tooltip("Tiempo mínimo en segundos para pegar un volantazo y cambiar de rumbo")]
    [SerializeField] private float minTimeDirection = 0.05f;
    [Tooltip("Tiempo máximo en segundos antes de obligar al dado a cambiar de rumbo")]
    [SerializeField] private float maxTimeDirection = 0.25f;
    [Tooltip("Límite de velocidad horizontal para que el dado no salga disparado infinitamente")]
    [SerializeField] private float maxHorizontalSpeed = 35f;

    [Header("Efectos Visuales")]
    [Tooltip("Arrastra aquí el sistema de partículas acoplado a la base del dado")]
    [SerializeField] private ParticleSystem rocketTrailParticles;

    // Variables internas de control
    private bool isFlying = false;
    private float lockedHeightY;
    private Vector3 currentTargetDirection;
    private float directionTimer = 0f;

    // Este método se activa automáticamente cuando el jugador lanza el dado desde la mesa
    public override void Roll(Vector3 launchForce, Vector3 launchTorque)
    {
        // Ejecutamos el lanzamiento físico inicial
        base.Roll(launchForce, launchTorque);

        // Encendemos el motor del cohete
        StartCoroutine(LaunchRocketRoutine());
    }

    private IEnumerator LaunchRocketRoutine()
    {
        // Memorizamos la altura exacta en la que se ha activado el lanzamiento
        lockedHeightY = transform.position.y;
        isFlying = true;

        // Activamos los fuegos artificiales visuales
        if (rocketTrailParticles != null) rocketTrailParticles.Play();

        // Iniciamos la lluvia de dinero
        StartCoroutine(GenerateMoneyWhileFlying());

        // Esperamos en segundo plano a que se agote el combustible
        yield return new WaitForSeconds(flightDuration);

        isFlying = false;

        // Apagamos los efectos del motor para que caiga por gravedad
        if (rocketTrailParticles != null) rocketTrailParticles.Stop();

        Debug.Log($"[DFirework] Combustible agotado. ¡Físicas normales restauradas!");
    }

    private IEnumerator GenerateMoneyWhileFlying()
    {
        while (isFlying)
        {
            yield return new WaitForSeconds(moneyGenerationInterval);

            // Sumamos el dinero directamente al TableManager de tu juego
            if (TableManager.Instance != null && isFlying)
            {
                TableManager.Instance.AddMoney(moneyPerTick);
            }
        }
    }

    protected override void FixedUpdate()
    {
        // Ejecutamos primero las comprobaciones base del dado (frenado, velocidad residual, etc.)
        base.FixedUpdate();

        // Buscamos dinámicamente el Rigidbody (evita fallos si cambia de capa con el MouseManager)
        Rigidbody currentRb = GetComponentInParent<Rigidbody>();

        if (isFlying && currentRb != null)
        {
            // ====================================================================
            // 1. ESTABILIZACIÓN EN EL EJE Y (Mantiene la altura fija)
            // ====================================================================
            Vector3 currentPos = currentRb.position;
            currentRb.position = new Vector3(currentPos.x, lockedHeightY, currentPos.z);

            Vector3 currentVel = currentRb.linearVelocity;
            currentRb.linearVelocity = new Vector3(currentVel.x, 0f, currentVel.z);

            // ====================================================================
            // 2. TEMPORIZADOR DEL CAOS: CAMBIOS DE RUMBO IMPREDECIBLES
            // ====================================================================
            directionTimer -= Time.fixedDeltaTime;
            if (directionTimer <= 0f)
            {
                // Elegimos una dirección horizontal (X, Z) completamente aleatoria y pura
                currentTargetDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

                // Calculamos un nuevo lapso de tiempo irregular para el próximo volantazo
                directionTimer = Random.Range(minTimeDirection, maxTimeDirection);
            }

            // ====================================================================
            // 3. PROPULSIÓN CONTINUA CON INERCIA (Efecto Garabato/Derrape)
            // ====================================================================
            // Aplicamos una fuerza constante frame a frame hacia el objetivo caótico.
            // Al no detener la velocidad a cero, el Rigidbody dibuja curvas cerradas y 
            // derrapes pesados muy fluidos cuando cambia bruscamente el vector.
            currentRb.AddForce(currentTargetDirection * propulsionForce, ForceMode.Acceleration);

            // ====================================================================
            // 4. LIMITADOR DE VELOCIDAD DE SEGURIDAD
            // ====================================================================
            if (currentRb.linearVelocity.magnitude > maxHorizontalSpeed)
            {
                currentRb.linearVelocity = currentRb.linearVelocity.normalized * maxHorizontalSpeed;
            }

            // ====================================================================
            // 5. ROTACIÓN CAÓTICA VISUAL (Giro de peonza descontrolada)
            // ====================================================================
            Vector3 chaosTorque = new Vector3(
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f)
            );
            currentRb.AddTorque(chaosTorque, ForceMode.Acceleration);
        }
    }
}