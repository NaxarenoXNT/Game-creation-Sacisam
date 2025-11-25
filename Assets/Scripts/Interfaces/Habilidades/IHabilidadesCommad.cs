using System.Collections.Generic;
using Interfaces;



namespace Habilidades
{
    public interface IHabilidadesCommad
    {
        // === Ejecución y Validación ===

        /// Verifica si la habilidad puede ser usada por el invocador contra el objetivo (coste, restricciones de facción, etc.).
        
        bool EsViable(IEntidadCombate invocador, IEntidadCombate objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos);

        /// Ejecuta la lógica central de la habilidad.
        /// Delega a los IAbilityEffect para aplicar cambios de estado.
        
        void Ejecutar(IEntidadCombate invocador, IEntidadCombate objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos);

        // === Datos para la IA/UI ===
        
        
        /// Devuelve el ScriptableObject de datos que implementa esta interfaz.
        HabilidadData ObtenerDatos();
    }
}