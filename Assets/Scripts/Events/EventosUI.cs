using UnityEngine;

namespace Managers
{
    // =================================================================
    // =================== EVENTOS DE UI ===============================
    // =================================================================
    
    public struct EventoMostrarMensaje : IEvento
    {
        public string Mensaje;
        public float Duracion;
        public Color? ColorTexto;
    }
    
    public struct EventoActualizarUI : IEvento
    {
        public string PanelId;
    }
}
