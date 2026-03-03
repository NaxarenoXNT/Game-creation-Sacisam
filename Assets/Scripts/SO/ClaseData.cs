using UnityEngine;
using Padres;
using Subclases;
using Subclases.Modulos;
using Flags;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "Nueva Clase", menuName = "Combate/Clase Jugador")]
  public class ClaseData : ScriptableObject
  {
      [Header("Info General")]
      public string nombreClase;
      public Sprite iconoClase;
      [TextArea(2, 3)]
      public string descripcionClase;
      
      [Header("Stats Base")]
      public int vidaBase = 100;
      public int ataqueBase = 10;
      public float defensaBase = 5f;
      public int manaBase = 50;
      public int velocidadBase = 50;
      
      [Header("Escalado por Nivel")]
      [Tooltip("Crecimiento de stats por nivel. Si no se configura, usa valores por defecto.")]
      public EscaladoJugador escalado = new EscaladoJugador();
      
      [Header("Atributos")]
      public ElementAttribute atributos;
      public TipoEntidades tipoEntidad;
      public CombatStyle estiloCombate;
      
      [Header("Visual y Animación")]
      public AnimatorOverrideController animatorOverride;
      public GameObject prefabProyectil;
      
      [Header("Habilidades Iniciales")]
      [Tooltip("Habilidades activas con las que empieza la clase")]
      public List<HabilidadData> habilidadesIniciales = new List<HabilidadData>();
      
      [Tooltip("Habilidades pasivas con las que empieza la clase")]
      public List<PasivaData> pasivasIniciales = new List<PasivaData>();
      
      [Header("Límites")]
      [Tooltip("Máximo de habilidades activas equipadas (0 = sin límite)")]
      public int limiteHabilidadesActivas = 8;
      
      [Tooltip("Máximo de habilidades pasivas equipadas (0 = sin límite)")]
      public int limitePasivas = 4;

      [Header("Módulos de Evolución Iniciales")]
      [Tooltip("Módulos de comportamiento activos desde el inicio. Útil para NPCs que arrancan ya evolucionados.")]
      public List<ModuloClaseSO> modulosIniciales = new List<ModuloClaseSO>();
      
      public Jugador CrearInstancia()
      {
          Jugador instancia = nombreClase switch
          {
              "Guerrero" => new Guerrero(this),
              "Mago"     => new Mago(this),
              "Arquero"  => new Arquero(this),
              _          => throw new System.Exception($"Clase {nombreClase} no implementada")
          };

          // Aplicar módulos iniciales (para NPCs o personajes que arrancan ya evolucionados)
          if (modulosIniciales != null)
              foreach (var so in modulosIniciales)
                  if (so != null) instancia.AgregarModulo(so.Instanciar());

          return instancia;
      }
  }