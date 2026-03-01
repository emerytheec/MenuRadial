# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## RESUMEN DEL PROYECTO

**Menu Radial (MR)** — Sistema no-destructivo para generar menús de expresiones radiales en avatares VRChat.
Pipeline: Detección → Organización → Generación de archivos VRChat → Build NDMF.

- **Versión**: 0.9.74
- **Unity**: 2022.3.22f1
- **VRChat SDK**: Avatars 3.5.0+
- **NDMF**: 1.4.0+
- **Autor**: Bender_Dios (MIT)

## Desarrollo

- **Proyecto Unity** — Sin CLI. Compilación automática al guardar.
- **Tests**: Window > General > Test Runner
- **Localización**: 6 idiomas (es, en, zh, ja, ko, ru)

## Estructura

```
Assets/Bender_Dios/MenuRadial/          → Código fuente (desarrollo)
Packages/com.bender-dios.menu-radial/   → Ruta en distribución VPM
Assets/Bender_Dios/Generated/           → Archivos generados (animaciones, controllers)
```

- **VPM**: Se desarrolla en `Assets/` pero VPM instala en `Packages/com.bender-dios.menu-radial/`
- **Iconos** (`[Icon]`): Usan ruta `Packages/` — no se ven en desarrollo, sí en distribución
- **Localización**: `Resources.Load()` con reintento lazy (`MAX_RETRIES=5`) para race conditions VPM

## Assemblies

| Assembly | Plataforma | Propósito |
|----------|-----------|-----------|
| `BenderDios.MenuRadial.Runtime` | All | Componentes, referencias, utilidades |
| `BenderDios.MenuRadial.Editor` | Editor | Inspectors, plugins NDMF |
| `BenderDios.MenuRadial.Menu` | All | MRMenuControl, generadores VRChat |
| `BenderDios.MenuRadial.Menu.Editor` | Editor | Inspectors del menú |
| `BenderDios.MenuRadial.Localization` | Editor | Sistema de idiomas |

**Dependencias clave del Editor assembly**: Runtime, Menu, Localization, `nadena.dev.ndmf`
**Version Define**: `MR_NDMF_AVAILABLE` cuando NDMF >= 1.0.0

---

## ARQUITECTURA

### Jerarquía de Componentes

```
MRMenuRadial (Orquestador)        Runtime/Components/MenuRadial/
├── MRCoserRopa                   Runtime/Components/CoserRopa/      → Cosido ropa/pelucas
├── MROrganizaPB                  Runtime/Components/OrganizaPB/     → Organiza PhysBones
├── MRAjustarBounds               Runtime/Components/AjustarBounds/  → Unifica bounds
├── MRAnalisisColision            Runtime/Components/AnalisisColision/ → Conflictos MA
├── MRPesoTexturas                Runtime/Components/PesoTexturas/   → Análisis texturas
└── MRMenuControl                 Components/Menu/                   → Genera archivos VRChat
        ├── MRUnificarObjetos (Radiales)   Runtime/Components/Radial/
        │   └── MRAgruparObjetos (Frames)  Runtime/Components/Frame/
        │       └── ObjectReference, MaterialReference, BlendshapeReference
        ├── MRUnificarMateriales           Runtime/Components/UnifyMaterial/
        │   └── MRAgruparMateriales        Runtime/Components/AlternativeMaterial/
        │       ├── MRMaterialSlot  └── MRMaterialGroup
        └── MRIluminacionRadial            Runtime/Components/Illumination/
```

### Tipos de Animación

```csharp
public enum AnimationType { None, OnOff, AB, Linear, SubMenu }
// NO existe valor Auto. El tipo se determina por cantidad de frames.
```

| Tipo | Frames | Archivos generados | Parámetro VRChat |
|------|--------|--------------------|------------------|
| OnOff | 1 | `_on.anim`, `_off.anim` | Bool (1 bit) |
| AB | 2 | `_A.anim`, `_B.anim` | Bool (1 bit) |
| Linear | 3+ | `_lin.anim` (255 frames @ 60fps = 4.25s) | Float (8 bits) |

### Sistema de Referencias (`Runtime/Core/Common/`)

Todas heredan de `ReferenceBase<T>`:

- **ObjectReference**: `Target` (GameObject) + `IsActive` (estado DESEADO, no actual). `Equals()` compara Target + IsActive. `ReferenceListManager` deduplica solo por Target (intencional)
- **MaterialReference**: `TargetRenderer` + `MaterialIndex` + `AlternativeMaterial` + `OriginalMaterial`. Path: `m_Materials.Array.data[{index}]`
- **BlendshapeReference**: `SkinnedMeshRenderer` + `BlendshapeName` + `Value` (0-100). Path: `blendShape.{name}`

### Sistema de Materiales (3 capas)

```
MRUnificarMateriales (siempre Linear, 255 frames)
└── MRAgruparMateriales (por prenda)
    ├── MRMaterialSlot (Renderer + índice)
    └── MRMaterialGroup (≥2 materiales para ser válido)
```

Distribución: `N materiales → 255/N frames cada uno, sobrantes al último`
Binding usa `ObjectReferenceKeyframe` (NO curvas float) con `typeof(SkinnedMeshRenderer)`

### Preview (`Runtime/Core/Preview/`)

**PreviewManager** es clase **estática** (NO singleton con instancia):
- `PreviewManager.ActivatePreview(IPreviewable, MRMenuControl)`
- `PreviewManager.DeactivateCurrentPreview()`
- `PreviewManager.ClearAll()` ← usar esta, `ResetAllPreviews()` no existe
- Componentes registrados via `WeakReference` para evitar memory leaks

---

## PLUGINS NDMF

### Orden de ejecución (CRÍTICO)

```
Resolving.BeforeMA:    MRDisableMAPlugin, MRAnalisisColisionPlugin, MROrganizaPBPlugin
Transforming.BeforeMA: MRCoserRopaPlugin, MRMenuRadialPlugin (merge FX/Parameters/Menu)
  → MA procesa (ShapeChanger detecta toggles de MR ✓)
Transforming.AfterMA:  MRAjustarBoundsPlugin, MRMenuRadialCleanupPass
```

### Control de procesos

En `MRMenuRadial`:
- `DisableBoneStitchingNDMF` → NDMF NO cose armatures
- `DisableVRChatMergeNDMF` → NDMF NO mezcla FX/Parameters/Menu

### Gotchas NDMF

- **HierarchyPath es absoluto**: `ReferenceBase.UpdateHierarchyPath()` produce rutas absolutas (sin root). Los builders recalculan paths correctamente, pero el campo `HierarchyPath` en sí NO es relativo al avatar
- **Referencias directas sí se remapean**: `Instantiate()` de Unity remapea correctamente las referencias serializadas al clonar. Usar `objRef.GameObject` en vez de `transform.Find(HierarchyPath)` en contexto NDMF
- **FindMenuRadials por nombre**: Los plugins buscan MRMenuRadial externo comparando nombre del avatar (tras quitar "(Clone)"). Ambiguo si el avatar original y el clon están en escena
- **Excepciones no detienen el build**: Si `ProcessMenuRadial()` lanza excepción, se loguea pero continúa — el avatar puede subirse roto

---

## SISTEMA DE COSIDO (MRCoserRopa)

### Clasificación de piezas (`PieceEntry.DeterminePieceType()`)

Multi-señal con 3 detectores:
- **WigDetector** (threshold=7): 8 señales de scoring, 3 fuentes de candidatos
- **BoneWeightAnalyzer** (>60% head weight en TODOS los meshes)
- **MatchesHairPattern()**: Nombres (hair, wig, pelo, bangs, etc.)

Lógica:
1. `isWig + zona Head/None` → Pelo
2. `isWig + zona cuerpo + bone weights/nombre pelo` → Pelo (peluca con MergeArmature)
3. `isWig + zona cuerpo sin confirmación` → Ropa (falso positivo WigDetector)
4. `BoneProxy→Head + señales pelo` → Pelo
5. `zona cuerpo` → Ropa; `Head/None sin señales` → Pieza

**AutoMenuGenerator NO usa PieceType** — usa `WigDetector.DetectWigs()` independientemente para separar radiales Outfits/Pelucas. PieceType solo se usa en MRCoserRopaPlugin (warning NDMF).

### BoneNameDatabase (`Runtime/Components/CoserRopa/BoneNames/`)

- 230+ patrones (Blender, MMD, VRM, Unity)
- Normaliza: `ToLowerInvariant().Replace("_","").Replace(".","").Replace(" ","")`
- Huesos IGNORADOS: LeftEye, RightEye, Jaw (rompen expresiones)

---

## REGLAS CRÍTICAS

### Animaciones

- **NUNCA usar `activeInHierarchy`** para valores de animación — usar `IsActive` de ObjectReference
- **Write Defaults = OFF** obligatorio (VRChat best practice)
- **Interruption Source = None** en todas las transiciones
- Animaciones de materiales usan `ObjectReferenceKeyframe`, NO curvas float

### VRChat Limits

- **256 bits máximo** para parámetros (Bool=1 bit, Float/Int=8 bits)
- **8 controles máximo** por menú VRChat
- **FX Layer**: Todo lo que no sea humanoid transforms va aquí (GameObjects, materiales, blendshapes, shaders)

### Unity

- **SIEMPRE `sharedMaterials`**, NUNCA `materials` (memory leaks en Edit Mode)
- **OnValidate NUNCA debe crear GameObjects** — causa duplicación en domain reloads
- **`DestroyImmediate`** solo en Editor, **`Destroy`** en Runtime
- **NO destruir BoneProxy** de MA — rompe unión de pelucas/accesorios
- **MA Mesh Settings SÍ se destruye** — interfiere con bounds unificados
- Componentes MA problemáticos se **destruyen** (no solo desactivan) — MA encuentra desactivados con `GetComponentsInChildren(true)`

### MRMenuRadial

- `_isPropagating` es guard de re-entrancia — previene loops infinitos en `PropagateAvatarToChildren()`
- `Reset()` usa asignación directa (`_avatarRoot = found`), NO el setter (que dispara propagación)
- El setter solo se usa desde el editor `EndChangeCheck`
- **UNA sola ruta de creación** para Menu Control: `GetOrCreateMenuControl` en AutoMenuGenerator

---

## GENERACIÓN DE ANIMACIONES

### RadialAnimationBuilder (`Components/Menu/AnimationSystem/`)

**Para OnOff/AB (1-2 frames)**:
- Frame A: usar `IsActive` directamente
- Frame B: INVERTIR `IsActive`

**Para Linear (3+ frames)**:
- 255 frames total distribuidos uniformemente
- `gameObjectBindings` almacena `(frameIndex, tStart, isActive)`
- Usar `regionData.isActive` en las curvas, NO asumir que existencia = 1

### UnifyMaterialAnimationBuilder

- Binding: `m_Materials.Array.data[index]` con `typeof(SkinnedMeshRenderer)`
- Usa `ObjectReferenceKeyframe` (no float) porque materiales son assets

### Generadores VRChat (`Components/Menu/Generators/`)

| Generador | Propósito |
|-----------|-----------|
| `MRVRChatFileGenerator` | Orquesta todos los generadores |
| `MRFXControllerGenerator` | AnimatorController FX |
| `MRParametersGenerator` | VRCExpressionParameters |
| `MRMenuGenerator` | VRCExpressionsMenu |

Configuración de transiciones: `duration=0, hasExitTime=false, exitTime=0, writeDefaultValues=false`

---

## LOCALIZACIÓN

- **Locale files**: `Localization/Resources/Locales/{lang}.json`
- **Patrón**: `using L = ...MRLocalizationKeys; MRLocalization.Get(L.Section.KEY, args)`
- **LocaleSection**: Clase plana — necesita campo por cada key del JSON (sin él, JsonUtility ignora el valor)
- **zh.json**: Usar `「」` en vez de `"` `"` (rompen JSON)
- **Reintento lazy**: Si `_translations.Count==0`, `EnsureInitialized()` reintenta hasta `MAX_RETRIES=5`
- **NO localizable**: MRSlotManager, MRSubMenuManager, SlotNameConflictValidator, MRMenuControl (el assembly Menu no puede referenciar Localization)
- **MRLocalizationKeys**: Tiene clases SEPARADAS (MenuRadial vs MenuRadialEditor) mapeando a la MISMA sección JSON

---

## CONSTANTES (`Runtime/Core/Common/MRConstants.cs`)

SCREAMING_SNAKE_CASE:
- `ANIMATION_OUTPUT_PATH` = "Assets/Bender_Dios/Generated/"
- `TOTAL_FRAMES` = 255, `FRAME_RATE` = 60
- `MAX_SLOTS` = 8, `MAX_PARAMETER_BITS` = 256
- `MAX_BOUNDS_SIZE_XZ` = 5m, `MAX_BOUNDS_SIZE_Y` = 6m

---

## CONVENCIONES

### Nomenclatura

- **Prefijo MR**: Componentes públicos (`MRMenuRadial`, `MRAgruparObjetos`)
- **Sufijo Strategy/Generator/Controller**: Por patrón de diseño

### Namespaces

```
Bender_Dios.MenuRadial.Core.Common          → Referencias, constantes
Bender_Dios.MenuRadial.Core.Preview         → PreviewManager
Bender_Dios.MenuRadial.Components.*         → Componentes específicos
Bender_Dios.MenuRadial.Components.Menu      → Sistema de menú VRChat
Bender_Dios.MenuRadial.Shaders              → Estrategias de shader
Bender_Dios.MenuRadial.Localization         → Localización
```

### Serialización

```csharp
[SerializeField] private VRCAvatarDescriptor _avatar;
public VRCAvatarDescriptor Avatar { get => _avatar; set => _avatar = value; }
```

### Patrones de Diseño

| Patrón | Uso |
|--------|-----|
| Strategy | Shaders (`Runtime/Shaders/Strategies/`) |
| Factory | `ShaderStrategyFactory` |
| Static Manager | `PreviewManager`, `DynamicIconManager` |
| Composite | MenuRadial > Radials > Frames |

### Shaders

- **LilToon**: Completo (`LilToonShaderStrategy.cs`) — _AsUnlit, _LightMaxLimit, _ShadowBorder, _ShadowStrength
- **Poiyomi**: Solo infraestructura (interface + factory). `PoiyomiShaderStrategy` NO existe

---

## KNOWN ISSUES

### Bugs conocidos

1. **HierarchyPath es absoluto, no relativo al avatar** — Los builders recalculan correctamente, pero el campo en sí contiene ruta desde scene root. En NDMF usar referencias directas serializadas

### Notas de diseño verificadas

- **ObjectReference.Equals() SÍ compara IsActive** (línea 103). `ReferenceListManager.IsDuplicateReference()` compara solo por `GameObject` — es diseño intencional (evitar mismo GO duplicado en un frame)
- **FindAvatarInParents() usa reflexión por nombre** — Intencional: assembly Runtime no referencia VRC SDK3A
- **ReferenceListManager.Add() es O(n)** — Aceptable: n típico = 5-30 refs por frame

### Fixes aplicados (v0.9.72+)

1. **Límite 256 bits validado en merge NDMF** — MergeParameters() calcula bit cost antes de añadir cada parámetro
2. **WriteDefaults forzado a OFF en merge NDMF** — CloneStateMachine() siempre usa false
3. **FindMenuRadials filtra clones** — Excluye avatares con "(Clone)" + usa solo primer match
4. **ComputeRegions dead code eliminado** — Solo queda CalculateTimeRegions() que es correcta
5. **PreviewManager protegido contra objetos destruidos** — Verifica si IPreviewable es UnityObject destruido
6. **CloneMenuRecursive con protección** — Límite profundidad (16) + detección ciclos
7. **BlendshapeReference.GetBlendshapeIndex() con cache** — Cache por mesh+nombre, invalidación automática
8. **SplitMenuIfNeeded con límite de profundidad** — Máximo 10 niveles de menús "More"
9. **Excepciones NDMF re-lanzadas** — ProcessMenuRadial() re-throw para que NDMF detenga el build
10. **SetDirty() eliminado de MRMenuControl.OnValidate()** — Los cambios se guardan automáticamente
11. **MRMeshImportFixPlugin docstring corregida** — Dice "detectar" en vez de "corregir"
12. **Log prefixes NDMF normalizados** — Patrón `[MR{Componente} NDMF]` consistente
13. **Dead variables eliminadas** — PreviewManager.CleanupDestroyedComponents()
14. **Verbose debug logging reducido** — MRAnalisisColisionPlugin de ~15 líneas a 1 resumen

---

## TROUBLESHOOTING

| Problema | Solución |
|----------|----------|
| Animaciones no funcionan | Usar `IsActive`, NO `activeInHierarchy`. Write Defaults = OFF |
| Avatar sube sin ropa (NDMF) | Verificar DefaultFrameIndex. Usar refs directas, no HierarchyPath |
| Ropa no se cose | Verificar `BoneNameDatabase`. LeftEye/RightEye/Jaw se ignoran |
| Peluca clasificada como Ropa | WigDetector score (≥7) + BoneWeightAnalyzer (>60% head) |
| Materiales no cambian | Grupo necesita ≥2 materiales. Slot debe estar vinculado |
| Preview no se desactiva | `PreviewManager.ClearAll()` (estática, NO singleton) |
| Localización muestra [key] | Falta campo en LocaleSection de MRLocalization.cs |
| Localización [key] en VPM | Reintento lazy (MAX_RETRIES=5) lo resuelve automáticamente |
| Iconos no se ven en desarrollo | Normal — `[Icon]` usa ruta `Packages/` (solo visible en VPM) |
| Menu Control se duplica | Solo una ruta de creación: `GetOrCreateMenuControl` |
| Meshes ropa confundidos con body | Agregar patrón a `ClothingPatterns` en BodyMeshDetector |

---

## GITHUB / RELEASES

Al pedir "commit", "actualizar github" o "subir cambios":
1. Commit con todos los cambios
2. Bump versión en `package.json`
3. Actualizar `CHANGELOG.md`
4. `git tag vX.X.X && git push origin main --tags`
5. `gh release edit` (CI crea el release automáticamente)

**Repositorio**: `emerytheec/MenuRadial` (git root: `Assets/Bender_Dios/MenuRadial/`)
**VPM**: `emerytheec/vpm-listing` (CI actualiza `index.json`)

CI workflow (`.github/workflows/release.yml`): Push tag `v*` → ZIP + unitypackage → Release → actualiza vpm-listing
