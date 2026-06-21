using UnityEngine;
using System.Collections;

public class FireworkDice : PhysicalDice
{
    [Header("Configuración del Cohete (DFirework)")]
    [SerializeField] private float flightDuration = 2.5f;
    [SerializeField] private float moneyGenerationInterval = 0.1f;
    [SerializeField] private int moneyPerTick = 1;

    [Header("Movimiento Cinemático (Estilo Garabato Puro)")]
    [Tooltip("Velocidad de desplazamiento por la mesa")]
    [SerializeField] private float moveSpeed = 25f;
    [Tooltip("Cada cuántos segundos cambia obligatoriamente de dirección")]
    [SerializeField] private float minTimeBetweenQuiebros = 0.05f;
    [SerializeField] private float maxTimeBetweenQuiebros = 0.2f;

    [Header("Límites de la Mesa (Rango de los puntos)")]
    [SerializeField] private float limitX = 7.5f;
    [SerializeField] private float limitZ = 7.5f;

    [Header("Efectos Visuales")]
    [SerializeField] private ParticleSystem rocketTrailParticles;

    private bool isFlying = false;
    private float lockedHeightY;
    private Vector3 currentVelocityVector; // El vector actual de movimiento
    private float directionTimer = 0f;

    public override void Roll(Vector3 launchForce, Vector3 launchTorque)
    {
        base.Roll(launchForce, launchTorque);
        StartCoroutine(LaunchRocketRoutine());
    }

    private IEnumerator LaunchRocketRoutine()
    {
        Rigidbody currentRb = GetComponentInParent<Rigidbody>();
        if (currentRb != null)
        {
            // Ponemos el Rigidbody en cinemático temporalmente para que no le afecte la inercia ni los choques
            currentRb.isKinematic = true;
        }

        lockedHeightY = transform.position.y;
        isFlying = true;

        // Elegimos el primer vector de movimiento
        ChooseNewRandomDirection();

        if (rocketTrailParticles != null) rocketTrailParticles.Play();
        StartCoroutine(GenerateMoneyWhileFlying());

        yield return new WaitForSeconds(flightDuration);

        isFlying = false;
        if (rocketTrailParticles != null) rocketTrailParticles.Stop();

        if (currentRb != null)
        {
            // Devolvemos las físicas normales al apagarse el motor para que caiga a la mesa
            currentRb.isKinematic = false;
            currentRb.useGravity = true;
            currentRb.linearVelocity = currentVelocityVector * 0.5f; // Le dejamos una pequeña velocidad de caída
        }
        Debug.Log($"[DFirework] Combustible agotado.");
    }

    private IEnumerator GenerateMoneyWhileFlying()
    {
        while (isFlying)
        {
            yield return new WaitForSeconds(moneyGenerationInterval);
            if (TableManager.Instance != null && isFlying)
            {
                TableManager.Instance.AddMoney(moneyPerTick);
            }
        }
    }

    private void ChooseNewRandomDirection()
    {
        Vector3 currentPos = transform.position;

        // Si estamos cerca de un borde, forzamos que el vector apunte hacia el lado contrario (el centro)
        float targetX = Random.Range(-limitX, limitX);
        float targetZ = Random.Range(-limitZ, limitZ);

        Vector3 targetPoint = new Vector3(targetX, lockedHeightY, targetZ);

        // Calculamos el vector directo hacia ese punto de la mesa
        currentVelocityVector = (targetPoint - currentPos).normalized;

        // Tiempo hiperactivo para el próximo quiebro
        directionTimer = Random.Range(minTimeBetweenQuiebros, maxTimeBetweenQuiebros);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Rigidbody currentRb = GetComponentInParent<Rigidbody>();

        if (isFlying && currentRb != null)
        {
            // 1. CONTROL DEL TEMPORIZADOR DE QUIEBROS
            directionTimer -= Time.fixedDeltaTime;

            // Si el dado se sale de los límites de la mesa, fuerza un quiebro inmediatamente
            Vector3 currentPos = currentRb.position;
            if (Mathf.Abs(currentPos.x) > limitX || Mathf.Abs(currentPos.z) > limitZ || directionTimer <= 0f)
            {
                ChooseNewRandomDirection();
            }

            // 2. MOVIMIENTO DIRECTO EN LÍNEA RECTA (Fluido, constante y sin tirones)
            // Movemos la posición del Rigidbody directamente usando nuestro vector. 
            // Al no haber fuerzas, va recto como un tiralíneas hasta que cambia el vector, clavando los picos de tu dibujo.
            Vector3 newPosition = currentRb.position + (currentVelocityVector * moveSpeed * Time.fixedDeltaTime);
            newPosition.y = lockedHeightY; // Garantizamos la altura Y fija
            currentRb.MovePosition(newPosition);

            // 3. ROTACIÓN VISUAL DE PEONZA DESCONTROLADA
            // Como el movimiento va por código, podemos hacer que el D4 gire sobre sí mismo de forma loquísima 
            // sin que afecte a la trayectoria recta de la línea negra.
            Quaternion randomRot = Quaternion.Euler(
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f)
            );
            currentRb.MoveRotation(Quaternion.RotateTowards(currentRb.rotation, randomRot, 1000f * Time.fixedDeltaTime));
        }
    }
}