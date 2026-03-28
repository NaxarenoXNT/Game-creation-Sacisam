using UnityEngine;
using Managers;

namespace World.Travel
{
    /// <summary>
    /// Implementación por defecto de ITravelPartyHandler.
    /// Reposiciona al party activo en un arco circular alrededor del personaje principal
    /// al finalizar un viaje rápido.
    ///
    /// - El personaje principal (MainCharacter) NO se mueve aquí: ya fue teleportado por TravelManager.
    /// - Los personajes estacionados NO se mueven (solo afecta al ActiveParty).
    /// - Intenta bajar cada personaje al terreno mediante un Raycast vertical.
    /// </summary>
    public class TravelPartyHandler : ITravelPartyHandler
    {
        private readonly float _radius;
        private readonly LayerMask _terrainMask;

        /// <param name="radius">Radio del arco de formación alrededor del main (unidades).</param>
        /// <param name="terrainMask">LayerMask para el Raycast de ajuste de terreno. Usar ~0 si no importa.</param>
        public TravelPartyHandler(float radius = 3f, LayerMask terrainMask = default)
        {
            _radius      = radius;
            _terrainMask = terrainMask == default ? ~0 : terrainMask;
        }

        public void RepositionParty(Vector3 mainPosition)
        {
            var partyManager = PlayerPartyManager.Instance;
            if (partyManager == null)
            {
                Debug.LogWarning("[TravelPartyHandler] PlayerPartyManager no disponible.");
                return;
            }

            var activeParty = partyManager.ActiveParty;
            var main        = partyManager.MainCharacter;
            int count       = activeParty.Count;

            if (count == 0) return;

            // Construir lista de miembros a reubicar (excluir el main: ya está en destino)
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                var member = activeParty[i];
                if (member == null || member == main || member.gameObject == null) continue;

                Vector3 targetPos = CalculatePosition(mainPosition, placed, count - 1);
                SetPosition(member.transform, targetPos);
                placed++;
            }

            if (placed > 0)
                Debug.Log($"[TravelPartyHandler] {placed} miembro(s) reposicionados alrededor de {mainPosition}.");
        }

        // ── Privado ──────────────────────────────────────────────────────────────

        private Vector3 CalculatePosition(Vector3 center, int index, int total)
        {
            // Distribuir en un semicírculo detrás del main (evitar que aparezcan adelante)
            float range       = total > 1 ? 180f : 0f;
            float startAngle  = -range * 0.5f - 90f; // centrado hacia atrás
            float step        = total > 1 ? range / (total - 1) : 0f;
            float angleDeg    = startAngle + step * index;
            float angleRad    = angleDeg * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * _radius;
            Vector3 pos    = center + offset;

            // Ajustar altura al terreno
            return SnapToTerrain(pos);
        }

        private static void SetPosition(Transform t, Vector3 pos)
        {
            if (t.TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
                t.position = pos;
                cc.enabled = true;
            }
            else
            {
                t.position = pos;
            }
        }

        private Vector3 SnapToTerrain(Vector3 pos)
        {
            const float rayOriginHeight = 50f;
            const float rayDistance     = 100f;

            if (Physics.Raycast(pos + Vector3.up * rayOriginHeight, Vector3.down, out RaycastHit hit,
                                rayDistance, _terrainMask))
            {
                return hit.point;
            }

            return pos;
        }
    }
}
