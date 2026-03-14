using System.Collections.Generic;
using Interfaces;
using Padres;
using Flags;
using IA.Roles;

namespace IA
{
    /// <summary>
    /// Cerebro de IA: coordina la toma de decisiones de un enemigo por turno.
    /// No contiene lógica de combate propia; delega completamente al IDecisionRol asignado.
    ///
    /// Flujo:
    ///   EnemigoData.arquetipoIA  →  CerebroIA.CrearParaArquetipo()
    ///                            →  Registra el IDecisionRol correcto
    ///                            →  Decidir() llama al rol cada turno
    ///
    /// Para agregar un nuevo rol:
    ///   1. Crear Assets/Scripts/IA/Roles/RolNuevo.cs implementando IDecisionRol
    ///   2. Agregar el valor al enum ArquetipoIA en Flags/TipoRecurso.cs
    ///   3. Registrar la asignación en el bloque _registry de esta clase
    /// </summary>
    [System.Serializable]
    public class CerebroIA
    {
        // El rol activo que toma las decisiones de combate
        private IDecisionRol rolActivo;

        // ----------------------------------------------------------------
        //  REGISTRY: enum ArquetipoIA → IDecisionRol concreto
        //  Agregar una línea acá para registrar un nuevo rol.
        // ----------------------------------------------------------------
        private static readonly Dictionary<ArquetipoIA, System.Func<IDecisionRol>> _registry =
            new Dictionary<ArquetipoIA, System.Func<IDecisionRol>>
        {
            { ArquetipoIA.Basico,      () => new RolBasico()      },
            { ArquetipoIA.Guerrero,    () => new RolGuerrero()    },
            { ArquetipoIA.Mago,        () => new RolMago()        },
            { ArquetipoIA.Sanador,     () => new RolSanador()     },
            { ArquetipoIA.Berserk,     () => new RolBerserk()     },
            { ArquetipoIA.Tanque,      () => new RolTanque()      },
            { ArquetipoIA.Controlador, () => new RolControlador() },
            { ArquetipoIA.Soporte,     () => new RolSoporte()     },
        };

        // ----------------------------------------------------------------
        //  CONSTRUCTOR PRIVADO — usar CrearParaArquetipo()
        // ----------------------------------------------------------------
        private CerebroIA(IDecisionRol rol)
        {
            rolActivo = rol;
        }

        // ----------------------------------------------------------------
        //  FACTORY — punto de entrada único
        // ----------------------------------------------------------------

        /// <summary>
        /// Crea el CerebroIA con el rol correspondiente al arquetipo.
        /// Si el arquetipo no está registrado, usa RolBasico como fallback.
        /// </summary>
        public static CerebroIA CrearParaArquetipo(ArquetipoIA arquetipo)
        {
            if (_registry.TryGetValue(arquetipo, out var factory))
                return new CerebroIA(factory());

            UnityEngine.Debug.LogWarning(
                $"[CerebroIA] Arquetipo '{arquetipo}' no registrado. Usando RolBasico como fallback.");
            return new CerebroIA(new RolBasico());
        }

        // ----------------------------------------------------------------
        //  DECIDIR — llamado por Enemigos.ObtenerAccionElegida() cada turno
        // ----------------------------------------------------------------

        /// <summary>
        /// Evalúa el árbol del rol activo y retorna la decisión (objetivo + habilidad).
        /// Retorna null si el rol no encontró acción válida.
        /// </summary>
        public ResultadoIA Decidir(List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            return rolActivo?.Decidir(enemigo, jugadores, aliados);
        }

        // Referencia al enemigo dueño de este cerebro (seteada por Configurar)
        private Enemigos enemigo;

        /// <summary>
        /// Vincula el cerebro con el enemigo que lo posee.
        /// Llamar una sola vez al inicializar desde EnemigoData.
        /// </summary>
        public void Configurar(Enemigos enemigo)
        {
            this.enemigo = enemigo;
        }

        // ----------------------------------------------------------------
        //  HELPERS LEGACY — mantenidos para compatibilidad
        // ----------------------------------------------------------------
        public static CerebroIA CrearBasico()   => CrearParaArquetipo(ArquetipoIA.Basico);
        public static CerebroIA CrearAgresivo() => CrearParaArquetipo(ArquetipoIA.Guerrero);
        public static CerebroIA CrearDefensivo() => CrearParaArquetipo(ArquetipoIA.Tanque);
    }
}
