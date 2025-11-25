using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Interfaces;


namespace Managers
{
    public class TurnManager
    {
        private Queue<IEntidadCombate> colaDeTurnos;
        private IEntidadCombate entidadActual;
        
        public IEntidadCombate EntidadActual => entidadActual;
        
        public void InicializarTurnos(List<IEntidadCombate> todasLasEntidades)
        {
            // Ordenar por velocidad (mayor velocidad = primero en actuar)
            var ordenadas = todasLasEntidades
                .Where(e => e.EstaVivo())
                .OrderByDescending(e => e.Velocidad)
                .ToList();
            
            colaDeTurnos = new Queue<IEntidadCombate>(ordenadas);
            
            if (colaDeTurnos.Count > 0)
            {
                entidadActual = colaDeTurnos.Dequeue();
                colaDeTurnos.Enqueue(entidadActual); // Re-encolar
            }
        }
        
        public void SiguienteTurno()
        {
            if (colaDeTurnos.Count == 0) return;
            
            // Buscar la siguiente entidad viva
            int intentos = 0;
            while (intentos < colaDeTurnos.Count)
            {
                entidadActual = colaDeTurnos.Dequeue();
                
                if (entidadActual.EstaVivo() && entidadActual.PuedeActuar())
                {
                    colaDeTurnos.Enqueue(entidadActual); // Re-encolar
                    return;
                }
                
                // Si está muerta, no re-encolar
                intentos++;
            }
            
            entidadActual = null; // No quedan entidades vivas
        }
        
        public void EliminarEntidad(IEntidadCombate entidad)
        {
            colaDeTurnos = new Queue<IEntidadCombate>(
                colaDeTurnos.Where(e => e != entidad)
            );
        }
        
        public bool HayEntidadesVivas()
        {
            return colaDeTurnos.Any(e => e.EstaVivo());
        }
        
        public List<IEntidadCombate> ObtenerOrdenActual()
        {
            return colaDeTurnos.ToList();
        }
    }
}