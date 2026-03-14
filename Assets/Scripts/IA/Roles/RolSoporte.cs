using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol Soporte — buffea y cura con más agresividad que el Sanador.
    /// Diferencia con Sanador: umbral de curación más bajo (actúa antes),
    /// mayor probabilidad de buff y no necesita aliado muy herido para actuar.
    /// Válido para bardos, chamanes de apoyo, acolitos, etc.
    /// </summary>
    public class RolSoporte : IDecisionRol
    {
        public ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                // Curar aliado herido (umbral más generoso que Sanador)
                new Secuencia(
                    new CondicionAliadoHerido(0.70f),
                    new AccionCurarAliado()
                ),
                // Curarse a sí mismo si vida moderada
                new Secuencia(
                    new CondicionVidaBaja(0.35f),
                    new AccionCurarse()
                ),
                // Buffear con probabilidad media
                new Secuencia(
                    new CondicionProbabilidad(0.50f),
                    new AccionBuffearAliado()
                ),
                // Fallback
                new AccionAtacarAleatorio()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
