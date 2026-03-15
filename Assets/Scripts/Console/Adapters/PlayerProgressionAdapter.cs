using UnityEngine;
using Console.Context;
using Managers;
using Padres;

namespace Console.Adapters
{
    /// <summary>
    /// Adapter que conecta IPlayerProgression con los sistemas reales:
    /// - PlayerPartyManager.Instance.MainCharacter (EntityController)
    /// - Entidad lógica interna (Jugador) para SubirNivel y Curar
    /// </summary>
    public class PlayerProgressionAdapter : IPlayerProgression
    {
        private EntityController MainCharacter =>
            PlayerPartyManager.Instance?.MainCharacter;

        public int CurrentLevel =>
            MainCharacter != null ? MainCharacter.Nivel_Entidad : 0;

        public int CurrentHealth =>
            MainCharacter != null ? MainCharacter.VidaActual_Entidad : 0;

        public int MaxHealth =>
            MainCharacter != null ? MainCharacter.Vida_Entidad : 0;

        public void LevelUp(int amount)
        {
            var controller = MainCharacter;
            if (controller == null)
            {
                Debug.LogWarning("[Console] No main character found for LevelUp.");
                return;
            }

            // Acceder a la Jugador lógica para llamar SubirNivel directamente
            var jugador = controller.EntidadLogica as Jugador;
            if (jugador == null)
            {
                Debug.LogWarning("[Console] Main character is not a Jugador.");
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                jugador.SubirNivel();
            }
        }

        public void HealToFull()
        {
            var controller = MainCharacter;
            if (controller == null)
            {
                Debug.LogWarning("[Console] No main character found for Heal.");
                return;
            }

            int missing = controller.Vida_Entidad - controller.VidaActual_Entidad;
            if (missing > 0)
            {
                controller.Curar(missing);
            }
        }
    }
}
