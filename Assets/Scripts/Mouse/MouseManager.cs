using UnityEngine;

public class MouseManager : MonoBehaviour
{
    private IGrabbable currentGrabbed;
    private IGrabbable lastHovered; // Guarda el dado que estamos mirando

    void Update()
    {
        // 1. GESTIÓN DEL HOVER (Pasar el ratón por encima)
        // Solo buscamos hovers si no tenemos ningún dado agarrado ya
        if (currentGrabbed == null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IGrabbable hovered = hit.collider.GetComponent<IGrabbable>();

                // Si cambiamos de objeto mirado
                if (hovered != lastHovered)
                {
                    // Apagamos el anterior si existía
                    if (lastHovered != null) lastHovered.SetHighlight(false);

                    // Encendemos el nuevo: NEGRO y MEDIDA 3
                    if (hovered != null) hovered.SetHighlight(true, Color.black, 5f);

                    lastHovered = hovered;
                }
            }
            else
            {
                // Si el ratón apunta al vacío, apagamos el último que mirábamos
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
            currentGrabbed.Grab(); // Esto cambiará internamente el outline a BLANCO
            lastHovered = null;    // Limpiamos el hover mientras esté agarrado
        }

        // 3. SOLTAR EL CLIC: LANZAR
        if (Input.GetMouseButtonUp(0) && currentGrabbed != null)
        {
            currentGrabbed.Release(Vector3.zero); // Esto apagará el outline
            currentGrabbed = null;
        }
    }
}