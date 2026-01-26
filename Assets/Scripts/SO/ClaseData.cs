using UnityEngine;
using Padres;
using Subclases;
using Flags;



[CreateAssetMenu(fileName = "Nueva Clase", menuName = "Combate/Clase Jugador")]
  public class ClaseData : ScriptableObject
  {
      [Header("Info General")]
      public string nombreClase;
      public Sprite iconoClase;
      
      [Header("Stats Base")]
      public int vidaBase = 100;
      public int ataqueBase = 10;
      public float defensaBase = 5f;
      public int manaBase = 50;
      public int velocidadBase = 50;
      
      [Header("Atributos")]
      public ElementAttribute atributos;
      public TipoEntidades tipoEntidad;
      public CombatStyle estiloCombate;
      
      [Header("Visual y Animación")]
      public AnimatorOverrideController animatorOverride;
      public GameObject prefabProyectil;
      
      //[Header("Habilidades")]
      //public HabilidadData[] habilidadesIniciales;
      
      public Jugador CrearInstancia()
      {
          return nombreClase switch
          {
              "Guerrero" => new Guerrero(this),
              //"Mago" => new Mago(this),
              //"Arquero" => new Arquero(this),
              _ => throw new System.Exception($"Clase {nombreClase} no implementada")
          };
      }
  }