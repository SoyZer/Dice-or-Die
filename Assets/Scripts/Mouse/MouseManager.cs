using UnityEngine;

public class MouseManager : MonoBehaviour
{
    private IGrabbable currentGrabbed;
    private IGrabbable lastHovered;

    [Header("Configuración de la Tienda Física")]
    [SerializeField] private float shopCameraYThreshold = 8f;
    [SerializeField] private float shopHeightOffset = 3f;

    [Header("Configuración de Capas (Layers)")]
    [Tooltip("Nombre de la capa temporal para el dado agarrado (Ej: GrabbedDice)")]
    [SerializeField] private string grabbedLayerName = "GrabbedDice";

    private int originalDiceLayer; // Guarda la capa original del dado que agarramos

    void Update()
    {
        // 1. GESTIÓN DEL HOVER
        if (currentGrabbed == null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IGrabbable hovered = hit.collider.GetComponentInParent<IGrabbable>();

                if (hovered != lastHovered)
                {
                    if (lastHovered != null) lastHovered.SetHighlight(false);
                    if (hovered != null) hovered.SetHighlight(true, Color.black, 5f);

                    lastHovered = hovered;
                }
            }
            else
            {
                if (lastHovered != null)
                {
                    lastHovered.SetHighlight(false);
                    lastHovered = null;
                }
            }
        }

        // 2. CLIC IZQUIERDO: AGARRAR
        if (Input.GetMouseButtonDown(0) && lastHovered != null)
        {
            currentGrabbed = lastHovered;

            if (currentGrabbed is Component component)
            {
                // Buscamos el objeto raíz del dado para no perder sus físicas ni jerarquía
                GameObject diceRoot = component.transform.root.gameObject;

                // Guardamos su capa original antes de cambiarla
                originalDiceLayer = diceRoot.layer;

                // CAMBIO DE CAPA: Pasamos todo el dado a la capa fantasma 'GrabbedDice'
                SetLayerRecursively(diceRoot, LayerMask.NameToLayer(grabbedLayerName));

                Rigidbody rb = diceRoot.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }

            currentGrabbed.Grab();
            lastHovered = null;
        }

        // --- DINÁMICA DE ALTURA SEGÚN EL MOVIMIENTO DE LA CÁMARA ---
        if (currentGrabbed != null)
        {
            if (currentGrabbed is Component component)
            {
                Rigidbody rb = component.GetComponentInParent<Rigidbody>();
                if (rb != null && Camera.main.transform.position.y > shopCameraYThreshold)
                {
                    rb.AddForce(Vector3.up * 155f, ForceMode.Acceleration);
                }
            }
        }

        // 3. SOLTAR EL CLIC: ¿MESA O DESTRUCCIÓN?
        if (Input.GetMouseButtonUp(0) && currentGrabbed != null)
        {
            bool droppedInDeleteZone = false;

            if (currentGrabbed is Component component)
            {
                GameObject diceRoot = component.transform.root.gameObject;

                // Lanzamos un rayo hacia abajo para detectar la trituradora
                if (Physics.Raycast(component.transform.position, Vector3.down, out RaycastHit hit, 50f))
                {
                    if (hit.collider.CompareTag("DeleteZone"))
                    {
                        Debug.Log("Trituradora Detectada");
                        droppedInDeleteZone = true;
                    }
                }

                if (droppedInDeleteZone)
                {
                    Debug.Log($"[Tienda] Dado soltado en la zona roja. Destruyendo: {diceRoot.name}");
                    Destroy(diceRoot);
                    currentGrabbed = null;
                }
                else
                {
                    // --- SOLUCIÓN POR ROTACIÓN DE CÁMARA ---
                    // En Unity, las rotaciones se guardan internamente como Quaternions. 
                    // Para leer los grados limpios (0 a 360) usamos eulerAngles.x
                    float currentCameraRotationX = Camera.main.transform.eulerAngles.x;

                    // Si por algún motivo da un valor negativo o extraño, lo normalizamos
                    if (currentCameraRotationX > 180) currentCameraRotationX -= 360;

                    // Si la rotación de la cámara es menor de 50 grados, significa que estás mirando la tienda (X:15)
                    // y NO la mesa (X:85)
                    if (currentCameraRotationX < 50f)
                    {
                        // Teletransportamos el dado a una posición segura y centrada justo sobre el tapete
                        // Ajusta este Vector3 (X, Y, Z) según las coordenadas reales de tu mesa de juego
                        diceRoot.transform.position = new Vector3(0f, 30f, 17f);
                    }

                    // Limpiamos fuerzas acumuladas del arrastre para una caída vertical limpia
                    Rigidbody rb = diceRoot.GetComponentInChildren<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    // RESTAURAR CAPA: Devolvemos el dado a su capa original
                    SetLayerRecursively(diceRoot, originalDiceLayer);

                    currentGrabbed.Release(Vector3.zero);
                    currentGrabbed = null;
                }
            }
        }
    }

    // Función auxiliar para cambiar la capa al objeto y a todos sus hijos (esencial para poliedros complejos)
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}