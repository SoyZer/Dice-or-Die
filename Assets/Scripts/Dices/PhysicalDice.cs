using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PhysicalDice : MonoBehaviour, IDice, IGrabbable, IEffectable
{
    public static event Action<PhysicalDice, int> OnDiceStopped;

    [Header("Configuración Física")]
    [SerializeField] private float stopThreshold = 0.05f;
    [SerializeField] private float grabHeight = 10f;
    [SerializeField] private float grabSpeed = 25f;
    [SerializeField] private float maxHorizontalVelocity = 1f;
    [SerializeField] private float rollSafetyDuration = 0.3f;

    [Header("Mecánica de Agitación (Shake)")]
    [SerializeField] private float shakeRequirement = 0.5f; // Cuántos segundos acumulados de agitación frenética hacen falta
    [SerializeField] private float shakeThreshold = 15f;    // Umbral de velocidad del ratón para que cuente como "agitación frenética"
    [SerializeField] private float energyDecay = 2f;        // Qué tan rápido pierde la energía si dejas de agitarlo

    [Header("Corrección de Posición de Emergencia")]
    [SerializeField] private float antiStuckJumpForce = 5f; // Fuerza del pequeño salto si se queda de canto
    [SerializeField] private float antiStuckTorqueForce = 10f;

    private Rigidbody rb;
    private bool isGrabbed = false;
    private bool hasRolled = false;
    private List<IModifier> activeModifiers = new List<IModifier>();

    // Variables de movimiento y cálculo
    private Vector3 targetWorldPosition;
    private Vector3 lastPosition;
    private Vector3 customThrowVelocity;

    // Variables del sistema de agitación
    private float currentShakeEnergy = 0f;
    private bool isFullyCharged = false;

    private Outline outlineComponent;
    protected DiceFace faceOnGround;

    // NUEVO: Temporizador para la capa de seguridad
    private float rollSafetyTimer = 0f;
    protected bool hasTriggeredAbilityThisTurn = false; // Mantenemos el control antibucle para el RewardSystem

    private void Awake()
    {

        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Buscamos el componente de Outline en el dado (o en sus hijos si el modelo 3D está dentro)
        outlineComponent = GetComponentInChildren<Outline>();
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false; // Empezamos apagado
            outlineComponent.OutlineWidth = 5f; // Grosor del contorno
            outlineComponent.OutlineColor = Color.white; // Color inicial
        }
    }

    private void Update()
    {
        if (isGrabbed)
        {
            CalculateTargetPosition();
        }
        else if (hasRolled)
        {
            // NUEVO: Reducimos el temporizador de seguridad
            if (rollSafetyTimer > 0f)
            {
                rollSafetyTimer -= Time.deltaTime;
                return; // Mientras el temporizador esté activo, IGNORAMOS por completo la comprobación de frenado
            }

            if (rb.linearVelocity.magnitude < stopThreshold && rb.angularVelocity.magnitude < stopThreshold)
            {
                // 1. Comprobamos si el resultado es válido
                int finalResult = GetFinalResult();

                if (finalResult == -99)
                {
                    // ¡ALERTA! El dado se ha parado pero está de canto o bugeado fuera de la mesa
                    Debug.LogWarning($"[Dice] {gameObject.name} se ha quedado de canto o sin cara válida. ¡Pegando salto de reajuste!");

                    // Aplicamos el microsalto de emergencia utilizando el método Roll para reactivar el escudo de seguridad
                    Vector3 jumpVector = new Vector3(UnityEngine.Random.Range(-1f, 1f), antiStuckJumpForce, UnityEngine.Random.Range(-1f, 1f));
                    Vector3 torqueVector = new Vector3(UnityEngine.Random.Range(-antiStuckTorqueForce, antiStuckTorqueForce), UnityEngine.Random.Range(-antiStuckTorqueForce, antiStuckTorqueForce), UnityEngine.Random.Range(-antiStuckTorqueForce, antiStuckTorqueForce));

                    Roll(jumpVector, torqueVector);
                }
                else
                {
                    // El dado es válido y ha caído sobre una cara limpia
                    hasRolled = false;
                    OnDiceStopped?.Invoke(this, finalResult);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (isGrabbed)
        {
            // 1. Mover el dado con físicas hacia el cursor
            Vector3 direction = targetWorldPosition - transform.position;
            rb.linearVelocity = direction * grabSpeed;

            // 2. Calcular la velocidad real del arrastre frame a frame
            customThrowVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
            lastPosition = transform.position;

            // 3. PROCESAR AGITACIÓN
            float movementSpeedThisFrame = customThrowVelocity.magnitude;

            if (movementSpeedThisFrame > shakeThreshold)
            {
                currentShakeEnergy += Time.fixedDeltaTime;
            }
            else
            {
                currentShakeEnergy -= Time.fixedDeltaTime * energyDecay;
            }

            currentShakeEnergy = Mathf.Clamp(currentShakeEnergy, 0f, shakeRequirement);

            // 4. NUEVO: HACER QUE GIRE AL AGITARLO
            if (currentShakeEnergy > 0)
            {
                // Calculamos un porcentaje de 0 a 1 de cuánto se ha cargado el dado
                float chargePercent = currentShakeEnergy / shakeRequirement;

                // Definimos una velocidad de rotación máxima (ej. 30). Puedes subir este número si quieres que gire más rápido.
                float currentSpinSpeed = chargePercent * 10f;

                // Hacemos que gire en diagonal para que se vea caótico en todas las direcciones
                rb.angularVelocity = new Vector3(1f, 1.5f, 0.5f).normalized * currentSpinSpeed;
            }
            else
            {
                // Si está totalmente quieto en la mano, no gira
                rb.angularVelocity = Vector3.zero;
            }

            // Comprobamos si ya está cargado al 100%
            if (currentShakeEnergy >= shakeRequirement && !isFullyCharged)
            {
                isFullyCharged = true;
                TriggerChargeFeedback();
            }
        }
    }

    // --- IMPLEMENTACIÓN DE IGRABBABLE ---
    public void Grab()
    {
        if (hasRolled)
        {
            Debug.Log($"[Dice] No puedes agarrar {gameObject.name} hasta que se detenga.");
            return;
        }

        isGrabbed = true;
        hasRolled = false;
        isFullyCharged = false;
        currentShakeEnergy = 0f;

        rb.isKinematic = false;
        rb.useGravity = false;

        CalculateTargetPosition();
        transform.position = new Vector3(transform.position.x, grabHeight, transform.position.z);
        lastPosition = transform.position;

        // AL AGARRAR: Cambia a contorno BLANCO (mantenemos grosor 3 o el que quieras)
        SetHighlight(true, Color.white, 3f);
    }

    public void Release(Vector3 mouseDirectionIgnored)
    {
        isGrabbed = false;
        rb.useGravity = true;

        SetHighlight(false);

        if (isFullyCharged)
        {
            // 1. Separamos la velocidad vertical (Y) de la horizontal (X, Z)
            Vector3 horizontalVelocity = new Vector3(customThrowVelocity.x, 0f, customThrowVelocity.z);
            float verticalVelocity = customThrowVelocity.y;

            // 2. LIMITAMOS LA FUERZA HORIZONTAL: Si supera el máximo, la recortamos manteniendo la dirección
            if (horizontalVelocity.magnitude > maxHorizontalVelocity)
            {
                horizontalVelocity = horizontalVelocity.normalized * maxHorizontalVelocity;
            }

            // 3. Recomponemos el vector de fuerza final
            Vector3 finalLaunchForce = horizontalVelocity;
            // Nos aseguramos de que siempre tenga un impulso hacia arriba agradable (parábola)
            finalLaunchForce.y = Mathf.Max(verticalVelocity, 4f);

            // Torque caótico por estar cargado
            Vector3 randomTorque = new Vector3(UnityEngine.Random.Range(-30, 30), UnityEngine.Random.Range(-30, 30), UnityEngine.Random.Range(-30, 30));

            Roll(finalLaunchForce, randomTorque);
        }
        else
        {
            // Si no se agitó, se cae flojo
            rb.linearVelocity = Vector3.down * 2f;
            rb.angularVelocity = new Vector3(UnityEngine.Random.Range(-2, 2), UnityEngine.Random.Range(-2, 2), UnityEngine.Random.Range(-2, 2));
            rollSafetyTimer = rollSafetyDuration;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Table"))
        {
            // Buscamos en TODOS los componentes DiceFace que hay en los hijos del dado
            DiceFace[] todasLasCaras = GetComponentsInChildren<DiceFace>();

            foreach (DiceFace cara in todasLasCaras)
            {
                // Usamos la propiedad del Collider para ver cuál de nuestras caras está intersectando la mesa
                Collider colliderCara = cara.GetComponent<Collider>();
                if (colliderCara != null && colliderCara.bounds.Intersects(other.bounds))
                {
                    faceOnGround = cara;
                    // Debug.Log($"[Dice] Detectada cara boca abajo: {faceOnGround.GetFaceValue()}");
                    break;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Table"))
        {
            faceOnGround = null;
        }
    }

    public int GetFinalResult()
    {
        if (faceOnGround != null)
        {
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                activeModifiers[i].OnRoll(this);
            }
            return faceOnGround.GetFaceValue();
        }

        // Caso de emergencia: si se quedó de canto o flotando, devolvemos un valor seguro
        return -99;
    }

    // Método de la interfaz modificado para aceptar parámetros dinámicos
    public void SetHighlight(bool active, Color color = default, float width = 5f)
    {
        if (outlineComponent != null)
        {
            // Si el color es el por defecto (transparente), usamos negro por seguridad
            if (color == default) color = Color.black;

            outlineComponent.enabled = active;
            outlineComponent.OutlineColor = color;
            outlineComponent.OutlineWidth = width;
        }
    }

    private void CalculateTargetPosition()
    {
        Plane horizonPlane = new Plane(Vector3.up, new Vector3(0, grabHeight, 0));
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (horizonPlane.Raycast(ray, out float distance))
        {
            targetWorldPosition = ray.GetPoint(distance);
        }
    }

    private void TriggerChargeFeedback()
    {
        Debug.Log("¡DADO CARGADO!");
    }

    public void Roll(Vector3 force, Vector3 torque)
    {
        hasRolled = true;
        rollSafetyTimer = rollSafetyDuration;

        rb.AddForce(force, ForceMode.VelocityChange);
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    // Añade esto en tu PhysicalDice.cs (y borra el IsBurnt si quieres limpiar el código)
    public List<IModifier> GetActiveModifiers()
    {
        return activeModifiers;
    }

    public int GetResult() => UnityEngine.Random.Range(1, 5);
    // Reemplaza estos métodos en tu PhysicalDice.cs:

    public void ApplyModifier(IModifier modifier)
    {
        // Buscamos si ya existe UN modificador con el mismo nombre en la lista
        IModifier existente = activeModifiers.Find(m => m.NameKey == modifier.NameKey);

        if (existente != null)
        {
            // Si ya existe (ej: ya estaba en llamas), le volvemos a aplicar el efecto 
            // para que refresque el tiempo o aumente la intensidad del fuego
            existente.OnApply(this);
        }
        else
        {
            // Si es un efecto nuevo, lo añadimos y lo activamos
            activeModifiers.Add(modifier);
            modifier.OnApply(this);
        }
    }

    public void RemoveModifier(IModifier modifier)
    {
        // Buscamos por nombre exacto para asegurarnos de borrarlo de la lista
        IModifier aEliminar = activeModifiers.Find(m => m.NameKey == modifier.NameKey);

        if (aEliminar != null)
        {
            activeModifiers.Remove(aEliminar);
            Debug.Log($"[Dice] Modificador '{modifier.NameKey}' eliminado con éxito de {gameObject.name}.");
        }
    }
}