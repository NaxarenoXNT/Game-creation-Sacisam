using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol básico para enemigos genéricos.
    /// Comportamiento: se cura si la vida es crítica (con probabilidad), ataca aleatoriamente.
    /// Cualquier enemigo sin un rol definido usa este comportamiento.
    /// </summary>
    public class RolBasico : IDecisionRol
    {
        public ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                new Secuencia(
                    new CondicionVidaBaja(0.25f),
                    new CondicionProbabilidad(0.6f),
                    new AccionCurarse()
                ),
                new AccionAtacarAleatorio()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
