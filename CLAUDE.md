# CLAUDE.md

Sistema **MR (Menu Radial)** para avatares VRChat en Unity 2022.3.22f1. Genera animaciones, controllers FX, parámetros y menús VRChat de forma no-destructiva usando NDMF.

## Desarrollo
- **Proyecto Unity** - Sin CLI. Compilación automática al guardar.
- **Tests**: Window → General → Test Runner
- **Unity Version**: 2022.3.22f1
- **VRChat SDK**: VRChatSDK3A (Avatars)

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
├── MRCoserRopa            - Runtime/Components/CoserRopa/      → Cosido de ropa
├── MROrganizaPB           - Runtime/Components/OrganizaPB/     → Organiza PhysBones
├── MRMenuControl          - Components/Menu/                   → Genera archivos VRChat
└── MRAjustarBounds        - Runtime/Components/AjustarBounds/  → Unifica bounds
```

### Flujo de Datos
```
MRMenuRadial
└── MRMenuControl
    ├── MRUnificarObjetos (Radiales) - Runtime/Components/Radial/
    │   └── MRAgruparObjetos (Frames) - Runtime/Components/Frame/
    │       └── ObjectReference, MaterialReference, BlendshapeReference
    │
    └── MRUnificarMateriales - Runtime/Components/UnifyMaterial/
        └── MRAgruparMateriales - Runtime/Components/AlternativeMaterial/
            ├── MRMaterialSlot
            └── MRMaterialGroup
```

### Tipos de Animación
| Tipo | Frames | Archivos | Parámetro |
|------|--------|----------|-----------|
| OnOff | 1 | `_on.anim`, `_off.anim` | Bool (1 bit) |
| AB | 2 | `_A.anim`, `_B.anim` | Bool (1 bit) |
| Linear | 3+ | `_lin.anim` (255 frames @ 60fps) | Float (8 bits) |

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

---

## SISTEMA DE REFERENCIAS

### ObjectReference
```csharp
public GameObject Target;
public bool IsActive;        // Estado DESEADO (no actual)
public string HierarchyPath; // Ruta relativa al avatar
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
    └── MRMaterialGroup (materiales intercambiables)
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

| Plugin | Propósito |
|--------|-----------|
| `MRMenuRadialPlugin` | Merge FX/Parameters/Menu |
| `MRCoserRopaPlugin` | Cosido de armatures |
| `MROrganizaPBPlugin` | Reorganización PhysBones |
| `MRAjustarBoundsPlugin` | Ajuste de bounds |

### Control de NDMF
```csharp
// En MRMenuRadial
DisableBoneStitchingNDMF = true;  // Desactiva cosido
DisableVRChatMergeNDMF = true;    // Desactiva merge
```

---

## CONVENCIONES

### Namespaces
```
Bender_Dios.MenuRadial.Runtime
Bender_Dios.MenuRadial.Editor
Bender_Dios.MenuRadial.Menu
Bender_Dios.MenuRadial.Menu.Generators
```

### Nomenclatura
- **Prefijo MR**: Componentes públicos
- **Sufijo Strategy**: Estrategias de shader
- **Sufijo Generator**: Generadores
- **Sufijo Controller**: Controladores

### Serialización
```csharp
[SerializeField] private VRCAvatarDescriptor avatar;
public VRCAvatarDescriptor Avatar { get => avatar; set => avatar = value; }
```

---

## TROUBLESHOOTING

| Problema | Solución |
|----------|----------|
| Animaciones no funcionan | Usar `IsActive`, NO `activeInHierarchy`. Write Defaults = OFF |
| Ropa no se cose | Verificar `BoneNameDatabase`. LeftEye/RightEye/Jaw se ignoran |
| Materiales no cambian | Slot debe tener grupo vinculado. Grupo necesita ≥2 materiales |
| Preview no se desactiva | `PreviewManager.Instance.ResetAllPreviews()` |
| Memory leaks | Usar `sharedMaterials`, NUNCA `materials` |

---

## GITHUB

Al pedir commit/actualizar:
1. Commit cambios
2. Bump versión en `package.json`
3. Actualizar `CHANGELOG.md`
4. `git tag vX.X.X && git push origin main --tags`
5. `gh release create`

**Repositorio**: `Assets/Bender_Dios/MenuRadial/`
