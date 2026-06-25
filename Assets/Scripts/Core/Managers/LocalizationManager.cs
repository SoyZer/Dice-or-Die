using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language { Spanish, English }
    [Header("Configuración de Idioma")]
    [SerializeField] private Language currentLanguage = Language.Spanish;

    // Diccionario para almacenar las traducciones en memoria: Clave -> Texto
    private Dictionary<string, string> activeTranslations = new Dictionary<string, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalizationFile(); // Cargamos el archivo CSV al arrancar
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Carga el archivo CSV desde la carpeta Resources y parsea el idioma actual.
    /// </summary>
    public void LoadLocalizationFile()
    {
        activeTranslations.Clear();

        // Buscamos el archivo "localization.csv" en Assets/Resources/Localization/
        TextAsset csvFile = Resources.Load<TextAsset>("Localization/DiceorDie_Lenguages");

        if (csvFile == null)
        {
            Debug.LogError("[Localization] No se encontró el archivo 'localization.csv' en Resources/Localization/");
            return;
        }

        // Dividimos el archivo en líneas (cada fila del Excel)
        string[] lineas = csvFile.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lineas.Length <= 1) return; // Archivo vacío o solo tiene la cabecera

        // Detectamos en qué columna está nuestro idioma basándonos en la primera línea (cabecera)
        string[] cabecera = lineas[0].Split(';');
        int columnaIdiomaObjetivo = -1;

        string idiomaBuscado = currentLanguage.ToString(); // "Spanish" o "English"
        for (int i = 0; i < cabecera.Length; i++)
        {
            if (cabecera[i].Trim().Equals(idiomaBuscado, StringComparison.OrdinalIgnoreCase))
            {
                columnaIdiomaObjetivo = i;
                break;
            }
        }

        if (columnaIdiomaObjetivo == -1)
        {
            Debug.LogError($"[Localization] No se encontró la columna para el idioma {idiomaBuscado} en el CSV.");
            return;
        }

        // Recorremos el resto de filas (empezando en la 1 para saltar la cabecera)
        for (int i = 1; i < lineas.Length; i++)
        {
            string[] celdas = lineas[i].Split(';');

            if (celdas.Length > columnaIdiomaObjetivo)
            {
                string key = celdas[0].Trim();
                string textoTraducido = celdas[columnaIdiomaObjetivo].Trim();

                if (!string.IsNullOrEmpty(key) && !activeTranslations.ContainsKey(key))
                {
                    activeTranslations.Add(key, textoTraducido);
                }
            }
        }

        Debug.Log($"[Localization] ¡Idioma {idiomaBuscado} cargado con éxito! {activeTranslations.Count} claves listas.");
    }

    /// <summary>
    /// Devuelve la traducción de una clave. Si no existe, devuelve la propia clave como alerta visual.
    /// </summary>
    public string GetTranslation(string key)
    {
        if (activeTranslations.TryGetValue(key, out string translation))
        {
            return translation;
        }

        Debug.LogWarning($"[Localization] Clave no encontrada: {key}");
        return key;
    }

    /// <summary>
    /// Permite cambiar el idioma en tiempo real (por ejemplo, desde un menú de opciones).
    /// </summary>
    public void ChangeLanguage(Language newLanguage)
    {
        currentLanguage = newLanguage;
        LoadLocalizationFile(); // Recargamos el diccionario con el nuevo idioma
    }
}