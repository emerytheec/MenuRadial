using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;
using Bender_Dios.MenuRadial.Editor.Components.Frame.Modules;

namespace Bender_Dios.MenuRadial.Editor.Components.AjustarBounds
{
    /// <summary>
    /// Editor personalizado para MRAjustarBounds.
    /// Proporciona interfaz visual para escanear, calcular y aplicar bounds unificados.
    /// </summary>
    [CustomEditor(typeof(MRAjustarBounds))]
    public class MRAjustarBoundsEditor : UnityEditor.Editor
    {
        private MRAjustarBounds _target;
        private bool _showMeshList = false;
        private bool _showParticleList = false;
        private Vector2 _meshListScrollPos;
        private Vector2 _particleListScrollPos;

        // Colores
        private static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color ErrorColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color AppliedColor = new Color(0.3f, 0.6f, 0.9f);

        private void OnEnable()
        {
            _target = (MRAjustarBounds)target;
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            // Header
            DrawHeader();
            EditorGUILayout.Space(5);

            // Avatar
            DrawAvatarSection();

            // Solo mostrar el resto si hay avatar
            if (_target.AvatarRoot != null)
            {
                // Eliminar MA Mesh Settings automaticamente si existen
                AutoDestroyMAMeshSettings();

                EditorGUILayout.Space(8);

                // Configuracion
                DrawConfigSection();

                EditorGUILayout.Space(8);

                // Anchor Override
                DrawAnchorSection();

                EditorGUILayout.Space(8);

                // Resultado del calculo
                DrawResultSection();

                EditorGUILayout.Space(8);

                // Botones de accion
                DrawActionButtons();

                EditorGUILayout.Space(8);

                // Lista de meshes (foldout)
                DrawMeshList();

                EditorGUILayout.Space(8);

                // Seccion de particulas
                DrawParticleSection();

                EditorGUILayout.Space(8);

                // Info de MA Mesh Settings eliminados
                DrawMAMeshSettingsInfo();

                EditorGUILayout.Space(8);

                // Resumen de estado
                DrawStatusSummary();
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "Arrastra tu avatar aqui para escanear sus meshes y calcular bounds unificados.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Header & Avatar

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MR Ajustar Bounds", EditorStyleManager.HeaderStyle);
            EditorGUILayout.LabelField(
                "Unifica los bounds de todos los meshes del avatar",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawAvatarSection()
        {
            EditorGUI.BeginChangeCheck();
            var newAvatar = (GameObject)EditorGUILayout.ObjectField(
                "Avatar",
                _target.AvatarRoot, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && newAvatar != _target.AvatarRoot)
            {
                Undo.RecordObject(_target, "Cambiar Avatar");
                _target.AvatarRoot = newAvatar;
                EditorUtility.SetDirty(_target);
            }

            // Info del avatar
            if (_target.AvatarRoot != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth + 2);
                GUI.contentColor = SuccessColor;
                EditorGUILayout.LabelField($"{_target.DetectedMeshCount} SkinnedMeshRenderer detectados", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Configuration

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("Configuracion", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Margen
            EditorGUI.BeginChangeCheck();
            float newMargin = EditorGUILayout.Slider(
                new GUIContent("Margen Extra", "Porcentaje adicional de tamanio (0.1 = 10%)"),
                _target.MarginPercentage, 0f, 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar Margen");
                _target.MarginPercentage = newMargin;
                EditorUtility.SetDirty(_target);
            }

            // Mostrar porcentaje
            EditorGUILayout.LabelField($"  = {(_target.MarginPercentage * 100):F0}% extra", EditorStyles.miniLabel);

            EditorGUILayout.Space(3);

            // Auto-aplicar
            EditorGUI.BeginChangeCheck();
            bool newAutoApply = EditorGUILayout.Toggle(
                new GUIContent("Auto-aplicar", "Aplicar bounds automaticamente al detectar avatar"),
                _target.AutoApply);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar Auto-aplicar");
                _target.AutoApply = newAutoApply;
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Anchor Override

        private void DrawAnchorSection()
        {
            EditorGUILayout.LabelField("Anchor Override (Iluminacion)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Toggle para habilitar anchor
            EditorGUI.BeginChangeCheck();
            bool newUnifyAnchor = EditorGUILayout.Toggle(
                new GUIContent("Unificar Iluminacion", "Todos los meshes usaran el mismo punto de referencia para iluminacion"),
                _target.UnifyAnchorOverride);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Anchor Override");
                _target.UnifyAnchorOverride = newUnifyAnchor;
                EditorUtility.SetDirty(_target);
            }

            if (_target.UnifyAnchorOverride)
            {
                EditorGUILayout.Space(3);

                // Auto-detectar Chest
                EditorGUI.BeginChangeCheck();
                bool newAutoDetect = EditorGUILayout.Toggle(
                    new GUIContent("Auto-detectar Chest", "Usar automaticamente el hueso Chest del avatar"),
                    _target.AutoDetectChest);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Toggle Auto-detectar Chest");
                    _target.AutoDetectChest = newAutoDetect;
                    EditorUtility.SetDirty(_target);
                }

                // Campo para anchor manual (solo si no es auto-detectar)
                if (!_target.AutoDetectChest)
                {
                    EditorGUI.BeginChangeCheck();
                    var newAnchor = (Transform)EditorGUILayout.ObjectField(
                        new GUIContent("Anchor Manual", "Transform a usar como punto de referencia"),
                        _target.AnchorOverride, typeof(Transform), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_target, "Cambiar Anchor");
                        _target.AnchorOverride = newAnchor;
                        EditorUtility.SetDirty(_target);
                    }
                }

                // Mostrar anchor efectivo
                var effectiveAnchor = _target.EffectiveAnchor;
                if (effectiveAnchor != null)
                {
                    EditorGUILayout.Space(3);
                    GUI.contentColor = SuccessColor;
                    EditorGUILayout.LabelField($"Anchor: {effectiveAnchor.name}", EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;

                    // Estado de aplicacion
                    if (_target.AnchorApplied)
                    {
                        GUI.contentColor = AppliedColor;
                        EditorGUILayout.LabelField("Anchor APLICADO", EditorStyles.miniBoldLabel);
                        GUI.contentColor = Color.white;
                    }
                }
                else
                {
                    EditorGUILayout.Space(3);
                    GUI.contentColor = WarningColor;
                    EditorGUILayout.LabelField("No se detecto hueso Chest", EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;
                }

                EditorGUILayout.Space(5);

                // Botones de accion para anchor
                EditorGUILayout.BeginHorizontal();

                // Boton Aplicar Anchor
                GUI.enabled = effectiveAnchor != null && !_target.AnchorApplied;
                if (GUILayout.Button("Aplicar Anchor", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_target, "Aplicar Anchor Override");
                    foreach (var meshInfo in _target.DetectedMeshes)
                    {
                        if (meshInfo.IsValid && meshInfo.Renderer != null)
                        {
                            Undo.RecordObject(meshInfo.Renderer, "Aplicar Anchor Override");
                        }
                    }
                    _target.ApplyAnchorOverride();
                    EditorUtility.SetDirty(_target);
                }
                GUI.enabled = true;

                // Boton Restaurar Anchor
                GUI.enabled = _target.AnchorApplied;
                if (GUILayout.Button("Restaurar Anchor", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_target, "Restaurar Anchor Override");
                    foreach (var meshInfo in _target.DetectedMeshes)
                    {
                        if (meshInfo.IsValid && meshInfo.Renderer != null)
                        {
                            Undo.RecordObject(meshInfo.Renderer, "Restaurar Anchor Override");
                        }
                    }
                    _target.RestoreAnchorOverride();
                    EditorUtility.SetDirty(_target);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // Ayuda contextual
            if (_target.UnifyAnchorOverride && _target.EffectiveAnchor == null)
            {
                EditorGUILayout.HelpBox(
                    "El Anchor Override unifica la iluminacion de todos los meshes.\n" +
                    "Sin esto, diferentes partes del avatar pueden verse mas claras u oscuras.",
                    MessageType.Info);
            }

        }

        #endregion

        #region Result

        private void DrawResultSection()
        {
            EditorGUILayout.LabelField("Resultado", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_target.HasValidCalculation)
            {
                var result = _target.LastCalculationResult;

                // Estado
                Color statusColor = _target.BoundsApplied ? AppliedColor : SuccessColor;
                string statusText = _target.BoundsApplied ? "APLICADO" : "CALCULADO";

                GUI.contentColor = statusColor;
                EditorGUILayout.LabelField($"[{statusText}]", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                // Detalles del bounding box
                EditorGUILayout.Space(3);

                var bounds = result.UnifiedBoundsWithMargin;

                EditorGUILayout.LabelField("Bounding Box Unificado:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"  Centro: ({bounds.center.x:F2}, {bounds.center.y:F2}, {bounds.center.z:F2})", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  Tamanio: {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2} metros", EditorStyles.miniLabel);

                EditorGUILayout.Space(3);

                EditorGUILayout.LabelField("Limites verticales:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"  Punto mas bajo (Y min): {bounds.min.y:F2}m", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  Punto mas alto (Y max): {bounds.max.y:F2}m", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  Altura total: {bounds.size.y:F2}m", EditorStyles.miniLabel);

                EditorGUILayout.Space(3);

                EditorGUILayout.LabelField($"Meshes procesados: {result.ValidMeshCount}/{result.MeshCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Margen aplicado: {(result.MarginPercentage * 100):F0}%", EditorStyles.miniLabel);

                // Aviso si fue limitado a max VRChat
                if (result.WasClamped)
                {
                    GUI.contentColor = WarningColor;
                    EditorGUILayout.LabelField("Bounds limitados al maximo de VRChat (Very Poor - 1cm)", EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;
                }

                // rootBone compartido
                if (_target.SharedRootBone != null)
                {
                    EditorGUILayout.LabelField($"rootBone: {_target.SharedRootBone.name} (espacio de referencia)", EditorStyles.miniLabel);
                }
            }
            else if (_target.LastCalculationResult != null)
            {
                // Error en calculo
                GUI.contentColor = ErrorColor;
                EditorGUILayout.LabelField("[ERROR]", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                foreach (var error in _target.LastCalculationResult.Errors)
                {
                    EditorGUILayout.LabelField($"  - {error}", EditorStyles.miniLabel);
                }
            }
            else
            {
                // Sin calculo
                GUI.contentColor = WarningColor;
                EditorGUILayout.LabelField("Sin calcular", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
                EditorGUILayout.LabelField("Haz clic en 'Calcular' para obtener los bounds unificados", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Action Buttons

        private void DrawActionButtons()
        {
            // Boton Escanear (escanea + calcula en un solo paso)
            var prevBgScan = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.6f, 1f);
            if (GUILayout.Button(new GUIContent("Escanear", "Re-escanear meshes del avatar y calcular bounds"), GUILayout.Height(25)))
            {
                Undo.RecordObject(_target, "Escanear y Calcular Bounds");
                _target.ScanAvatar();
                if (_target.DetectedMeshCount > 0)
                {
                    _target.CalculateBounds();
                }
                EditorUtility.SetDirty(_target);
            }
            GUI.backgroundColor = prevBgScan;

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();

            // Boton Aplicar
            GUI.enabled = _target.HasValidCalculation && !_target.BoundsApplied;
            GUI.backgroundColor = _target.HasValidCalculation && !_target.BoundsApplied ? new Color(1f, 0.4f, 0.4f) : Color.white;
            if (GUILayout.Button(new GUIContent("Aplicar Bounds", "Aplicar bounds unificados a todos los meshes"), GUILayout.Height(30)))
            {
                Undo.RecordObject(_target, "Aplicar Bounds");
                // Registrar Undo para cada renderer y su Transform (se modifica rootBone, localBounds y transform)
                foreach (var meshInfo in _target.DetectedMeshes)
                {
                    if (meshInfo.IsValid && meshInfo.Renderer != null)
                    {
                        Undo.RecordObject(meshInfo.Renderer, "Aplicar Bounds");
                        Undo.RecordObject(meshInfo.Renderer.transform, "Aplicar Bounds");
                    }
                }
                _target.ApplyBounds();
                EditorUtility.SetDirty(_target);
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            // Boton Restaurar
            GUI.enabled = _target.BoundsApplied;
            GUI.backgroundColor = _target.BoundsApplied ? new Color(0.9f, 0.6f, 0.2f) : Color.white;
            if (GUILayout.Button(new GUIContent("Restaurar", "Restaurar bounds originales"), GUILayout.Height(30)))
            {
                Undo.RecordObject(_target, "Restaurar Bounds");
                // Registrar Undo para cada renderer y su Transform (se restaura rootBone, localBounds y transform)
                foreach (var meshInfo in _target.DetectedMeshes)
                {
                    if (meshInfo.IsValid && meshInfo.Renderer != null)
                    {
                        Undo.RecordObject(meshInfo.Renderer, "Restaurar Bounds");
                        Undo.RecordObject(meshInfo.Renderer.transform, "Restaurar Bounds");
                    }
                }
                _target.RestoreBounds();
                EditorUtility.SetDirty(_target);
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Mesh List

        private void DrawMeshList()
        {
            _showMeshList = EditorGUILayout.Foldout(_showMeshList,
                $"Meshes Detectados ({_target.ValidMeshCount}/{_target.DetectedMeshCount})", true);

            if (!_showMeshList || _target.DetectedMeshCount == 0)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mesh", EditorStyles.miniBoldLabel, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField("Bounds Originales", EditorStyles.miniBoldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Estado", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            // Lista con scroll
            _meshListScrollPos = EditorGUILayout.BeginScrollView(_meshListScrollPos, GUILayout.MaxHeight(200));

            foreach (var meshInfo in _target.DetectedMeshes)
            {
                DrawMeshInfoRow(meshInfo);
            }

            EditorGUILayout.EndScrollView();

            // Boton limpiar invalidos
            int invalidCount = _target.DetectedMeshCount - _target.ValidMeshCount;
            if (invalidCount > 0)
            {
                EditorGUILayout.Space(3);
                if (GUILayout.Button($"Eliminar {invalidCount} mesh(es) invalido(s)", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_target, "Eliminar Meshes Invalidos");
                    _target.RemoveInvalidMeshes();
                    EditorUtility.SetDirty(_target);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMeshInfoRow(MeshBoundsInfo meshInfo)
        {
            EditorGUILayout.BeginHorizontal();

            // Estado visual
            Color rowColor = meshInfo.IsValid ? Color.white : new Color(1f, 0.5f, 0.5f, 0.3f);
            GUI.contentColor = meshInfo.IsValid ? Color.white : ErrorColor;

            // Nombre del mesh (clickeable para seleccionar)
            if (meshInfo.Renderer != null)
            {
                if (GUILayout.Button(meshInfo.MeshName, EditorStyles.linkLabel, GUILayout.MinWidth(100)))
                {
                    Selection.activeGameObject = meshInfo.Renderer.gameObject;
                    EditorGUIUtility.PingObject(meshInfo.Renderer.gameObject);
                }
            }
            else
            {
                EditorGUILayout.LabelField(meshInfo.MeshName ?? "(null)", GUILayout.MinWidth(100));
            }

            // Bounds originales
            if (meshInfo.IsValid)
            {
                var size = meshInfo.OriginalBounds.size;
                EditorGUILayout.LabelField($"{size.x:F2}x{size.y:F2}x{size.z:F2}", EditorStyles.miniLabel, GUILayout.Width(150));
            }
            else
            {
                EditorGUILayout.LabelField("-", EditorStyles.miniLabel, GUILayout.Width(150));
            }

            // Estado (distinguir: invalido, sin huesos, OK)
            string status;
            if (!meshInfo.IsValid)
            {
                status = "Invalido";
            }
            else if (!meshInfo.HasBones)
            {
                if (meshInfo.BoundsCurrentlyApplied)
                {
                    GUI.contentColor = AppliedColor;
                    status = "Limitado";
                }
                else
                {
                    GUI.contentColor = WarningColor;
                    status = "Sin huesos";
                }
            }
            else
            {
                status = "OK";
            }
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(70));

            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Particle Section

        private void DrawParticleSection()
        {
            EditorGUILayout.LabelField("Particulas", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Checkbox para incluir particulas
            EditorGUI.BeginChangeCheck();
            bool newIncludeParticles = EditorGUILayout.Toggle(
                new GUIContent("Incluir Particulas", "Ajustar bounds de sistemas de particulas"),
                _target.IncludeParticles);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Particulas");
                _target.IncludeParticles = newIncludeParticles;

                // Escanear particulas si se habilita
                if (newIncludeParticles && _target.DetectedParticleCount == 0)
                {
                    _target.ScanParticles();
                    _target.CalculateParticleBounds();
                }

                EditorUtility.SetDirty(_target);
            }

            // Solo mostrar opciones de particulas si esta habilitado
            if (_target.IncludeParticles)
            {
                EditorGUILayout.Space(5);

                // Margen de particulas
                EditorGUI.BeginChangeCheck();
                float newParticleMargin = EditorGUILayout.Slider(
                    new GUIContent("Margen Particulas", "Porcentaje adicional para particulas (0.2 = 20%)"),
                    _target.ParticleMarginPercentage, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Cambiar Margen Particulas");
                    _target.ParticleMarginPercentage = newParticleMargin;
                    EditorUtility.SetDirty(_target);
                }

                EditorGUILayout.LabelField($"  = {(_target.ParticleMarginPercentage * 100):F0}% extra", EditorStyles.miniLabel);

                EditorGUILayout.Space(5);

                // Info de particulas detectadas
                if (_target.DetectedParticleCount > 0)
                {
                    GUI.contentColor = SuccessColor;
                    EditorGUILayout.LabelField($"{_target.ValidParticleCount} particulas detectadas", EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;

                    // Estado de aplicacion
                    if (_target.ParticleBoundsApplied)
                    {
                        GUI.contentColor = AppliedColor;
                        EditorGUILayout.LabelField("Bounds de particulas APLICADOS", EditorStyles.miniLabel);
                        GUI.contentColor = Color.white;
                    }
                }
                else
                {
                    GUI.contentColor = WarningColor;
                    EditorGUILayout.LabelField("No se detectaron particulas", EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;
                }

                EditorGUILayout.Space(5);

                // Botones de accion para particulas
                EditorGUILayout.BeginHorizontal();

                // Boton Escanear Particulas
                if (GUILayout.Button("Escanear", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_target, "Escanear Particulas");
                    _target.ScanParticles();
                    _target.CalculateParticleBounds();
                    EditorUtility.SetDirty(_target);
                }

                // Boton Aplicar Particulas
                GUI.enabled = _target.DetectedParticleCount > 0 && !_target.ParticleBoundsApplied;
                if (GUILayout.Button("Aplicar", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_target, "Aplicar Bounds Particulas");
                    foreach (var particleInfo in _target.DetectedParticles)
                    {
                        if (particleInfo.IsValid && particleInfo.Renderer != null)
                        {
                            Undo.RecordObject(particleInfo.Renderer, "Aplicar Bounds Particulas");
                        }
                    }
                    _target.ApplyParticleBounds();
                    EditorUtility.SetDirty(_target);
                }
                GUI.enabled = true;

                // Boton Restaurar Particulas
                GUI.enabled = _target.ParticleBoundsApplied;
                if (GUILayout.Button("Restaurar", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_target, "Restaurar Bounds Particulas");
                    foreach (var particleInfo in _target.DetectedParticles)
                    {
                        if (particleInfo.IsValid && particleInfo.Renderer != null)
                        {
                            Undo.RecordObject(particleInfo.Renderer, "Restaurar Bounds Particulas");
                        }
                    }
                    _target.RestoreParticleBounds();
                    EditorUtility.SetDirty(_target);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                // Lista de particulas (foldout)
                DrawParticleList();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawParticleList()
        {
            if (_target.DetectedParticleCount == 0)
                return;

            EditorGUILayout.Space(5);

            _showParticleList = EditorGUILayout.Foldout(_showParticleList,
                $"Lista de Particulas ({_target.ValidParticleCount}/{_target.DetectedParticleCount})", true);

            if (!_showParticleList)
                return;

            // Lista con scroll
            _particleListScrollPos = EditorGUILayout.BeginScrollView(_particleListScrollPos, GUILayout.MaxHeight(150));

            foreach (var particleInfo in _target.DetectedParticles)
            {
                DrawParticleInfoRow(particleInfo);
            }

            EditorGUILayout.EndScrollView();

            // Boton limpiar invalidos
            int invalidCount = _target.DetectedParticleCount - _target.ValidParticleCount;
            if (invalidCount > 0)
            {
                if (GUILayout.Button($"Eliminar {invalidCount} particula(s) invalida(s)", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_target, "Eliminar Particulas Invalidas");
                    _target.RemoveInvalidParticles();
                    EditorUtility.SetDirty(_target);
                }
            }
        }

        private void DrawParticleInfoRow(ParticleBoundsInfo particleInfo)
        {
            EditorGUILayout.BeginHorizontal();

            GUI.contentColor = particleInfo.IsValid ? Color.white : ErrorColor;

            // Nombre de la particula (clickeable para seleccionar)
            if (particleInfo.ParticleSystem != null)
            {
                if (GUILayout.Button(particleInfo.ParticleName, EditorStyles.linkLabel, GUILayout.MinWidth(120)))
                {
                    Selection.activeGameObject = particleInfo.ParticleSystem.gameObject;
                    EditorGUIUtility.PingObject(particleInfo.ParticleSystem.gameObject);
                }
            }
            else
            {
                EditorGUILayout.LabelField(particleInfo.ParticleName ?? "(null)", GUILayout.MinWidth(120));
            }

            // Info de la particula
            if (particleInfo.IsValid && particleInfo.ParticleSystem != null)
            {
                EditorGUILayout.LabelField(particleInfo.GetParticleInfo(), EditorStyles.miniLabel);
            }

            // Estado
            string status = particleInfo.IsValid ? "OK" : "Invalido";
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(50));

            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region MA Mesh Settings

        /// <summary>
        /// Contador de MA Mesh Settings eliminados en la ultima accion.
        /// Se muestra como info en el inspector.
        /// </summary>
        [System.NonSerialized] private int _lastMAMeshSettingsDestroyed;

        /// <summary>
        /// Detecta y elimina MA Mesh Settings automaticamente.
        /// Se llama cada vez que se dibuja el inspector con avatar asignado.
        /// </summary>
        private void AutoDestroyMAMeshSettings()
        {
            if (_target.AvatarRoot == null) return;
            if (!ModularAvatarDetector.Instance.HasMeshSettings(_target.AvatarRoot)) return;

            _lastMAMeshSettingsDestroyed = ModularAvatarDetector.Instance.DestroyMeshSettingsComponents(_target.AvatarRoot);
        }

        private void DrawMAMeshSettingsInfo()
        {
            if (_lastMAMeshSettingsDestroyed > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.contentColor = new Color(0.4f, 0.7f, 1f); // Azul MA
                EditorGUILayout.LabelField("MA Mesh Settings eliminados", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.LabelField(
                    $"Se eliminaron {_lastMAMeshSettingsDestroyed} componente(s) MA Mesh Settings.\n" +
                    "MR Ajustar Bounds recalcula los bounds desde cero,\n" +
                    "MA Mesh Settings interfiere con este calculo.",
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.EndVertical();
            }
        }

        #endregion

        #region Status Summary

        private void DrawStatusSummary()
        {
            if (_target.HasValidCalculation && _target.ValidMeshCount > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Estado de bounds
                if (_target.BoundsApplied)
                {
                    GUI.contentColor = AppliedColor;
                    EditorGUILayout.LabelField($"Bounds APLICADOS a {_target.ValidMeshCount} mesh(es)", EditorStyles.boldLabel);
                }
                else
                {
                    GUI.contentColor = SuccessColor;
                    EditorGUILayout.LabelField($"Bounds calculados para {_target.ValidMeshCount} mesh(es) (sin aplicar)", EditorStyles.boldLabel);
                }
                GUI.contentColor = Color.white;

                // Meshes sin huesos: distinguir entre saltados y limitados
                int bonelessCount = 0;
                int clampedCount = 0;
                foreach (var m in _target.DetectedMeshes)
                {
                    if (m.IsValid && !m.HasBones)
                    {
                        bonelessCount++;
                        if (m.BoundsCurrentlyApplied) clampedCount++;
                    }
                }
                if (bonelessCount > 0)
                {
                    if (clampedCount > 0)
                    {
                        GUI.contentColor = AppliedColor;
                        EditorGUILayout.LabelField($"{clampedCount} mesh(es) sin huesos limitado(s) a max VRChat", EditorStyles.miniLabel);
                        GUI.contentColor = Color.white;
                    }
                    int justSkipped = bonelessCount - clampedCount;
                    if (justSkipped > 0)
                    {
                        GUI.contentColor = WarningColor;
                        EditorGUILayout.LabelField($"{justSkipped} mesh(es) sin huesos (bounds OK)", EditorStyles.miniLabel);
                        GUI.contentColor = Color.white;
                    }
                }

                // Estado de anchor
                if (_target.UnifyAnchorOverride)
                {
                    var anchor = _target.EffectiveAnchor;
                    if (anchor != null)
                    {
                        string anchorStatus = _target.AnchorApplied ? "APLICADO" : "listo";
                        GUI.contentColor = _target.AnchorApplied ? AppliedColor : SuccessColor;
                        EditorGUILayout.LabelField($"Anchor: {anchor.name} ({anchorStatus})", EditorStyles.miniLabel);
                        GUI.contentColor = Color.white;
                    }
                    else
                    {
                        GUI.contentColor = WarningColor;
                        EditorGUILayout.LabelField("Anchor: no detectado", EditorStyles.miniLabel);
                        GUI.contentColor = Color.white;
                    }
                }

                EditorGUILayout.EndVertical();
            }
            else if (_target.DetectedMeshCount > 0)
            {
                EditorGUILayout.HelpBox(
                    "Haz clic en 'Calcular' para obtener los bounds unificados y luego 'Aplicar' para verlos en la escena.",
                    MessageType.Info);
            }
        }

        #endregion

        #region Scene GUI

        private void OnSceneGUI()
        {
            if (_target == null || !_target.HasValidCalculation)
                return;

            // Los bounds estan en espacio del rootBone (Hips)
            var bounds = _target.LastCalculationResult.UnifiedBoundsWithMargin;
            var rootBone = _target.SharedRootBone;

            if (rootBone != null)
            {
                // Transformar bounds de rootBone-local a world space
                Matrix4x4 matrix = rootBone.localToWorldMatrix;
                Handles.matrix = matrix;

                // Color segun estado
                Color boundsColor = _target.BoundsApplied
                    ? new Color(0.3f, 0.6f, 0.9f, 0.5f)
                    : new Color(0.3f, 0.9f, 0.3f, 0.5f);

                Handles.color = boundsColor;
                Handles.DrawWireCube(bounds.center, bounds.size);

                // Dibujar centro
                Handles.color = Color.yellow;
                float handleSize = HandleUtility.GetHandleSize(bounds.center) * 0.1f;
                Handles.DrawWireDisc(bounds.center, Vector3.up, handleSize);
                Handles.DrawWireDisc(bounds.center, Vector3.right, handleSize);
                Handles.DrawWireDisc(bounds.center, Vector3.forward, handleSize);

                Handles.matrix = Matrix4x4.identity;
            }
        }

        #endregion
    }
}
