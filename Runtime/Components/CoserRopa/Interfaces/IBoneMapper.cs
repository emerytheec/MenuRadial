using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Interfaces
{
    /// <summary>
    /// Interfaz para estrategias de mapeo de huesos
    /// </summary>
    public interface IBoneMapper
    {
        /// <summary>
        /// Detecta y mapea huesos entre avatar y ropa
        /// </summary>
        /// <param name="avatar">Referencia al armature del avatar</param>
        /// <param name="clothing">Referencia al armature de la ropa</param>
        /// <returns>Lista de mapeos de huesos detectados</returns>
        List<BoneMapping> DetectBoneMappings(ArmatureReference avatar, ArmatureReference clothing);

        /// <summary>
        /// Obtiene un hueso especifico por tipo HumanBodyBones
        /// </summary>
        /// <param name="animator">Animator del cual obtener el hueso</param>
        /// <param name="boneType">Tipo de hueso a obtener</param>
        /// <returns>Transform del hueso o null si no existe</returns>
        Transform GetBone(Animator animator, HumanBodyBones boneType);

        /// <summary>
        /// Intenta encontrar un hueso por nombre (fallback)
        /// </summary>
        /// <param name="root">Transform raiz donde buscar</param>
        /// <param name="boneType">Tipo de hueso a buscar</param>
        /// <returns>Transform encontrado o null</returns>
        Transform FindBoneByName(Transform root, HumanBodyBones boneType);
    }
}
