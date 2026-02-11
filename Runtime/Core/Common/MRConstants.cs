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
        /// Duración de cada frame en segundos (1/60)
        /// </summary>
        public const float FRAME_DURATION = 0.0166667f;

        /// <summary>
        /// Duración total de la animación en segundos (255/60)
        /// </summary>
        public const float TOTAL_DURATION = 4.25f;

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

    }

    /// <summary>
    /// Constantes de iluminación lilToon
    /// Define los valores para los 3 keyframes: Frame 0 (Normal), Frame 127 (Intermedio), Frame 255 (Unlit)
    /// </summary>
    public static class MRIlluminationConstants
    {
        #region Valores por Defecto

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
    /// Sufijos para archivos de animación generados
    /// </summary>
    public static class MRAnimationSuffixes
    {
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

    }

}
