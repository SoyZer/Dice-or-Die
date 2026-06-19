using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VoltDice : PhysicalDice
{
    [Header("Configuración Eléctrica (DVolt)")]
    [SerializeField] private float chainDelay = 0.05f;       // Espera entre saltos 
    [SerializeField] private float reactivationForce = 20f;    // Fuerza del salto vertical

    [Header("Efecto Visual")]
    [SerializeField] private ParticleSystem sparkParticles;

    // Lo llama el RewardSystem al leer el resultado 
    public void ActivateElectricChain(int totalDiceToReactivate)
    {
        StartCoroutine(ElectricChainProximityRoutine(totalDiceToReactivate));
    }

    private IEnumerator ElectricChainProximityRoutine(int maxSaltos)
    {
        // Lista para recordar a quién hemos reactivado ya en esta cadena y no repetir
        List<PhysicalDice> dadosYaReactivados = new List<PhysicalDice>();
        dadosYaReactivados.Add(this); // Nos incluimos a nosotros mismos para no autoelegirnos

        // El punto de origen del rayo empieza siendo este propio dado (el DVolt)
        Vector3 origenRayo = transform.position;

        int saltosRealizados = 0;

        while (saltosRealizados < maxSaltos)
        {
            // 1. Buscar todos los dados de la mesa
            PhysicalDice[] todosLosDados = FindObjectsOfType<PhysicalDice>();
            PhysicalDice dadoMasCercano = null;
            float distanciaMinima = float.MaxValue;

            // 2. Encontrar el más cercano al "origenRayo" actual que no haya sido tocado aún
            foreach (PhysicalDice candidato in todosLosDados)
            {
                if (dadosYaReactivados.Contains(candidato)) continue; // Saltarse los ya tocados

                float distancia = Vector3.Distance(origenRayo, candidato.transform.position);
                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    dadoMasCercano = candidato;
                }
            }

            // Si no quedan más dados libres en la mesa, la cadena se rompe
            if (dadoMasCercano == null)
            {
                Debug.Log("[DVolt] No hay más dados cercanos disponibles en la mesa. Cadena terminada.");
                yield break;
            }

            // 3. REACTIVAR el dado más cercano encontrado
            Rigidbody targetRb = dadoMasCercano.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                // Permitimos que este dado vuelva a rodar y ganar dinero
                targetRb.isKinematic = false;
                targetRb.useGravity = true;

                // Importante: Le devolvemos el estado de "lanzado" para que el RewardSystem lo vuelva a leer al parar
                // dadoMasCercano.ResetForNewRoll(); // O la lógica que uses para reactivar su hasRolled

                // Impulso físico caótico hacia arriba y lados
                Vector3 simulatedThrowVelocity = new Vector3(
                    Random.Range(-4f, 4f),
                    reactivationForce + Random.Range(0f, 10f), // Buena fuerza vertical
                    Random.Range(-4f, 4f)
                );

                // 4. Calculamos una rotación caótica muy rápida (frenesí de rotación)
                Vector3 simulatedTorque = new Vector3(
                    Random.Range(-40f, 40f),
                    Random.Range(-40f, 40f),
                    Random.Range(-40f, 40f)
                );

                // 5. ¡Llamamos al método oficial Roll() del dado objetivo!
                // Esto hará que se active TODA la lógica real: estados de lanzamiento, efectos visuales de fuego/brillo, etc.
                dadoMasCercano.Roll(simulatedThrowVelocity, simulatedTorque);

                // Efecto visual en la posición del dado golpeado
                if (sparkParticles != null)
                {
                    Instantiate(sparkParticles, dadoMasCercano.transform.position, Quaternion.identity);
                }

                Debug.Log($"[DVolt] Salto {saltosRealizados + 1}: El rayo viaja a {dadoMasCercano.gameObject.name}");

                // 4. PREPARAR EL SIGUIENTE SALTO
                dadosYaReactivados.Add(dadoMasCercano); // Lo marcamos como visitado
                origenRayo = dadoMasCercano.transform.position; // ¡El próximo rayo saldrá desde ESTE dado!
                saltosRealizados++;

                // Esperamos la milésima de segundo antes del siguiente chispazo 
                yield return new WaitForSeconds(chainDelay);
            }
        }

        Debug.Log("[DVolt] Cadena de rayos completada con éxito.");
    }
}