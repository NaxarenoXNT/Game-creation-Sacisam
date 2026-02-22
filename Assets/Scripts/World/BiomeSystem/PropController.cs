using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Componente para objetos del mundo con interacción (cofres, NPCs, entradas a zonas, etc.).
    /// Se agrega al prefab del prop y se inicializa automáticamente al cargar el chunk.
    ///
    /// Para props puramente decorativos NO se necesita este componente.
    ///
    /// ASUNCIONES:
    /// - Existe una interfaz IInteractable o un sistema de interacción propio.
    ///   Si no la tenés, este componente funciona igual pero el método OnInteract()
    ///   tenés que llamarlo vos desde tu sistema de input/interacción.
    /// - Para interactionType "carga_zona" se asume que existe un SceneLoader o similar.
    ///   Está marcado como TODO donde necesitás conectar tu sistema.
    /// - Para interactionType "npc" se asume que existe un sistema de diálogo.
    ///   También marcado como TODO.
    /// </summary>
    public class PropController : MonoBehaviour
    {
        private PropSpawnConfig config;
        private Vector2Int chunkCoords;
        private bool isInitialized = false;

        // ─── Inicialización ──────────────────────────────────────────────────────

        /// <summary>
        /// Llamado por WorldChunkManager al spawnear el prop.
        /// </summary>
        public void Initialize(PropSpawnConfig config, Vector2Int chunkCoords)
        {
            this.config = config;
            this.chunkCoords = chunkCoords;
            isInitialized = true;

            // Si el objeto ya fue consumido, aplicar estado visual de consumido
            if (config.isConsumed)
                ApplyConsumedVisualState();
        }

        // ─── Interacción ─────────────────────────────────────────────────────────

        /// <summary>
        /// Punto de entrada de la interacción del jugador.
        /// Llamar desde tu sistema de interacción cuando el jugador interactúa con este objeto.
        /// </summary>
        public void OnInteract()
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"[PropController] OnInteract llamado antes de Initialize en {gameObject.name}");
                return;
            }

            if (config.isConsumed && config.propData.consumeOnInteract)
            {
                // Ya fue consumido, no hacer nada
                return;
            }

            switch (config.interactionType)
            {
                case "cofre":
                    HandleChest();
                    break;

                case "consumible":
                    HandleConsumable();
                    break;

                case "npc":
                    HandleNPC();
                    break;

                case "puerta":
                    HandleDoor();
                    break;

                case "carga_zona":
                    HandleZoneEntry();
                    break;

                default:
                    Debug.LogWarning($"[PropController] Tipo de interacción desconocido: '{config.interactionType}' en {config.propId}");
                    break;
            }
        }

        // ─── Handlers por tipo ───────────────────────────────────────────────────

        private void HandleChest()
        {
            // TODO: Conectar con tu sistema de loot/inventario.
            // Ejemplo:
            // LootManager.Instance.OpenChest(config.propId);

            Debug.Log($"[PropController] Cofre abierto: {config.propId}");

            if (config.propData.consumeOnInteract)
                ConsumeObject();
        }

        private void HandleConsumable()
        {
            // TODO: Conectar con tu sistema de items o efectos.
            // Ejemplo:
            // ItemManager.Instance.PickupItem(config.propId);

            Debug.Log($"[PropController] Consumible recogido: {config.propId}");
            ConsumeObject();
        }

        private void HandleNPC()
        {
            // TODO: Conectar con tu sistema de diálogo.
            // Ejemplo:
            // DialogueManager.Instance.StartDialogue(config.propId);

            Debug.Log($"[PropController] Diálogo iniciado con NPC: {config.propId}");
        }

        private void HandleDoor()
        {
            // TODO: Conectar con tu sistema de puertas/mecánicas.
            // Ejemplo:
            // Animator anim = GetComponentInChildren<Animator>();
            // anim?.SetTrigger("Open");

            Debug.Log($"[PropController] Puerta activada: {config.propId}");
        }

        private void HandleZoneEntry()
        {
            if (string.IsNullOrEmpty(config.targetZone))
            {
                Debug.LogWarning($"[PropController] ZoneEntry sin targetZone en {config.propId}");
                return;
            }

            // TODO: Conectar con tu sistema de carga de escenas/zonas.
            // Ejemplo:
            // SceneLoader.Instance.LoadZone(config.targetZone);

            Debug.Log($"[PropController] Entrando a zona: {config.targetZone}");
        }

        // ─── Estado consumido ────────────────────────────────────────────────────

        private void ConsumeObject()
        {
            config.isConsumed = true;

            // Notificar al WorldChunkManager si el estado debe persistir
            if (config.propData.persistConsumedState && WorldChunkManager.Instance != null)
            {
                WorldChunkManager.Instance.NotificarPropConsumido(config.propId, chunkCoords);
            }

            gameObject.SetActive(false);
        }

        private void ApplyConsumedVisualState()
        {
            // Por defecto simplemente desactiva el objeto.
            // Si necesitás un estado visual especial (cofre abierto, árbol talado, etc.)
            // podés sobrescribir esto en una subclase o agregar lógica aquí.
            gameObject.SetActive(false);
        }

        // ─── Acceso de solo lectura ──────────────────────────────────────────────

        public string PropId => config?.propId;
        public bool IsConsumed => config?.isConsumed ?? false;
        public string InteractionType => config?.interactionType;
    }
}