using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Contrato que debe cumplir cada rol de combate.
    /// Implementar este archivo para definir la lógica de decisión de un rol específico.
    /// El rol es independiente del tipo de enemigo (Goblin, Humano, Elemental, etc.).
    /// </summary>
    public interface IDecisionRol
    {
        /// <summary>
        /// Decide la acción para este turno.
        /// </summary>
        /// <param name="yo">El enemigo que está tomando la decisión.</param>
        /// <param name="jugadores">Objetivos hostiles (los jugadores del party).</param>
        /// <param name="aliados">Aliados del enemigo (otros enemies en combate).</param>
        /// <returns>El resultado con objetivo + habilidad, o null si no hay acción válida.</returns>
        ResultadoIA Decidir(
            Enemigos yo,
            List<IEntidadCombate> jugadores,
            List<IEntidadCombate> aliados
        );
    }
}
