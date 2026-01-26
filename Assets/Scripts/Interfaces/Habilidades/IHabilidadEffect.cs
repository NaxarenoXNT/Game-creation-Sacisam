using Interfaces;
using Padres;
using System.Collections.Generic;

namespace Habilidades
{
    public interface IHabilidadEffect
    {
        /// Aplica el efecto de la habilidad. Es lógica pura, sin Unity.
        void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos);
    }
}