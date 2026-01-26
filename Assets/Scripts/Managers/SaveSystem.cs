using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Sistema de guardado y carga para progreso del jugador.
    /// Soporta multiples slots y auto-guardado.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FOLDER = "Saves";
        private const string SAVE_EXTENSION = ".sav";
        private const string AUTO_SAVE_NAME = "autosave";
        
        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        
        /// <summary>
        /// Inicializa el sistema de guardado creando la carpeta si no existe.
        /// </summary>
        public static void Inicializar()
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
                Debug.Log("Carpeta de guardado creada: " + SavePath);
            }
        }
        
        /// <summary>
        /// Guarda los datos en un slot especifico.
        /// </summary>
        public static bool Guardar(string slotName, SaveData datos)
        {
            try
            {
                Inicializar();
                
                string filePath = Path.Combine(SavePath, slotName + SAVE_EXTENSION);
                string json = JsonUtility.ToJson(datos, true);
                
                File.WriteAllText(filePath, json);
                
                Debug.Log("Partida guardada en: " + filePath);
                EventBus.Publicar(new EventoPartidaGuardada { SlotName = slotName, Exitoso = true });
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error al guardar: " + ex.Message);
                EventBus.Publicar(new EventoPartidaGuardada { SlotName = slotName, Exitoso = false });
                return false;
            }
        }
        
        /// <summary>
        /// Carga los datos de un slot especifico.
        /// </summary>
        public static SaveData Cargar(string slotName)
        {
            try
            {
                string filePath = Path.Combine(SavePath, slotName + SAVE_EXTENSION);
                
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning("Archivo de guardado no encontrado: " + filePath);
                    return null;
                }
                
                string json = File.ReadAllText(filePath);
                SaveData datos = JsonUtility.FromJson<SaveData>(json);
                
                Debug.Log("Partida cargada desde: " + filePath);
                EventBus.Publicar(new EventoPartidaCargada { SlotName = slotName, Datos = datos });
                
                return datos;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error al cargar: " + ex.Message);
                return null;
            }
        }
        
        /// <summary>
        /// Realiza un auto-guardado.
        /// </summary>
        public static bool AutoGuardar(SaveData datos)
        {
            return Guardar(AUTO_SAVE_NAME, datos);
        }
        
        /// <summary>
        /// Carga el auto-guardado.
        /// </summary>
        public static SaveData CargarAutoGuardado()
        {
            return Cargar(AUTO_SAVE_NAME);
        }
        
        /// <summary>
        /// Verifica si existe un guardado en un slot.
        /// </summary>
        public static bool ExisteGuardado(string slotName)
        {
            string filePath = Path.Combine(SavePath, slotName + SAVE_EXTENSION);
            return File.Exists(filePath);
        }
        
        /// <summary>
        /// Elimina un guardado.
        /// </summary>
        public static bool EliminarGuardado(string slotName)
        {
            try
            {
                string filePath = Path.Combine(SavePath, slotName + SAVE_EXTENSION);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log("Guardado eliminado: " + slotName);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error al eliminar guardado: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Obtiene la lista de todos los guardados disponibles.
        /// </summary>
        public static List<SaveSlotInfo> ObtenerGuardados()
        {
            Inicializar();
            
            var guardados = new List<SaveSlotInfo>();
            string[] archivos = Directory.GetFiles(SavePath, "*" + SAVE_EXTENSION);
            
            foreach (string archivo in archivos)
            {
                try
                {
                    string nombre = Path.GetFileNameWithoutExtension(archivo);
                    FileInfo info = new FileInfo(archivo);
                    
                    // Cargar datos basicos para mostrar info
                    string json = File.ReadAllText(archivo);
                    SaveData datos = JsonUtility.FromJson<SaveData>(json);
                    
                    guardados.Add(new SaveSlotInfo
                    {
                        SlotName = nombre,
                        FechaGuardado = info.LastWriteTime,
                        NivelJugador = datos.nivelJugador,
                        TiempoJugado = datos.tiempoJugadoSegundos,
                        UbicacionActual = datos.escenaActual
                    });
                }
                catch
                {
                    // Ignorar archivos corruptos
                }
            }
            
            return guardados;
        }
    }
    
    /// <summary>
    /// Datos serializables para guardar el progreso.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Metadatos
        public string version = "1.0";
        public long timestampGuardado;
        public float tiempoJugadoSegundos;
        
        // Ubicacion
        public string escenaActual;
        public Vector3Serializable posicionJugador;
        
        // Progresion del jugador
        public int nivelJugador;
        public float xpActual;
        public int vidaActual;
        public int vidaMaxima;
        public int manaActual;
        public int manaMaximo;
        
        // Stats base
        public int ataque;
        public float defensa;
        public int velocidad;
        
        // Inventario y equipo (IDs)
        public List<string> inventarioIds = new List<string>();
        public List<string> equipoIds = new List<string>();
        
        // Elementos desbloqueados
        public List<ElementoGuardado> elementosActivos = new List<ElementoGuardado>();
        
        // Habilidades desbloqueadas
        public List<string> habilidadesDesbloqueadas = new List<string>();
        
        // Progreso del mundo
        public List<string> cofresAbiertos = new List<string>();
        public List<string> enemigosEliminados = new List<string>();
        public List<string> misionesCompletadas = new List<string>();
        public Dictionary<string, int> contadoresMisiones = new Dictionary<string, int>();
        
        // Configuracion
        public float volumenMusica = 1f;
        public float volumenEfectos = 1f;
        
        /// <summary>
        /// Crea un SaveData con valores por defecto.
        /// </summary>
        public static SaveData CrearNuevo()
        {
            return new SaveData
            {
                timestampGuardado = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                tiempoJugadoSegundos = 0,
                escenaActual = "SampleScene",
                posicionJugador = new Vector3Serializable(0, 0, 0),
                nivelJugador = 1,
                xpActual = 0,
                vidaActual = 100,
                vidaMaxima = 100,
                manaActual = 50,
                manaMaximo = 50,
                ataque = 10,
                defensa = 5,
                velocidad = 10
            };
        }
    }
    
    /// <summary>
    /// Vector3 serializable para JSON.
    /// </summary>
    [Serializable]
    public struct Vector3Serializable
    {
        public float x, y, z;
        
        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public Vector3Serializable(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
        
        public Vector3 ToVector3() => new Vector3(x, y, z);
        
        public static implicit operator Vector3(Vector3Serializable v) => v.ToVector3();
        public static implicit operator Vector3Serializable(Vector3 v) => new Vector3Serializable(v);
    }
    
    /// <summary>
    /// Elemento guardado con su nivel y XP.
    /// </summary>
    [Serializable]
    public struct ElementoGuardado
    {
        public string elementoId;
        public int nivel;
        public float xp;
    }
    
    /// <summary>
    /// Informacion resumida de un slot de guardado.
    /// </summary>
    public struct SaveSlotInfo
    {
        public string SlotName;
        public DateTime FechaGuardado;
        public int NivelJugador;
        public float TiempoJugado;
        public string UbicacionActual;
    }
    
    // Eventos del sistema de guardado
    public struct EventoPartidaGuardada : IEvento
    {
        public string SlotName;
        public bool Exitoso;
    }
    
    public struct EventoPartidaCargada : IEvento
    {
        public string SlotName;
        public SaveData Datos;
    }
}
