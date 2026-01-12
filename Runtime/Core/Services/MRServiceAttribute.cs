using System;

namespace Bender_Dios.MenuRadial.Core.Services
{
    /// <summary>
    /// Atributo para marcar servicios que deben registrarse automáticamente
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MRServiceAttribute : Attribute
    {
        /// <summary>
        /// Interfaces por las que registrar el servicio
        /// </summary>
        public Type[] ServiceInterfaces { get; set; }
        
        /// <summary>
        /// Constructor del atributo
        /// </summary>
        /// <param name="serviceInterfaces">Interfaces por las que registrar</param>
        public MRServiceAttribute(params Type[] serviceInterfaces)
        {
            ServiceInterfaces = serviceInterfaces ?? new Type[0];
        }
    }
}