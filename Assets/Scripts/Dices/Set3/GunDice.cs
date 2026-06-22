using UnityEngine;
using System.Collections;

public class GunDice : PhysicalDice
{
    [Header("Configuración del DGun")]
    [SerializeField] private Transform shootPoint;            // Desde dónde sale el disparo en el modelo
    [SerializeField] private float timeBetweenShots = 0.4f;    // Cadencia de fuego entre bala y bala
    [SerializeField] private float damagePerShot = 1f;         // Daño base de cada disparo
    [SerializeField] private float aimSpeed = 10f;             // Velocidad a la que gira el dado para apuntar

    [Header("Físicas de Retroceso (Recoil)")]
    [Tooltip("La fuerza con la que el dado sale despedido hacia atrás al disparar")]
    [SerializeField] private float recoilForce = 25f;
    [Tooltip("Pequeño impulso hacia arriba en el retroceso para que salte un poco")]
    [SerializeField] private float recoilUpForce = 10f;

    [Header("Sistema de Progresión (Kills)")]
    public int currentKills = 0;
    [SerializeField] private float damageBonusPerKill = 0.5f;
    [SerializeField] private int moneyBonusPerKill = 5;

    [Header("Efectos Visuales (Raycast)")]
    [Tooltip("Arrastra aquí un LineRenderer para pintar el trazado del balazo (Opcional)")]
    [SerializeField] private LineRenderer laserBeamPrefab;
    [SerializeField] private float laserDuration = 0.08f;

    private bool hasFiredThisRoll = false;
    public int result;

    public override void Roll(Vector3 launchForce, Vector3 launchTorque)
    {
        base.Roll(launchForce, launchTorque);
        hasFiredThisRoll = false;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Detectamos cuando el dado se detiene por completo en la mesa
        if (!hasFiredThisRoll && isStopped)
        {
            hasFiredThisRoll = true;
            StartCoroutine(ShootSequenceRoutine());
        }
    }

    private IEnumerator ShootSequenceRoutine()
    {
        // 1. Obtener el número del resultado del dado
        int diceResult = result;
        if (diceResult <= 0) diceResult = 1;

        Debug.Log($"[DGun] Detenido en {diceResult}. Buscando objetivos. Kills: {currentKills}");

        float finalDamage = damagePerShot + (currentKills * damageBonusPerKill);
        Rigidbody currentRb = GetComponentInParent<Rigidbody>();

        // 2. Ejecutar la ráfaga de disparos
        for (int i = 0; i < diceResult; i++)
        {
            GameObject targetDice = FindClosestTarget();

            if (targetDice != null)
            {
                // FASE DE APUNTADO: Giramos de forma fluida hacia el objetivo antes de disparar
                yield return StartCoroutine(AimAtTarget(targetDice));

                // FASE DE DISPARO (Raycast + Retroceso)
                ExecuteRaycastShot(targetDice, finalDamage, currentRb);
            }
            else
            {
                Debug.Log("[DGun] No quedan objetivos válidos en la mesa.");
                break;
            }

            // Esperamos la cadencia de fuego antes del siguiente tiro de la ráfaga
            yield return new WaitForSeconds(timeBetweenShots);
        }

        // 3. Recompensa económica al terminar la ráfaga
        if (TableManager.Instance != null && currentKills > 0)
        {
            TableManager.Instance.AddMoney(currentKills * moneyBonusPerKill);
        }
    }

    // Corrutina para rotar el dado suavemente hacia la víctima antes de apretar el gatillo
    private IEnumerator AimAtTarget(GameObject target)
    {
        float timeout = 0.5f; // Tiempo máximo para apuntar (evita que se quede atascado si el objetivo se mueve raro)
        float timer = 0f;

        while (target != null && timer < timeout)
        {
            Vector3 direction = (target.transform.position - transform.position);
            direction.y = 0f; // Mantener el apuntado en el plano horizontal de la mesa

            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, aimSpeed * Time.deltaTime);

                // Si ya está casi mirando al objetivo, salimos de la corrutina para disparar ya
                if (Quaternion.Angle(transform.rotation, targetRot) < 2f)
                {
                    break;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void ExecuteRaycastShot(GameObject target, float damage, Rigidbody rb)
    {
        Vector3 origin = shootPoint != null ? shootPoint.position : transform.position;
        // Apuntamos el rayo directamente hacia el centro del dado objetivo
        Vector3 direction = (target.transform.position - origin).normalized;

        Debug.Log($"[DGun] ¡PUM! Disparando a {target.name}");

        // --- 1. PROCESAR EL RAYCAST ---
        // Lanzamos el rayo a una distancia larga (ej: 100 unidades)
        if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
        {
            // Comprobamos si impactamos al dado objetivo (o a cualquier otro dado en la trayectoria)
            GameObject hitObject = hit.collider.transform.root.gameObject;

            // Pintamos el trazado visual del disparo
            DrawLaserBeam(origin, hit.point);

            if (hitObject == target || hitObject.GetComponentInChildren<PhysicalDice>() != null)
            {
                ProcessImpact(hitObject, damage);
            }
        }
        else
        {
            // Si el rayo falla por alineación visual, pintamos un disparo largo ficticio
            DrawLaserBeam(origin, origin + direction * 20f);
        }

        // --- 2. FISICAS DE RETROCESO (RECOIL) ---
        if (rb != null)
        {
            // Reactivamos físicas por si acaso estuviera en modo cinemático o durmiendo
            rb.isKinematic = false;

            // El vector de retroceso es el contrario a la dirección del disparo
            Vector3 recoilDir = -direction;
            recoilDir.y = 0f; // Fuerza puramente horizontal en el tapete
            recoilDir.Normalize();

            // Añadimos el empuje hacia atrás + un pequeño salto vertical
            Vector3 finalRecoilVector = (recoilDir * recoilForce) + (Vector3.up * recoilUpForce);

            // Aplicamos un impulso instantáneo
            rb.AddForce(finalRecoilVector, ForceMode.Impulse);

            // Opcional: Le metemos un pequeño torque/giro brusco para simular el impacto del arma
            rb.AddTorque(new Vector3(Random.Range(-10f, 10f), Random.Range(-50f, 50f), Random.Range(-10f, 10f)), ForceMode.Impulse);
        }
    }

    private void ProcessImpact(GameObject hitObject, float damage)
    {
        GunDice victimDGun = hitObject.GetComponentInChildren<GunDice>();
        int victimKills = 0;

        if (victimDGun != null)
        {
            victimKills = victimDGun.currentKills;
        }

        // Registramos la kill en este dado
        RegisterKill(victimKills);

        // Destruimos el dado impactado
        Destroy(hitObject);
    }

    private void DrawLaserBeam(Vector3 start, Vector3 end)
    {
        if (laserBeamPrefab != null)
        {
            LineRenderer beam = Instantiate(laserBeamPrefab);
            beam.SetPosition(0, start);
            beam.SetPosition(1, end);
            Destroy(beam.gameObject, laserDuration); // Se destruye en milisegundos creando el efecto de destello
        }
    }

    private GameObject FindClosestTarget()
    {
        PhysicalDice[] allDices = FindObjectsByType<PhysicalDice>(FindObjectsSortMode.None);
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (PhysicalDice dice in allDices)
        {
            if (dice == this || dice.gameObject == this.gameObject) continue;

            float distance = Vector3.Distance(currentPos, dice.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = dice.transform.root.gameObject;
            }
        }

        return closest;
    }

    public void RegisterKill(int victimKills)
    {
        currentKills++;
        currentKills += victimKills;

        Debug.Log($"[DGun] ¡Kill registrada! Kills totales: {currentKills}");

        // El dado crece un poco con cada baja confirmada
        //transform.localScale = Vector3.one * (100f + (currentKills * 0.1f));
    }
}