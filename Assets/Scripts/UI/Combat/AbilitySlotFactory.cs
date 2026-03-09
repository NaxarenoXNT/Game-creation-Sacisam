using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Habilidades;
using Flags;

namespace UI.Combat
{
    /// <summary>
    /// Factory estática que genera VisualElements ricos para habilidades.
    /// Lee los datos de un <see cref="HabilidadData"/> SO y construye un slot
    /// de UI Toolkit con nombre, categoría, tipo de objetivo, costo y estado.
    ///
    /// Reutilizable desde cualquier panel (combate, inventario, stats, tooltips).
    /// </summary>
    public static class AbilitySlotFactory
    {
        /// <summary>
        /// Crea un VisualElement completo para una habilidad.
        /// </summary>
        /// <param name="habilidad">Datos de la habilidad (ScriptableObject).</param>
        /// <param name="disponible">Si la habilidad está disponible (no en cooldown, tiene recursos).</param>
        /// <param name="onClick">Callback al hacer click. Null = botón no interactivo.</param>
        /// <returns>VisualElement listo para agregar a un contenedor.</returns>
        public static VisualElement Crear(HabilidadData habilidad, bool disponible, System.Action<HabilidadData> onClick = null, int cooldownRestante = 0)
        {
            // ── Contenedor principal (botón clickeable) ──
            var slot = new Button();
            slot.AddToClassList("ability-slot");

            if (!disponible)
                slot.AddToClassList("ability-slot--disabled");

            slot.SetEnabled(disponible);

            if (onClick != null)
            {
                HabilidadData captura = habilidad;
                slot.RegisterCallback<ClickEvent>(_ => onClick(captura));
            }

            // ── Fila superior: [icono] nombre + categoría ──
            var headerRow = new VisualElement();
            headerRow.AddToClassList("ability-slot__header");

            if (habilidad.icono != null)
            {
                var icon = new Image();
                icon.sprite = habilidad.icono;
                icon.AddToClassList("ability-slot__icon");
                headerRow.Add(icon);
            }

            var lblNombre = new Label(habilidad.nombreHabilidad);
            lblNombre.AddToClassList("ability-slot__name");

            var lblCategoria = new Label(DescribirCategoria(habilidad.categoria));
            lblCategoria.AddToClassList("ability-slot__category");
            lblCategoria.AddToClassList(ClaseCategoria(habilidad.categoria));

            headerRow.Add(lblNombre);
            headerRow.Add(lblCategoria);
            slot.Add(headerRow);

            // ── Fila inferior: objetivo + costo + efecto ──
            var detailRow = new VisualElement();
            detailRow.AddToClassList("ability-slot__details");

            var lblObjetivo = new Label(DescribirObjetivo(habilidad.tipoObjetivo));
            lblObjetivo.AddToClassList("ability-slot__target");

            var lblCosto = new Label(habilidad.ObtenerDescripcionCostos());
            lblCosto.AddToClassList("ability-slot__cost");

            var lblEfecto = new Label(DescribirEfectos(habilidad));
            lblEfecto.AddToClassList("ability-slot__effect");

            detailRow.Add(lblObjetivo);
            detailRow.Add(lblCosto);
            detailRow.Add(lblEfecto);

            if (!disponible && cooldownRestante > 0)
            {
                var lblCd = new Label($"CD: {cooldownRestante}");
                lblCd.AddToClassList("ability-slot__cooldown");
                detailRow.Add(lblCd);
            }

            slot.Add(detailRow);

            return slot;
        }

        // ─────────────────────────────────────────────────────────────
        #region Helpers de descripción (portados de AbilityButtonUI legacy)

        public static string DescribirCategoria(CategoriaHabilidad categoria)
        {
            return categoria switch
            {
                CategoriaHabilidad.Ataque   => "ATK",
                CategoriaHabilidad.Curacion => "HEAL",
                CategoriaHabilidad.Buff     => "BUFF",
                CategoriaHabilidad.Debuff   => "DEBUFF",
                CategoriaHabilidad.Control  => "CC",
                CategoriaHabilidad.Utilidad => "UTIL",
                _ => "-"
            };
        }

        public static string DescribirObjetivo(TargetType tipo)
        {
            return tipo switch
            {
                TargetType.EnemigoUnico => "1 Enemigo",
                TargetType.EnemigoTodos => "Todos Enem.",
                TargetType.AliadoUnico  => "1 Aliado",
                TargetType.AliadoTodos  => "Todos Aliad.",
                TargetType.Self         => "Self",
                _ => "?"
            };
        }

        public static string DescribirEfectos(HabilidadData habilidad)
        {
            if (habilidad.efectos == null || habilidad.efectos.Count == 0)
                return "";

            var primerEfecto = habilidad.efectos[0];
            if (primerEfecto == null) return "";

            string nombreTipo = primerEfecto.GetType().Name;
            if (nombreTipo.EndsWith("Effect"))
                nombreTipo = nombreTipo.Substring(0, nombreTipo.Length - 6);

            if (habilidad.efectos.Count > 1)
                return $"{nombreTipo} +{habilidad.efectos.Count - 1}";

            return nombreTipo;
        }

        /// <summary>
        /// Obtiene el elemento (ElementAttribute) principal de la habilidad
        /// basándose en el primer DamageEffect de sus efectos.
        /// </summary>
        public static ElementAttribute ObtenerElementoPrincipal(HabilidadData habilidad)
        {
            if (habilidad.efectos == null) return ElementAttribute.None;

            var damageEffect = habilidad.efectos.FirstOrDefault(e => e is DamageEffect) as DamageEffect;
            return damageEffect?.tipoDano ?? ElementAttribute.None;
        }

        /// <summary>
        /// Devuelve la clase USS de color para la categoría de habilidad.
        /// </summary>
        private static string ClaseCategoria(CategoriaHabilidad cat)
        {
            return cat switch
            {
                CategoriaHabilidad.Ataque   => "cat--atk",
                CategoriaHabilidad.Curacion => "cat--heal",
                CategoriaHabilidad.Buff     => "cat--buff",
                CategoriaHabilidad.Debuff   => "cat--debuff",
                CategoriaHabilidad.Control  => "cat--cc",
                CategoriaHabilidad.Utilidad => "cat--util",
                _ => ""
            };
        }

        #endregion
    }
}
