using System.Collections.Generic;

namespace IA
{
    /// <summary>
    /// Ejecuta hijos en secuencia hasta que uno falle.
    /// </summary>
    public class Secuencia : NodoIA
    {
        private List<NodoIA> hijos = new List<NodoIA>();
        private int indiceActual = 0;
        
        public Secuencia(params NodoIA[] nodos)
        {
            hijos.AddRange(nodos);
        }
        
        public override EstadoNodo Evaluar()
        {
            while (indiceActual < hijos.Count)
            {
                hijos[indiceActual].Configurar(enemigo, jugadores, aliados);
                var estado = hijos[indiceActual].Evaluar();
                
                if (estado == EstadoNodo.Fallo)
                {
                    indiceActual = 0;
                    return EstadoNodo.Fallo;
                }
                
                if (estado == EstadoNodo.Ejecutando)
                    return EstadoNodo.Ejecutando;
                
                indiceActual++;
            }
            
            indiceActual = 0;
            return EstadoNodo.Exito;
        }
        
        public override void Resetear()
        {
            indiceActual = 0;
            foreach (var hijo in hijos) hijo.Resetear();
        }
    }
    
    /// <summary>
    /// Ejecuta hijos hasta que uno tenga exito.
    /// </summary>
    public class Selector : NodoIA
    {
        private List<NodoIA> hijos = new List<NodoIA>();
        
        public Selector(params NodoIA[] nodos)
        {
            hijos.AddRange(nodos);
        }
        
        public override EstadoNodo Evaluar()
        {
            foreach (var hijo in hijos)
            {
                hijo.Configurar(enemigo, jugadores, aliados);
                var estado = hijo.Evaluar();
                
                if (estado == EstadoNodo.Exito)
                    return EstadoNodo.Exito;
                    
                if (estado == EstadoNodo.Ejecutando)
                    return EstadoNodo.Ejecutando;
            }
            
            return EstadoNodo.Fallo;
        }
        
        public override void Resetear()
        {
            foreach (var hijo in hijos) hijo.Resetear();
        }
    }
    
    /// <summary>
    /// Selecciona un hijo aleatorio para ejecutar.
    /// </summary>
    public class SelectorAleatorio : NodoIA
    {
        private List<NodoIA> hijos = new List<NodoIA>();
        
        public SelectorAleatorio(params NodoIA[] nodos)
        {
            hijos.AddRange(nodos);
        }
        
        public override EstadoNodo Evaluar()
        {
            if (hijos.Count == 0) return EstadoNodo.Fallo;
            
            int indice = UnityEngine.Random.Range(0, hijos.Count);
            hijos[indice].Configurar(enemigo, jugadores, aliados);
            return hijos[indice].Evaluar();
        }
    }
}
