using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Constantes centralizadas del sistema Menu Radial
    /// VERSIÓN 2.0: Consolidación de todos los números mágicos y strings hardcodeados
    /// </summary>
    public static class MRConstants
    {
        #region Rutas de Salida

        /// <summary>
        /// Ruta unificada donde se guardan todas las animaciones generadas
        /// </summary>
        public const string ANIMATION_OUTPUT_PATH = "Assets/Bender_Dios/Generated/";

        /// <summary>
        /// Ruta donde se guardan los archivos VRChat (FX, Parameters, Menu)
        /// </summary>
        public const string VRCHAT_OUTPUT_PATH = "Assets/Bender_Dios/Generated/";

        /// <summary>
        /// Ruta de iconos del sistema
        /// </summary>
        public const string ICONS_PATH = "Assets/Bender_Dios/MenuRadial/Components/Menu/Resources/";

        /// <summary>
        /// Prefijo requerido para rutas de Unity
        /// </summary>
        public const string ASSETS_PREFIX = "Assets/";

        #endregion

        #region Nombres por Defecto

        /// <summary>
        /// Nombre por defecto para animaciones de iluminación
        /// </summary>
        public const string DEFAULT_ILLUMINATION_NAME = "RadialIllumination";

        /// <summary>
        /// Nombre por defecto para animaciones de menú radial
        /// </summary>
        public const string DEFAULT_RADIAL_MENU_NAME = "RadialAnimation";

        #endregion
    }

    /// <summary>
    /// Constantes de animación del sistema
    /// Especificación: 255 frames a 60 FPS = 4.25 segundos de duración
    /// </summary>
    public static class MRAnimationConstants
    {
        /// <summary>
        /// Número total de frames en una animación lineal
        /// </summary>
        public const int TOTAL_FRAMES = 255;

        /// <summary>
        /// Frame rate de las animaciones (frames por segundo)
        /// </summary>
        public const int FRAME_RATE = 60;

        /// <summary>
        /// Frame rate como double para cálculos precisos
        /// </summary>
        public const double FRAME_RATE_DOUBLE = 60.0;

        /// <summary>
        /// Frame intermedio para interpolación (mitad de 255)
        /// </summary>
        public const int MIDDLE_FRAME = 127;

        /// <summary>
        /// Duración de cada frame en segundos (1/60)
        /// </summary>
        public const float FRAME_DURATION = 0.0166667f;

        /// <summary>
        /// Duración total de la animación en segundos (255/60)
        /// </summary>
        public const float TOTAL_DURATION = 4.25f;

        /// <summary>
        /// Convierte frame a tiempo en segundos con clamp a TOTAL_FRAMES
        /// </summary>
        public static float FrameToSeconds(int frame)
        {
            return Mathf.Min(frame, TOTAL_FRAMES) / (float)FRAME_RATE;
        }

        /// <summary>
        /// Convierte tiempo en segundos a frame
        /// </summary>
        public static int SecondsToFrame(float seconds)
        {
            return Mathf.Clamp(Mathf.RoundToInt(seconds * FRAME_RATE), 0, TOTAL_FRAMES);
        }
    }

    /// <summary>
    /// Constantes del menú radial
    /// </summary>
    public static class MRMenuConstants
    {
        /// <summary>
        /// Número máximo de slots permitidos en un menú (limitación VRChat: 8 controles por menú)
        /// </summary>
        public const int MAX_SLOTS = 8;

        /// <summary>
        /// Profundidad máxima de submenús anidados
        /// </summary>
        public const int MAX_SUBMENU_DEPTH = 4;

        /// <summary>
        /// Radio disponible para el menú radial (normalizado 0-1)
        /// </summary>
        public const float AVAILABLE_RADIUS = 0.5f;
    }

    /// <summary>
    /// Constantes de iluminación lilToon
    /// Define los valores para los 3 keyframes: Frame 0 (Normal), Frame 127 (Intermedio), Frame 255 (Unlit)
    /// </summary>
    public static class MRIlluminationConstants
    {
        #region Valores por Defecto

        /// <summary>
        /// Valor por defecto de iluminación (50% = frame 127)
        /// </summary>
        public const float DEFAULT_VALUE = 0.5f;

        /// <summary>
        /// Valor por defecto de inicialización para iluminación en VRChat
        /// </summary>
        public const float VRCHAT_DEFAULT_VALUE = 0.5f;

        #endregion

        #region Frame 0 (Normal/Lit)

        /// <summary>
        /// AsUnlit en frame 0 (totalmente lit)
        /// </summary>
        public const float FRAME0_AS_UNLIT = 0f;

        /// <summary>
        /// LightMaxLimit en frame 0
        /// </summary>
        public const float FRAME0_LIGHT_MAX_LIMIT = 0.15f;

        /// <summary>
        /// ShadowBorder en frame 0
        /// </summary>
        public const float FRAME0_SHADOW_BORDER = 1f;

        /// <summary>
        /// ShadowStrength en frame 0
        /// </summary>
        public const float FRAME0_SHADOW_STRENGTH = 1f;

        #endregion

        #region Frame 127 (Intermedio)

        /// <summary>
        /// AsUnlit en frame 127
        /// </summary>
        public const float FRAME127_AS_UNLIT = 0f;

        /// <summary>
        /// LightMaxLimit en frame 127
        /// </summary>
        public const float FRAME127_LIGHT_MAX_LIMIT = 1f;

        /// <summary>
        /// ShadowBorder en frame 127
        /// </summary>
        public const float FRAME127_SHADOW_BORDER = 0.05f;

        /// <summary>
        /// ShadowStrength en frame 127
        /// </summary>
        public const float FRAME127_SHADOW_STRENGTH = 0.5f;

        #endregion

        #region Frame 255 (Unlit)

        /// <summary>
        /// AsUnlit en frame 255 (totalmente unlit)
        /// </summary>
        public const float FRAME255_AS_UNLIT = 1f;

        /// <summary>
        /// LightMaxLimit en frame 255
        /// </summary>
        public const float FRAME255_LIGHT_MAX_LIMIT = 1f;

        /// <summary>
        /// ShadowBorder en frame 255
        /// </summary>
        public const float FRAME255_SHADOW_BORDER = 0.05f;

        /// <summary>
        /// ShadowStrength en frame 255
        /// </summary>
        public const float FRAME255_SHADOW_STRENGTH = 0f;

        #endregion
    }

    /// <summary>
    /// Nombres de propiedades de shader lilToon
    /// </summary>
    public static class MRShaderProperties
    {
        /// <summary>
        /// Propiedad AsUnlit del shader lilToon
        /// </summary>
        public const string AS_UNLIT = "_AsUnlit";

        /// <summary>
        /// Propiedad LightMaxLimit del shader lilToon
        /// </summary>
        public const string LIGHT_MAX_LIMIT = "_LightMaxLimit";

        /// <summary>
        /// Propiedad ShadowBorder del shader lilToon
        /// </summary>
        public const string SHADOW_BORDER = "_ShadowBorder";

        /// <summary>
        /// Propiedad ShadowStrength del shader lilToon
        /// </summary>
        public const string SHADOW_STRENGTH = "_ShadowStrength";
    }

    /// <summary>
    /// Nombres de propiedades de shader Poiyomi
    /// Nota: _MinBrightness y _Grayscale_Lighting requieren estar marcados como "Animated (when locked)" en Poiyomi
    /// </summary>
    public static class MRPoiyomiShaderProperties
    {
        /// <summary>
        /// Multiplicador de iluminación PP (controla intensidad de luz)
        /// Rango: 0.6 (iluminado) a 1.8 (unlit)
        /// </summary>
        public const string PP_LIGHTING_MULTIPLIER = "_PPLightingMultiplier";

        /// <summary>
        /// Brillo mínimo (requiere "Animated when locked")
        /// Rango: 0.0 (iluminado) a 0.03 (unlit)
        /// </summary>
        public const string MIN_BRIGHTNESS = "_MinBrightness";

        /// <summary>
        /// Iluminación en escala de grises (requiere "Animated when locked")
        /// Rango: 0.5 (iluminado) a 0.0 (unlit)
        /// </summary>
        public const string GRAYSCALE_LIGHTING = "_Grayscale_Lighting";
    }

    /// <summary>
    /// Constantes de iluminación Poiyomi
    /// Define los valores para los 3 keyframes: Frame 0 (Iluminado), Frame 127 (Intermedio), Frame 255 (Unlit)
    /// </summary>
    public static class MRPoiyomiIlluminationConstants
    {
        #region Frame 0 (Iluminado)

        /// <summary>
        /// PPLightingMultiplier en frame 0
        /// </summary>
        public const float FRAME0_PP_LIGHTING_MULTIPLIER = 0.6f;

        /// <summary>
        /// MinBrightness en frame 0
        /// </summary>
        public const float FRAME0_MIN_BRIGHTNESS = 0f;

        /// <summary>
        /// GrayscaleLighting en frame 0
        /// </summary>
        public const float FRAME0_GRAYSCALE_LIGHTING = 0.5f;

        #endregion

        #region Frame 127 (Intermedio)

        /// <summary>
        /// PPLightingMultiplier en frame 127 (interpolado)
        /// </summary>
        public const float FRAME127_PP_LIGHTING_MULTIPLIER = 1.2f;

        /// <summary>
        /// MinBrightness en frame 127 (interpolado)
        /// </summary>
        public const float FRAME127_MIN_BRIGHTNESS = 0.015f;

        /// <summary>
        /// GrayscaleLighting en frame 127 (interpolado)
        /// </summary>
        public const float FRAME127_GRAYSCALE_LIGHTING = 0.25f;

        #endregion

        #region Frame 255 (Unlit)

        /// <summary>
        /// PPLightingMultiplier en frame 255
        /// </summary>
        public const float FRAME255_PP_LIGHTING_MULTIPLIER = 1.8f;

        /// <summary>
        /// MinBrightness en frame 255
        /// </summary>
        public const float FRAME255_MIN_BRIGHTNESS = 0.03f;

        /// <summary>
        /// GrayscaleLighting en frame 255
        /// </summary>
        public const float FRAME255_GRAYSCALE_LIGHTING = 0f;

        #endregion
    }

    /// <summary>
    /// Sufijos para archivos de animación generados
    /// </summary>
    public static class MRAnimationSuffixes
    {
        /// <summary>
        /// Sufijo para animación ON (toggle)
        /// </summary>
        public const string ON = "_on";

        /// <summary>
        /// Sufijo para animación OFF (toggle)
        /// </summary>
        public const string OFF = "_off";

        /// <summary>
        /// Sufijo para animación A (alternancia)
        /// </summary>
        public const string A = "_A";

        /// <summary>
        /// Sufijo para animación B (alternancia)
        /// </summary>
        public const string B = "_B";

        /// <summary>
        /// Sufijo para animación lineal
        /// </summary>
        public const string LINEAR = "_lin";

        /// <summary>
        /// Variantes de sufijo ON para búsqueda de archivos
        /// </summary>
        public static readonly string[] ON_VARIANTS = { "_on", "_On", "_ON" };

        /// <summary>
        /// Variantes de sufijo OFF para búsqueda de archivos
        /// </summary>
        public static readonly string[] OFF_VARIANTS = { "_off", "_Off", "_OFF" };

        /// <summary>
        /// Variantes de sufijo A para búsqueda de archivos
        /// </summary>
        public static readonly string[] A_VARIANTS = { "_A", "_a" };

        /// <summary>
        /// Variantes de sufijo B para búsqueda de archivos
        /// </summary>
        public static readonly string[] B_VARIANTS = { "_B", "_b" };

        /// <summary>
        /// Variantes de sufijo LINEAR para búsqueda de archivos
        /// </summary>
        public static readonly string[] LINEAR_VARIANTS = { "", "_lin", "_lineal", "_linear", "_Lin", "_Lineal", "_Linear" };
    }

    /// <summary>
    /// Extensiones de archivos
    /// </summary>
    public static class MRFileExtensions
    {
        /// <summary>
        /// Extensión de archivo de animación
        /// </summary>
        public const string ANIMATION = ".anim";

        /// <summary>
        /// Extensión de archivo de controlador
        /// </summary>
        public const string CONTROLLER = ".controller";

        /// <summary>
        /// Extensión de archivo de asset
        /// </summary>
        public const string ASSET = ".asset";
    }

    /// <summary>
    /// Colores de UI del sistema
    /// </summary>
    public static class MRUIColors
    {
        /// <summary>
        /// Color de fondo para elementos de preview
        /// </summary>
        public static readonly Color PREVIEW_BACKGROUND = new Color(0.15f, 0.25f, 0.25f, 0.8f);

        /// <summary>
        /// Color de fondo para elementos inactivos
        /// </summary>
        public static readonly Color INACTIVE_BACKGROUND = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        /// <summary>
        /// Color de advertencia/error
        /// </summary>
        public static readonly Color WARNING = new Color(1f, 0.5f, 0.5f, 1f);
    }

    /// <summary>
    /// Constantes para ajuste de bounds de meshes
    /// </summary>
    public static class MRBoundsConstants
    {
        /// <summary>
        /// Porcentaje de margen por defecto (10%)
        /// </summary>
        public const float DEFAULT_MARGIN_PERCENTAGE = 0.10f;

        /// <summary>
        /// Porcentaje de margen minimo
        /// </summary>
        public const float MIN_MARGIN_PERCENTAGE = 0f;

        /// <summary>
        /// Porcentaje de margen maximo (50%)
        /// </summary>
        public const float MAX_MARGIN_PERCENTAGE = 0.5f;

        /// <summary>
        /// Nombre por defecto para componente de bounds
        /// </summary>
        public const string DEFAULT_COMPONENT_NAME = "AjustarBounds";
    }

    /// <summary>
    /// Constantes de VRChat SDK3
    /// </summary>
    public static class MRVRChatConstants
    {
        /// <summary>
        /// Número máximo de bits para parámetros sincronizados
        /// </summary>
        public const int MAX_PARAMETER_BITS = 256;

        /// <summary>
        /// Bits usados por un parámetro Bool
        /// </summary>
        public const int BOOL_BITS = 1;

        /// <summary>
        /// Bits usados por un parámetro Float
        /// </summary>
        public const int FLOAT_BITS = 8;

        /// <summary>
        /// Bits usados por un parámetro Int
        /// </summary>
        public const int INT_BITS = 8;

        /// <summary>
        /// Nombre del layer base en AnimatorController
        /// </summary>
        public const string BASE_LAYER_NAME = "Base Layer";

        /// <summary>
        /// Nombre del estado idle
        /// </summary>
        public const string IDLE_STATE_NAME = "Idle";
    }
}
