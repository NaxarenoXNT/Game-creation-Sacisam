using UnityEngine;

namespace Camera
{
    public enum CameraMode
    {
        Isometric,
        ThirdPerson
    }

    /// <summary>
    /// ScriptableObject con la configuración del sistema de cámara dual (isométrica / tercera persona).
    /// Crear: Assets/Create/Saclisam/Camera Settings
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Saclisam/Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        // ──────────────────────────────────────────────────────────────
        // MODO POR DEFECTO
        // ──────────────────────────────────────────────────────────────
        [Header("Modo por Defecto")]
        [Tooltip("Modo de cámara al iniciar el juego (fuera de combate)")]
        public CameraMode defaultMode = CameraMode.ThirdPerson;

        [Tooltip("Tecla para alternar entre modos fuera de combate")]
        public KeyCode toggleModeKey = KeyCode.Tab;

        [Tooltip("Duración de la transición suave entre modos (segundos)")]
        [Range(0.1f, 1.5f)]
        public float modeTransitionDuration = 0.4f;

        // ──────────────────────────────────────────────────────────────
        // CÁMARA ISOMÉTRICA
        // ──────────────────────────────────────────────────────────────
        [Header("Isométrica – Ángulo")]
        [Tooltip("Ángulo de inclinación de la cámara (típico isométrico: 30-45)")]
        [Range(20f, 60f)]
        public float pitchAngle = 45f;

        [Tooltip("Rotación inicial en Y")]
        public float initialYawAngle = 45f;

        [Header("Isométrica – Zoom")]
        [Tooltip("Distancia mínima de la cámara al objetivo")]
        public float minZoomDistance = 5f;

        [Tooltip("Distancia máxima de la cámara al objetivo")]
        public float maxZoomDistance = 20f;

        [Tooltip("Distancia inicial")]
        public float defaultZoomDistance = 12f;

        [Tooltip("Velocidad del zoom")]
        public float zoomSpeed = 5f;

        [Tooltip("Suavizado del zoom")]
        public float zoomSmoothing = 10f;

        [Header("Isométrica – Rotación")]
        [Tooltip("Permitir rotación de cámara con Q/E o click derecho")]
        public bool allowRotation = true;

        [Tooltip("Velocidad de rotación con Q/E")]
        public float rotationSpeed = 90f;

        [Tooltip("Rotación con mouse (click derecho + arrastrar)")]
        public bool mouseRotation = true;

        [Tooltip("Sensibilidad de rotación con mouse")]
        public float mouseRotationSensitivity = 2f;

        [Header("Isométrica – Seguimiento")]
        [Tooltip("Suavizado del seguimiento")]
        public float followSmoothing = 8f;

        [Tooltip("Offset vertical del punto de seguimiento")]
        public float targetHeightOffset = 1.5f;

        [Header("Isométrica – Límites")]
        [Tooltip("Limitar área de la cámara (útil para mapas)")]
        public bool useBounds = false;

        public Vector2 boundsMin = new Vector2(-50f, -50f);
        public Vector2 boundsMax = new Vector2(50f, 50f);

        // ──────────────────────────────────────────────────────────────
        // CÁMARA TERCERA PERSONA
        // ──────────────────────────────────────────────────────────────
        [Header("Tercera Persona – Posición")]
        [Tooltip("Distancia horizontal detrás del personaje")]
        [Range(2f, 20f)]
        public float tpDistance = 6f;

        [Tooltip("Distancia mínima de zoom en tercera persona")]
        [Range(1f, 10f)]
        public float tpMinDistance = 2f;

        [Tooltip("Distancia máxima de zoom en tercera persona")]
        [Range(5f, 25f)]
        public float tpMaxDistance = 14f;

        [Tooltip("Altura sobre el personaje")]
        [Range(0.5f, 6f)]
        public float tpHeight = 2.5f;

        [Tooltip("Ángulo de depresión fijo de la cámara (grados)")]
        [Range(5f, 50f)]
        public float tpPitchAngle = 18f;

        [Tooltip("Offset vertical del punto de enfoque del personaje")]
        public float tpTargetHeightOffset = 1.5f;

        [Header("Tercera Persona – Rotación")]
        [Tooltip("Velocidad de rotación orbital con Q/E")]
        public float tpRotationSpeed = 90f;

        [Tooltip("Rotación orbital con mouse (movimiento libre, sin mantener click)")]
        public bool tpMouseRotation = true;

        [Tooltip("Sensibilidad de rotación con mouse")]
        public float tpMouseRotationSensitivity = 2.5f;

        [Tooltip("Suavizado de la rotación en tercera persona (independiente del zoom)")]
        public float tpRotationSmoothing = 15f;  // FIX: campo nuevo, separado de tpZoomSmoothing

        [Tooltip("Al iniciar tercera persona, alinear cámara detrás del personaje automáticamente")]
        public bool tpSnapBehindOnEnter = true;

        [Header("Tercera Persona – Seguimiento")]
        [Tooltip("Suavizado de posición")]
        public float tpFollowSmoothing = 10f;

        [Tooltip("Velocidad de zoom con scroll")]
        public float tpZoomSpeed = 4f;

        [Tooltip("Suavizado del zoom")]
        public float tpZoomSmoothing = 10f;
    }
}