using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Models;
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
                EditorGUILayout.Space(8);

                // Configuracion
                DrawConfigSection();

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

                // Info NDMF
                DrawNDMFInfo();
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
            EditorGUILayout.BeginHorizontal();

            // Boton Escanear
            if (GUILayout.Button(new GUIContent("Escanear", "Re-escanear meshes del avatar"), GUILayout.Height(25)))
            {
                Undo.RecordObject(_target, "Escanear Avatar");
                _target.ScanAvatar();
                EditorUtility.SetDirty(_target);
            }

            // Boton Calcular
            GUI.enabled = _target.DetectedMeshCount > 0;
            if (GUILayout.Button(new GUIContent("Calcular", "Calcular bounds unificados"), GUILayout.Height(25)))
            {
                Undo.RecordObject(_target, "Calcular Bounds");
                _target.CalculateBounds();
                EditorUtility.SetDirty(_target);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();

            // Boton Aplicar
            GUI.enabled = _target.HasValidCalculation && !_target.BoundsApplied;
            GUI.backgroundColor = _target.HasValidCalculation && !_target.BoundsApplied ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
            if (GUILayout.Button(new GUIContent("Aplicar Bounds", "Aplicar bounds unificados a todos los meshes"), GUILayout.Height(30)))
            {
                Undo.RecordObject(_target, "Aplicar Bounds");
                // Registrar Undo para cada mesh
                foreach (var meshInfo in _target.DetectedMeshes)
                {
                    if (meshInfo.IsValid && meshInfo.Renderer != null)
                    {
                        Undo.RecordObject(meshInfo.Renderer, "Aplicar Bounds");
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
                // Registrar Undo para cada mesh
                foreach (var meshInfo in _target.DetectedMeshes)
                {
                    if (meshInfo.IsValid && meshInfo.Renderer != null)
                    {
                        Undo.RecordObject(meshInfo.Renderer, "Restaurar Bounds");
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
            EditorGUILayout.LabelField("Estado", EditorStyles.miniBoldLabel, GUILayout.Width(60));
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

            // Estado
            string status = meshInfo.IsValid ? "OK" : "Invalido";
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(60));

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

        #region NDMF Info

        private void DrawNDMFInfo()
        {
            if (_target.HasValidCalculation && _target.ValidMeshCount > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.contentColor = SuccessColor;
                string status = _target.BoundsApplied ? "aplicados" : "listos para aplicar";
                EditorGUILayout.LabelField($"[OK] {_target.ValidMeshCount} mesh(es) con bounds {status}", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.LabelField(
                    "Los bounds se procesaran automaticamente al:",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    "  - Entrar en Play Mode\n  - Subir el avatar a VRChat",
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.EndVertical();
            }
            else if (_target.DetectedMeshCount > 0)
            {
                EditorGUILayout.HelpBox(
                    "Calcula los bounds para que se procesen automaticamente.",
                    MessageType.Info);
            }
        }

        #endregion

        #region Scene GUI

        private void OnSceneGUI()
        {
            if (_target == null || !_target.HasValidCalculation)
                return;

            // Dibujar el bounding box unificado en la escena
            var bounds = _target.LastCalculationResult.UnifiedBoundsWithMargin;

            if (_target.AvatarRoot != null)
            {
                // Transformar bounds al world space
                Matrix4x4 matrix = _target.AvatarRoot.transform.localToWorldMatrix;
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
