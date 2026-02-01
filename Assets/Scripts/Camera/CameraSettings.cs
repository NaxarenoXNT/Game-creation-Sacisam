using UnityEngine;

namespace Camera
{
    /// <summary>
    /// ScriptableObject con la configuración de la cámara isométrica.
    /// Crear: Assets/Create/Saclisam/Camera Settings
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Saclisam/Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("Ángulo Isométrico")]
        [Tooltip("Ángulo de inclinación de la cámara (típico isométrico: 30-45)")]
        [Range(20f, 60f)]
        public float pitchAngle = 45f;
        
        [Tooltip("Rotación inicial en Y")]
        public float initialYawAngle = 45f;
        
        [Header("Zoom")]
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
        
        [Header("Rotación")]
        [Tooltip("Permitir rotación de cámara con Q/E o click derecho")]
        public bool allowRotation = true;
        
        [Tooltip("Velocidad de rotación")]
        public float rotationSpeed = 90f;
        
        [Tooltip("Rotación con mouse (click derecho + arrastrar)")]
        public bool mouseRotation = true;
        
        [Tooltip("Sensibilidad de rotación con mouse")]
        public float mouseRotationSensitivity = 2f;
        
        [Header("Seguimiento")]
        [Tooltip("Suavizado del seguimiento")]
        public float followSmoothing = 8f;
        
        [Tooltip("Offset vertical del punto de seguimiento")]
        public float targetHeightOffset = 1.5f;
        
        [Header("Límites")]
        [Tooltip("Limitar área de la cámara (útil para mapas)")]
        public bool useBounds = false;
        
        public Vector2 boundsMin = new Vector2(-50f, -50f);
        public Vector2 boundsMax = new Vector2(50f, 50f);
    }
}
