# CLAUDE.md

Sistema **MR (Menu Radial)** para avatares VRChat en Unity 2022.3.22f1. Genera animaciones, controllers FX, parámetros y menús VRChat de forma no-destructiva usando NDMF.

## Desarrollo
- **Proyecto Unity** - Sin CLI. Compilación automática al guardar.
- **Tests**: Window → General → Test Runner
- **Unity Version**: 2022.3.22f1
- **VRChat SDK**: VRChatSDK3A (Avatars) 3.5.0+
- **Dependencias**: NDMF 1.4.0+
- **Localización**: 6 idiomas (es, en, zh, ja, ko, ru)

## Estructura
```
Assets/Bender_Dios/MenuRadial/   → Código fuente
Assets/Bender_Dios/Generated/    → Archivos generados
```

## Assemblies
| Assembly | Propósito |
|----------|-----------|
| `BenderDios.MenuRadial.Runtime` | Componentes principales |
| `BenderDios.MenuRadial.Editor` | Inspectors, plugins NDMF |
| `BenderDios.MenuRadial.Menu` | Generadores VRChat |
| `BenderDios.MenuRadial.Menu.Editor` | Inspectors menú |
| `BenderDios.MenuRadial.Localization` | Sistema de idiomas |

---

## ARQUITECTURA

### Jerarquía de Componentes
```
MRMenuRadial (Orquestador) - Runtime/Components/MenuRadial/
├── MRCoserRopa            - Runtime/Components/CoserRopa/      → Cosido de ropa/pelucas
├── MROrganizaPB           - Runtime/Components/OrganizaPB/     → Organiza PhysBones
├── MRAjustarBounds        - Runtime/Components/AjustarBounds/  → Unifica bounds
├── MRAnalisisColision     - Runtime/Components/AnalisisColision/ → Conflictos MA
├── MRPesoTexturas         - Runtime/Components/PesoTexturas/   → Análisis texturas
└── MRMenuControl          - Components/Menu/                   → Genera archivos VRChat
```

### Flujo de Datos
```
MRMenuRadial
└── MRMenuControl
    ├── MRUnificarObjetos (Radiales) - Runtime/Components/Radial/
    │   └── MRAgruparObjetos (Frames) - Runtime/Components/Frame/
    │       └── ObjectReference, MaterialReference, BlendshapeReference
    │
    ├── MRUnificarMateriales - Runtime/Components/UnifyMaterial/
    │   └── MRAgruparMateriales - Runtime/Components/AlternativeMaterial/
    │       ├── MRMaterialSlot
    │       └── MRMaterialGroup
    │
    └── MRIluminacionRadial - Runtime/Components/Illumination/
```

### Tipos de Animación
| Tipo | Frames | Archivos | Parámetro |
|------|--------|----------|-----------|
| OnOff | 1 | `_on.anim`, `_off.anim` | Bool (1 bit) |
| AB | 2 | `_A.anim`, `_B.anim` | Bool (1 bit) |
| Linear | 3+ | `_lin.anim` (255 frames @ 60fps) | Float (8 bits) |

---

## SISTEMA DE COSIDO (MRCoserRopa)

### Modelos
- **PieceEntry** (`Runtime/Components/CoserRopa/Models/PieceEntry.cs`): Representa una pieza detectada (ropa, peluca o accesorio)
- **PieceType** (`Runtime/Components/CoserRopa/Models/PieceType.cs`): Enum `Ropa`, `Pelo`, `Pieza`
- **StitchZone** (`Runtime/Components/CoserRopa/Models/StitchZone.cs`): Zona de cosido (FullBody, Torso, Head, UpperLimb, LowerLimb, Hip, Chest, RightHand, LeftHand, RightFoot, LeftFoot, None)

### Clasificación de piezas (DeterminePieceType)
Clasificación automática multi-señal en `PieceEntry.DeterminePieceType()`:
- **WigDetector** marca piezas como `isWig` (scoring multi-señal, threshold=7)
- **BoneWeightAnalyzer** verifica si TODOS los meshes pesan en huesos de la cabeza (>60%)
- **MatchesHairPattern()** detecta nombres de pelo (hair, wig, pelo, bangs, etc.)
- **Lógica**: isWig + zona Head/None → Pelo; isWig + zona cuerpo + bone weights/nombre → Pelo; isWig + zona cuerpo sin confirmación → Ropa; BoneProxy→Head + señales pelo → Pelo; zona cuerpo → Ropa; zona Head/None sin señales → Pieza

### Controllers
- **ModularAvatarDetector**: Detecta MA MergeArmature/BoneProxy, `GetMATargetInfo()`, `IsBoneProxyToHead()`
- **BoneStitchingController**: Ejecuta el cosido de huesos
- **HumanoidBoneMapper**: Mapeo de huesos humanoid entre armatures
- **MeshRetargeter**: Retargetea meshes al armature del avatar

### Detección de pelucas
- **WigDetector** (`Runtime/Components/MenuRadial/WigDetector.cs`): 8 señales de scoring, 3 fuentes
- **BoneWeightAnalyzer** (`Runtime/Components/MenuRadial/BoneWeightAnalyzer.cs`): Análisis de vertex bone weights
- **AutoMenuGenerator**: Usa WigDetector independientemente (NO usa PieceType) para separar radiales Outfits/Pelucas
- **PieceType es usado** por MRCoserRopaPlugin.cs durante build NDMF (warning Pelo+DisableMA)

### Base de Datos de Huesos
**Ubicación**: `Runtime/Components/CoserRopa/BoneNames/BoneNameDatabase.cs`
- 230+ patrones de nombres (Blender, MMD, VRM, Unity)
- Normaliza: `ToLowerInvariant().Replace("_","").Replace(".","").Replace(" ","")`
- Huesos IGNORADOS: LeftEye, RightEye, Jaw (rompen expresiones)

---

## REGLAS CRÍTICAS

### Animaciones - NUNCA usar `activeInHierarchy`
```csharp
// CORRECTO: usar IsActive del ObjectReference
float value = objRef.IsActive ? 1f : 0f;

// INCORRECTO - causa bugs de solapamiento
float value = objRef.Target.activeInHierarchy ? 1f : 0f;
```

### Materiales - SIEMPRE usar `sharedMaterials`
```csharp
// CORRECTO: no causa memory leaks
var materials = renderer.sharedMaterials;

// INCORRECTO - causa memory leaks en Edit Mode
var materials = renderer.materials;
```

### VRChat Limits
- **256 bits máximo** para parámetros (Bool=1, Float/Int=8)
- **8 controles máximo** por menú
- **Write Defaults = OFF** obligatorio

### Grupos de Materiales
- Requieren **≥2 materiales** para ser válidos
- Comparar por **asset path**, no por nombre

### MA BoneProxy
- **NO destruir** BoneProxy — es el mecanismo de unión de pelucas/accesorios al avatar
- MR debe convivir con él, no eliminarlo

---

## ARCHIVOS CRÍTICOS

| Archivo | Razón |
|---------|-------|
| `RadialAnimationBuilder.cs` | Lógica de generación de animaciones |
| `MRFXControllerGenerator.cs` | Genera FX Controller |
| `BoneNameDatabase.cs` | 230+ patrones de huesos |
| `MRMenuRadialPlugin.cs` | Plugin NDMF principal |
| `MRConstants.cs` | Constantes globales |
| `ObjectReference.cs` | Base del sistema de referencias |
| `PieceEntry.cs` | Modelo de piezas + clasificación |
| `WigDetector.cs` | Detección de pelucas |
| `BoneWeightAnalyzer.cs` | Análisis de pesos de huesos |

---

## SISTEMA DE REFERENCIAS

Las referencias están en `Runtime/Core/Common/`.

### ObjectReference
```csharp
public GameObject Target;
public bool IsActive;        // Estado DESEADO (no actual)
public string HierarchyPath; // Ruta relativa al avatar
// NOTA: Equals() solo compara por Target, ignorando IsActive (bug conocido)
```

### MaterialReference
```csharp
public Renderer TargetRenderer;
public int MaterialIndex;
public Material AlternativeMaterial;
public Material OriginalMaterial;
// Path: m_Materials.Array.data[{index}]
```

### BlendshapeReference
```csharp
public SkinnedMeshRenderer SkinnedMeshRenderer;
public string BlendshapeName;
public float Value;  // 0-100
// Path: blendShape.{name}
```

---

## SISTEMA DE MATERIALES

### Arquitectura
```
MRUnificarMateriales (genera Linear 255 frames)
└── MRAgruparMateriales (por prenda)
    ├── MRMaterialSlot (Renderer + índice)
    └── MRMaterialGroup (materiales intercambiables, ≥2)
```

### Distribución de Frames
```
4 materiales → 255 / 4 = 63 frames cada uno
Material 0: 0-62, Material 1: 63-125
Material 2: 126-188, Material 3: 189-255 (sobrantes)
```

### Binding de Animación
```csharp
EditorCurveBinding {
    path = "Armature/Body",
    type = typeof(SkinnedMeshRenderer),
    propertyName = "m_Materials.Array.data[0]"
}
// Usa ObjectReferenceKeyframe, NO curvas float
```

---

## PLUGINS NDMF

### Orden de ejecución
```
Resolving.BeforeMA:    MRDisableMAPlugin, MRAnalisisColisionPlugin, MROrganizaPBPlugin
Transforming.BeforeMA: MRCoserRopaPlugin, MRMenuRadialPlugin (merge FX/Parameters/Menu)
  → MA procesa (ShapeChanger detecta toggles de MR)
Transforming.AfterMA:  MRAjustarBoundsPlugin, MRMenuRadialCleanupPass
```

### Control de NDMF
```csharp
// En MRMenuRadial
DisableBoneStitchingNDMF = true;  // Desactiva cosido
DisableVRChatMergeNDMF = true;    // Desactiva merge
```

---

## LOCALIZACIÓN

- **6 idiomas**: es, en, zh, ja, ko, ru
- **Locale files**: `Localization/Resources/Locales/{lang}.json`
- **Patrón**: `using L = ...MRLocalizationKeys; MRLocalization.Get(L.Section.KEY, args)`
- **LocaleSection**: Clase plana en MRLocalization.cs — necesita un campo por cada key del JSON, sin él JsonUtility ignora el valor
- **zh.json**: Usar `「」` en vez de `"` `"` (rompen JSON)

---

## CONVENCIONES

### Namespaces
```
Bender_Dios.MenuRadial.Core.Common          → Utilidades core, referencias
Bender_Dios.MenuRadial.Components.*         → Componentes específicos
Bender_Dios.MenuRadial.Components.Menu      → Sistema de menú VRChat
Bender_Dios.MenuRadial.Shaders              → Sistema de shaders
Bender_Dios.MenuRadial.Localization         → Localización
```

### Nomenclatura
- **Prefijo MR**: Componentes públicos (`MRMenuRadial`, `MRAgruparObjetos`)
- **Sufijo Strategy**: Estrategias de shader
- **Sufijo Generator**: Generadores
- **Sufijo Controller**: Controladores de lógica
- **Constantes**: SCREAMING_SNAKE_CASE (`MRConstants.LINEAR_FRAME_COUNT`)

### Serialización
```csharp
[SerializeField] private VRCAvatarDescriptor _avatar;
public VRCAvatarDescriptor Avatar { get => _avatar; set => _avatar = value; }
```

---

## TROUBLESHOOTING

| Problema | Solución |
|----------|----------|
| Animaciones no funcionan | Usar `IsActive`, NO `activeInHierarchy`. Write Defaults = OFF |
| Ropa no se cose | Verificar `BoneNameDatabase`. LeftEye/RightEye/Jaw se ignoran |
| Peluca clasificada como Ropa | Verificar WigDetector score (≥7) y BoneWeightAnalyzer (>60% head) |
| Materiales no cambian | Slot debe tener grupo vinculado. Grupo necesita ≥2 materiales |
| Preview no se desactiva | `PreviewManager.ClearAll()` (es clase estática, no singleton) |
| Memory leaks | Usar `sharedMaterials`, NUNCA `materials` |
| Localización muestra [key] | Falta campo en LocaleSection de MRLocalization.cs |

---

## GITHUB

Al pedir commit/actualizar:
1. Commit cambios
2. Bump versión en `package.json`
3. Actualizar `CHANGELOG.md`
4. `git tag vX.X.X && git push origin main --tags`
5. `gh release edit` (CI crea el release, usar edit para notas)

**Repositorio**: `emerytheec/MenuRadial` (git root: `Assets/Bender_Dios/MenuRadial/`)
**VPM**: `emerytheec/vpm-listing` (CI actualiza `index.json`)
