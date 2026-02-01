using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA
{
    /// <summary>
    /// Cerebro de IA que maneja el arbol de comportamiento.
    /// </summary>
    [System.Serializable]
    public class CerebroIA
    {
        private NodoIA arbolRaiz;
        private Enemigos enemigo;
        
        public CerebroIA(NodoIA arbolRaiz)
        {
            this.arbolRaiz = arbolRaiz;
        }
        
        public void Configurar(Enemigos enemigo)
        {
            this.enemigo = enemigo;
        }
        
        /// <summary>
        /// Evalua el arbol y retorna la decision.
        /// </summary>
        public ResultadoIA Decidir(List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            ContextoIA.UltimoResultado = null;
            
            arbolRaiz.Configurar(enemigo, jugadores, aliados);
            arbolRaiz.Evaluar();
            
            return ContextoIA.UltimoResultado;
        }
        
        /// <summary>
        /// Crea un cerebro basico para enemigos simples.
        /// </summary>
        public static CerebroIA CrearBasico()
        {
            var arbol = new Selector(
                // Si vida baja, intentar curarse
                new Secuencia(
                    new CondicionVidaBaja(0.25f),
                    new CondicionProbabilidad(0.5f),
                    new AccionCurarse()
                ),
                // Si hay jugador debil, atacarlo
                new Secuencia(
                    new CondicionJugadorDebil(0.3f),
                    new AccionAtacarDebil()
                ),
                // Por defecto, atacar aleatorio
                new AccionAtacarAleatorio()
            );
            
            return new CerebroIA(arbol);
        }
        
        /// <summary>
        /// Crea un cerebro agresivo que siempre ataca al mas debil.
        /// </summary>
        public static CerebroIA CrearAgresivo()
        {
            var arbol = new Selector(
                new AccionAtacarDebil(),
                new AccionAtacarAleatorio()
            );
            
            return new CerebroIA(arbol);
        }
        
        /// <summary>
        /// Crea un cerebro defensivo que prioriza sobrevivir.
        /// </summary>
        public static CerebroIA CrearDefensivo()
        {
            var arbol = new Selector(
                new Secuencia(
                    new CondicionVidaBaja(0.5f),
                    new AccionCurarse()
                ),
                new AccionAtacarTank()
            );
            
            return new CerebroIA(arbol);
        }
    }
}
