using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol Guerrero — ofensivo, prioriza atacar al más débil.
    /// Se cura solo si la vida es crítica (umbral bajo), sin distracciones de soporte.
    /// Válido para cualquier enemigo de rol melee/ofensivo: goblins guerreros, 
    /// soldados humanos, bestias, etc.
    /// </summary>
    public class RolGuerrero : IDecisionRol
    {
        public ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                // Curarse solo si vida es crítica
                new Secuencia(
                    new CondicionVidaBaja(0.20f),
                    new AccionCurarse()
                ),
                // Atacar al jugador con menos porcentaje de vida
                new AccionAtacarDebil()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
