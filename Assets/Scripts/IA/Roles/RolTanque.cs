using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol Tanque — prioriza sobrevivir y buffear a aliados antes de atacar.
    /// Ataca al jugador con más vida para absorber aggro.
    /// Válido para guardias, caballeros, golem, colosos, etc.
    /// </summary>
    public class RolTanque : IDecisionRol
    {
        public ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                // Curarse si vida baja
                new Secuencia(
                    new CondicionVidaBaja(0.50f),
                    new AccionCurarse()
                ),
                // Buffear aliado con cierta probabilidad
                new Secuencia(
                    new CondicionProbabilidad(0.35f),
                    new AccionBuffearAliado()
                ),
                // Atacar al jugador con más vida
                new AccionAtacarTank(),
                new AccionAtacarAleatorio()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
