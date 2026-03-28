using System.Collections;

namespace World.Travel
{
    /// <summary>
    /// Contrato para cualquier sistema que gestione fundidos de pantalla (fade in/out).
    /// Desacopla el TravelManager de una implementación concreta de UI.
    ///
    /// Implementa esta interfaz en un ScreenFadeController (UIDocument, CanvasGroup, etc.)
    /// e inyéctalo en TravelManager al iniciar la escena.
    /// </summary>
    public interface IFadeController
    {
        /// <summary>Funde la pantalla a negro. Devuelve una coroutine para encadenar con yield.</summary>
        IEnumerator FadeOut(float duration);

        /// <summary>Funde la pantalla desde negro a transparente. Devuelve una coroutine para encadenar con yield.</summary>
        IEnumerator FadeIn(float duration);

        /// <summary>Aplica el fade out instantáneamente (sin animación).</summary>
        void FadeOutImmediate();

        /// <summary>Aplica el fade in instantáneamente (sin animación).</summary>
        void FadeInImmediate();
    }
}
