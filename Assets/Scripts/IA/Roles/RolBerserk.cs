using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol Berserk — ataca siempre al jugador con más vida (el tanque).
    /// Sin autopreservación: ignora su propia vida para maximizar daño.
    /// Válido para berserkers, brutos, enemigos enrabiados, jefes furiosos, etc.
    /// </summary>
    public class RolBerserk : IDecisionRol
    {
        public ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                // Siempre al tanque, sin curarse primero
                new AccionAtacarTank(),
                new AccionAtacarAleatorio()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
