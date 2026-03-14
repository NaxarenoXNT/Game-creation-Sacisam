using System.Collections.Generic;
using Interfaces;
using Padres;

namespace IA.Roles
{
    /// <summary>
    /// Rol Sanador — mantiene con vida a los aliados por encima de todo.
    /// Prioriza curar aliados heridos, luego curarse a sí mismo, luego buffear.
    /// Solo ataca como último recurso.
    /// Válido para goblins sanadores, clérigos, chamanes curanderos, etc.
    /// Para variantes con lógica diferente, heredar de este y sobreescribir Decidir().
    /// </summary>
    public class RolSanador : IDecisionRol
    {
        public virtual ResultadoIA Decidir(Enemigos yo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            var arbol = new Selector(
                // Prioridad 1: curar al aliado más herido
                new Secuencia(
                    new CondicionAliadoHerido(0.60f),
                    new AccionCurarAliado()
                ),
                // Prioridad 2: curarse a sí mismo si vida baja
                new Secuencia(
                    new CondicionVidaBaja(0.40f),
                    new AccionCurarse()
                ),
                // Prioridad 3: buffear un aliado con algo de aleatoriedad
                new Secuencia(
                    new CondicionProbabilidad(0.50f),
                    new AccionBuffearAliado()
                ),
                // Fallback: atacar al más débil disponible
                new AccionAtacarDebil(),
                new AccionAtacarAleatorio()
            );

            arbol.Configurar(yo, jugadores, aliados);
            arbol.Evaluar();
            return ContextoIA.UltimoResultado;
        }
    }
}
