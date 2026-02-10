using System;
using Bender_Dios.MenuRadial.AnimationSystem.Interfaces;
using Bender_Dios.MenuRadial.AnimationSystem.Services;

namespace Bender_Dios.MenuRadial.Core.Services
{
    /// <summary>
    /// Localizador de servicios del sistema MenuRadial.
    /// Provee instancias lazy singleton de los 2 servicios de iluminación.
    /// </summary>
    public static class MenuRadialServiceBootstrap
    {
        private static IlluminationAnimationGenerator _animationGenerator;
        private static IlluminationMaterialScanner _materialScanner;

        public static T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(IIlluminationAnimationGenerator))
                return (_animationGenerator ??= new IlluminationAnimationGenerator()) as T;

            if (typeof(T) == typeof(IIlluminationMaterialScanner))
                return (_materialScanner ??= new IlluminationMaterialScanner()) as T;

            throw new InvalidOperationException($"Servicio {typeof(T).Name} no registrado");
        }
    }
}
