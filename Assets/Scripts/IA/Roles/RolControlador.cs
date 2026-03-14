using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol Controlador — prioriza debuffs y control de masas antes que daño.
    /// Objetivo: incapacitar al equipo enemigo para que sus aliados rematen.
    /// Válido para brujos, ilusionistas, nigromantes, enemigos de apoyo ofensivo, etc.
    /// </summary>
    public class RolControlador : IDecisionRol
    {
        public ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                // Autopreservación mínima
                new Secuencia(
                    new CondicionVidaBaja(0.25f),
                    new AccionCurarse()
                ),
                // Intentar controlar/debuffear con alta probabilidad
                new Secuencia(
                    new CondicionProbabilidad(0.65f),
                    new AccionControlarObjetivo()
                ),
                // Atacar al más débil como acción de daño secundaria
                new AccionAtacarDebil(),
                new AccionAtacarAleatorio()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
