using Flasgs;
using Habilidades;
using System.Collections.Generic;

namespace Interfaces
{
    public interface IEntidadCombate
    {
        string Nombre_Entidad { get; }
        int Nivel_Entidad { get; }
        int Vida_Entidad { get; }
        int VidaActual_Entidad { get; }
        int PuntosDeAtaque_Entidad { get; }
        float PuntosDeDefensa_Entidad { get; }
        int Velocidad { get; }
        bool EsDerrotado { get; }
        bool EstaMuerto { get; }

        TipoEntidades TipoEntidad { get; }






        bool EstaVivo();
        bool PuedeActuar();



        bool EsTipoEntidad(TipoEntidades tipo);
        bool UsaEstiloDeCombate(CombatStyle estilo);
        void AplicarEstado(StatusFlag status, int duracion);
        void RecibirDaño(int dañoBruto, ElementAttribute tipo);


        // === Eventos para UI/Controllers ===
        //event Action<int, int> OnVidaCambiada;
        //event Action<int> OnDañoRecibido;
        //event Action OnMuerte;
    }
    public interface IEntidadActuable // Para unificar el acceso al Command
    {
        (IHabilidadesCommad comando, IEntidadCombate objetivo) ObtenerAccionElegida(
            List<IEntidadCombate> aliados, 
            List<IEntidadCombate> enemigos
        );
    }
}