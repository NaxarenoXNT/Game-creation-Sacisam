using System.Collections.Generic;
using UnityEngine;
using Interfaces;
using Padres;

namespace IA
{
    /// <summary>
    /// Sistema de IA modular basado en arboles de comportamiento simplificados.
    /// Permite crear comportamientos complejos para enemigos.
    /// </summary>
    public abstract class NodoIA
    {
        public enum EstadoNodo { Exito, Fallo, Ejecutando }
        
        protected Enemigos enemigo;
        protected List<IEntidadCombate> jugadores;
        protected List<IEntidadCombate> aliados;
        
        public void Configurar(Enemigos enemigo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            this.enemigo = enemigo;
            this.jugadores = jugadores;
            this.aliados = aliados;
        }
        
        public abstract EstadoNodo Evaluar();
        public virtual void Resetear() { }
    }
    
    // =================================================================
    // =================== NODOS COMPUESTOS ============================
    // =================================================================
    
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
            
            int indice = Random.Range(0, hijos.Count);
            hijos[indice].Configurar(enemigo, jugadores, aliados);
            return hijos[indice].Evaluar();
        }
    }
    
    // =================================================================
    // =================== NODOS DECORADORES ===========================
    // =================================================================
    
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
    
    // =================================================================
    // =================== NODOS CONDICIONALES =========================
    // =================================================================
    
    /// <summary>
    /// Verifica si la vida del enemigo esta por debajo de un porcentaje.
    /// </summary>
    public class CondicionVidaBaja : NodoIA
    {
        private float porcentaje;
        
        public CondicionVidaBaja(float porcentaje = 0.3f) => this.porcentaje = porcentaje;
        
        public override EstadoNodo Evaluar()
        {
            float vidaPorcentaje = (float)enemigo.VidaActual_Entidad / enemigo.Vida_Entidad;
            return vidaPorcentaje <= porcentaje ? EstadoNodo.Exito : EstadoNodo.Fallo;
        }
    }
    
    /// <summary>
    /// Verifica si hay jugadores con vida baja.
    /// </summary>
    public class CondicionJugadorDebil : NodoIA
    {
        private float porcentaje;
        
        public CondicionJugadorDebil(float porcentaje = 0.3f) => this.porcentaje = porcentaje;
        
        public override EstadoNodo Evaluar()
        {
            foreach (var jugador in jugadores)
            {
                if (!jugador.EstaVivo()) continue;
                
                float vidaPorcentaje = (float)jugador.VidaActual_Entidad / jugador.Vida_Entidad;
                if (vidaPorcentaje <= porcentaje)
                    return EstadoNodo.Exito;
            }
            return EstadoNodo.Fallo;
        }
    }
    
    /// <summary>
    /// Verifica si hay aliados caidos.
    /// </summary>
    public class CondicionAliadosCaidos : NodoIA
    {
        private int cantidadMinima;
        
        public CondicionAliadosCaidos(int cantidadMinima = 1) => this.cantidadMinima = cantidadMinima;
        
        public override EstadoNodo Evaluar()
        {
            int caidos = 0;
            foreach (var aliado in aliados)
            {
                if (!aliado.EstaVivo()) caidos++;
            }
            return caidos >= cantidadMinima ? EstadoNodo.Exito : EstadoNodo.Fallo;
        }
    }
    
    /// <summary>
    /// Ejecuta con probabilidad aleatoria.
    /// </summary>
    public class CondicionProbabilidad : NodoIA
    {
        private float probabilidad;
        
        public CondicionProbabilidad(float probabilidad = 0.5f) => this.probabilidad = probabilidad;
        
        public override EstadoNodo Evaluar()
        {
            return Random.value <= probabilidad ? EstadoNodo.Exito : EstadoNodo.Fallo;
        }
    }
    
    // =================================================================
    // =================== NODOS DE ACCION =============================
    // =================================================================
    
    /// <summary>
    /// Resultado de una decision de IA.
    /// </summary>
    public class ResultadoIA
    {
        public IEntidadCombate Objetivo { get; set; }
        public HabilidadData Habilidad { get; set; }
        public TipoAccionIA TipoAccion { get; set; }
        
        public enum TipoAccionIA { Atacar, Defender, Huir, Curar, Especial }
    }
    
    /// <summary>
    /// Contexto compartido para nodos de IA.
    /// </summary>
    public static class ContextoIA
    {
        public static ResultadoIA UltimoResultado { get; set; }
    }
    
    /// <summary>
    /// Selecciona al jugador con menos vida.
    /// </summary>
    public class AccionAtacarDebil : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            IEntidadCombate objetivo = null;
            int menorVida = int.MaxValue;
            
            foreach (var jugador in jugadores)
            {
                if (!jugador.EstaVivo()) continue;
                
                if (jugador.VidaActual_Entidad < menorVida)
                {
                    menorVida = jugador.VidaActual_Entidad;
                    objetivo = jugador;
                }
            }
            
            if (objetivo == null) return EstadoNodo.Fallo;
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = objetivo,
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Selecciona al jugador con mas vida (tank).
    /// </summary>
    public class AccionAtacarTank : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            IEntidadCombate objetivo = null;
            int mayorVida = 0;
            
            foreach (var jugador in jugadores)
            {
                if (!jugador.EstaVivo()) continue;
                
                if (jugador.VidaActual_Entidad > mayorVida)
                {
                    mayorVida = jugador.VidaActual_Entidad;
                    objetivo = jugador;
                }
            }
            
            if (objetivo == null) return EstadoNodo.Fallo;
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = objetivo,
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Selecciona un jugador aleatorio.
    /// </summary>
    public class AccionAtacarAleatorio : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            var vivos = new List<IEntidadCombate>();
            foreach (var j in jugadores)
            {
                if (j.EstaVivo()) vivos.Add(j);
            }
            
            if (vivos.Count == 0) return EstadoNodo.Fallo;
            
            int indice = Random.Range(0, vivos.Count);
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = vivos[indice],
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Intenta curarse si tiene vida baja.
    /// </summary>
    public class AccionCurarse : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = null, // Se curara a si mismo
                TipoAccion = ResultadoIA.TipoAccionIA.Curar
            };
            
            return EstadoNodo.Exito;
        }
    }
    
    // =================================================================
    // =================== CEREBRO DE IA ===============================
    // =================================================================
    
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
