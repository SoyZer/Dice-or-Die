using UnityEngine;

public class FlameDice : PhysicalDice
{
    [Header("Configuración Ígnea (DFlame)")]
    [SerializeField] private float collisionForceThreshold = 3f;   // Fuerza mínima del golpe para contagiar el fuego
    [SerializeField] private ParticleSystem fireImpactParticles;   // Partículas de explosión de fuego al chocar

    private void OnCollisionEnter(Collision collision)
    {
        // Comprobamos si chocamos contra otro dado físico que herede de PhysicalDice
        PhysicalDice otroDado = collision.gameObject.GetComponent<PhysicalDice>();

        if (otroDado != null && otroDado != this)
        {
            // Verificamos si el golpe ha sido lo suficientemente fuerte (velocidad relativa)
            if (collision.relativeVelocity.magnitude > collisionForceThreshold)
            {
                // 1. Instanciar partículas de fuego justo en el punto de contacto del choque
                if (fireImpactParticles != null)
                {
                    ContactPoint puntoContacto = collision.contacts[0];
                    Instantiate(fireImpactParticles, puntoContacto.point, Quaternion.identity);
                }

                // 2. Como PhysicalDice ya tiene los métodos de modificadores, le prendemos fuego
                OnFireModifier fuego = new OnFireModifier();
                otroDado.ApplyModifier(fuego);

                Debug.Log($"[DFlame] ¡{gameObject.name} ha golpeado con fuerza a {otroDado.gameObject.name} y le ha contagiado el fuego!");
            }
        }
    }
}