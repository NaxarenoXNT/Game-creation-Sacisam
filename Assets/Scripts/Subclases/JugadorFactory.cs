using Padres;

namespace Subclases
{
    /// <summary>
    /// Factory responsable de crear la instancia correcta de Jugador
    /// a partir de un ClaseData. Agrega un nuevo case por cada clase nueva.
    /// </summary>
    public static class JugadorFactory
    {
        public static Jugador Crear(ClaseData datos)
        {
            string nombre = datos.nombreClase?.Trim() ?? "";
            return nombre switch
            {
                "Guerrero" => new Guerrero(datos),
                "Mago"     => new Mago(datos),
                "Arquero"  => new Arquero(datos),
                _          => throw new System.ArgumentException(
                    $"Clase '{datos.nombreClase}' no registrada en JugadorFactory. " +
                    "Agrega un nuevo case en JugadorFactory.Crear().")
            };
        }
    }
}
