namespace IA
{
    /// <summary>
    /// Invierte el resultado del hijo.
    /// </summary>
    public class Inversor : NodoIA
    {
        private NodoIA hijo;
        
        public Inversor(NodoIA hijo) => this.hijo = hijo;
        
        public override EstadoNodo Evaluar()
        {
            hijo.Configurar(enemigo, jugadores, aliados);
            var estado = hijo.Evaluar();
            
            if (estado == EstadoNodo.Exito) return EstadoNodo.Fallo;
            if (estado == EstadoNodo.Fallo) return EstadoNodo.Exito;
            return EstadoNodo.Ejecutando;
        }
    }
    
    /// <summary>
    /// Repite el hijo N veces o hasta que falle.
    /// </summary>
    public class Repetidor : NodoIA
    {
        private NodoIA hijo;
        private int repeticiones;
        private int contadorActual = 0;
        
        public Repetidor(NodoIA hijo, int repeticiones)
        {
            this.hijo = hijo;
            this.repeticiones = repeticiones;
        }
        
        public override EstadoNodo Evaluar()
        {
            while (contadorActual < repeticiones)
            {
                hijo.Configurar(enemigo, jugadores, aliados);
                var estado = hijo.Evaluar();
                
                if (estado == EstadoNodo.Fallo)
                {
                    contadorActual = 0;
                    return EstadoNodo.Fallo;
                }
                
                if (estado == EstadoNodo.Ejecutando)
                    return EstadoNodo.Ejecutando;
                
                contadorActual++;
            }
            
            contadorActual = 0;
            return EstadoNodo.Exito;
        }
        
        public override void Resetear() => contadorActual = 0;
    }
}
