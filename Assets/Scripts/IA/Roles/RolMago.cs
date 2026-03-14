using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol Mago — prioriza habilidades especiales (Ataque, Control, Debuff) antes que ataque básico.
    /// Se cura si la vida baja a un umbral moderado.
    /// Válido para magos, chamanes, hechiceros, elementalistas, etc.
    /// </summary>
    public class RolMago : IDecisionRol
    {
        public ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                // Autopreservación moderada
                new Secuencia(
                    new CondicionVidaBaja(0.30f),
                    new AccionCurarse()
                ),
                // Intentar controlar/debuffear con probabilidad
                new Secuencia(
                    new CondicionProbabilidad(0.45f),
                    new AccionControlarObjetivo()
                ),
                // Atacar al más débil (priorizará habilidades de Ataque/Debuff por categoría)
                new AccionAtacarDebil(),
                // Fallback seguro
                new AccionAtacarAleatorio()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
