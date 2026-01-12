# Manual de Usuario - Sistema MR (Menu Radial) para VRChat

**Versión:** 1.0
**Compatibilidad:** Unity 2022.3.22f1 | VRChat SDK3 Avatars

---

## Tabla de Contenidos

1. [Introduccion](#1-introduccion)
2. [Requisitos Previos](#2-requisitos-previos)
3. [Conceptos Basicos](#3-conceptos-basicos)
4. [Componentes del Sistema](#4-componentes-del-sistema)
   - [MR Frame Object](#41-mr-frame-object)
   - [MR Radial Menu](#42-mr-radial-menu)
   - [MR Radial Illumination](#43-mr-radial-illumination)
   - [MR Unify Material](#44-mr-unify-material)
   - [MR Control Menu](#45-mr-control-menu)
5. [Tutorial Paso a Paso](#5-tutorial-paso-a-paso)
6. [Ejemplos Practicos](#6-ejemplos-practicos)
7. [Preguntas Frecuentes](#7-preguntas-frecuentes)
8. [Solucion de Problemas](#8-solucion-de-problemas)

---

## 1. Introduccion

El **Sistema MR (Menu Radial)** es una herramienta para Unity que te permite crear menus radiales funcionales para avatares de VRChat de forma visual e intuitiva, sin necesidad de configurar manualmente animaciones, controladores FX, parametros o menus de expresiones.

### Que puedes hacer con MR?

- Activar/desactivar objetos (gafas, sombreros, accesorios)
- Cambiar materiales (diferentes colores de ropa, texturas)
- Controlar blendshapes (expresiones faciales, morphs)
- Ajustar iluminacion de materiales lilToon
- Crear menus con hasta 8 opciones (y submenus para mas)

### Como funciona?

```
Tu configuras visualmente  -->  MR genera automaticamente  -->  Subes a VRChat
     (en Unity)                   - Animaciones .anim            (todo listo)
                                  - FX Controller
                                  - Parametros
                                  - Menus
```

---

## 2. Requisitos Previos

Antes de usar MR, asegurate de tener:

- **Unity 2022.3.22f1** (version recomendada por VRChat)
- **VRChat SDK3 - Avatars** instalado
- **Un avatar configurado** con VRC Avatar Descriptor
- (Opcional) **lilToon shader** si quieres usar control de iluminacion

### Verificar que todo esta instalado

1. Abre tu proyecto de Unity
2. Ve a `Window > Package Manager`
3. Verifica que aparezca "VRChat SDK - Avatars"
4. En tu avatar, verifica que tenga el componente "VRC Avatar Descriptor"

---

## 3. Conceptos Basicos

### Que es un "Frame"?

Un **frame** es como una "foto" de como quieres que se vea tu avatar en un momento especifico. Por ejemplo:

- **Frame "Sin Gafas"**: Las gafas estan desactivadas
- **Frame "Con Gafas"**: Las gafas estan activadas

### Tipos de Animacion

El sistema detecta automaticamente que tipo de animacion crear segun cuantos frames configures:

| Frames | Tipo | Control en VRChat | Ejemplo |
|--------|------|-------------------|---------|
| 1 | Toggle ON/OFF | Boton que activa/desactiva | Mostrar/ocultar gafas |
| 2 | Alternancia A/B | Boton que alterna entre dos estados | Gafas normales / Gafas de sol |
| 3+ | Slider Lineal | Dial que va de 0% a 100% | Multiples outfits |

### Jerarquia de Componentes

```
MR Control Menu (Menu Principal)
    |
    +-- Slot 1: MR Radial Menu (Accesorios)
    |       |
    |       +-- MR Frame Object (Sin accesorios)
    |       +-- MR Frame Object (Con gafas)
    |       +-- MR Frame Object (Con sombrero)
    |
    +-- Slot 2: MR Radial Illumination (Control de luz)
    |
    +-- Slot 3: MR Radial Menu (Ropa)
            |
            +-- MR Frame Object (Outfit 1)
            +-- MR Frame Object (Outfit 2)
```

---

## 4. Componentes del Sistema

### 4.1 MR Frame Object

**Que hace:** Captura UN estado especifico de tu avatar (objetos activos, materiales, blendshapes).

**Como agregarlo:**
1. Selecciona un GameObject en tu avatar
2. Click derecho > `Bender Dios > MR Frame Object`
3. O desde el menu: `Component > Bender Dios > MR Frame Object`

**Opciones en el Inspector:**

| Campo | Descripcion |
|-------|-------------|
| **Frame Name** | Nombre para identificar este estado (ej: "Con Gafas") |
| **Auto Update Paths** | Actualiza automaticamente las rutas si mueves objetos |
| **Objects** | Lista de GameObjects y si estan activos o no |
| **Materials** | Lista de cambios de material |
| **Blendshapes** | Lista de valores de blendshapes |

**Como agregar elementos:**

#### Agregar Objetos (GameObjects)
1. Expande la seccion "Objects"
2. Arrastra un GameObject desde la jerarquia al campo
3. Marca/desmarca la casilla para indicar si estara activo o inactivo

#### Agregar Materiales
1. Expande la seccion "Materials"
2. Click en "Add Material Reference"
3. Selecciona el Renderer (objeto con material)
4. Elige el indice del material (si tiene varios)
5. Asigna el material alternativo

#### Agregar Blendshapes
1. Expande la seccion "Blendshapes"
2. Click en "Add Blendshape"
3. Selecciona el SkinnedMeshRenderer (normalmente el Body)
4. Elige el blendshape de la lista
5. Ajusta el valor (0-100)

**Botones utiles:**
- **Preview Frame**: Ver como se ve este estado en la escena
- **Clear Invalid**: Eliminar referencias a objetos que ya no existen
- **Update Paths**: Recalcular rutas jerarquicas

---

### 4.2 MR Radial Menu

**Que hace:** Agrupa varios frames y genera las animaciones automaticamente.

**Como agregarlo:**
1. Selecciona un GameObject (puede ser el mismo que el avatar)
2. `Component > Bender Dios > MR Radial Menu`

**Opciones en el Inspector:**

| Campo | Descripcion |
|-------|-------------|
| **Animation Name** | Nombre del archivo de animacion (ej: "Gafas") |
| **Animation Path** | Carpeta donde guardar las animaciones |
| **Auto Update Paths** | Actualizar rutas automaticamente |
| **Default State Is On** | (Solo para 1 frame) Si por defecto esta activado |
| **Frames** | Lista de MR Frame Objects |

**Como agregar frames:**
1. En la seccion "Frames", click en "+"
2. Arrastra un MR Frame Object desde la jerarquia
3. Repite para cada estado que quieras

**El slider de preview:**
- Cuando tienes 3+ frames, aparece un slider circular
- Arrastralo para ver en tiempo real como cambia entre estados
- Esto NO afecta el avatar final, solo es para previsualizar

**Tipo de animacion generada:**

| Cantidad de Frames | Archivos Generados |
|-------------------|-------------------|
| 1 frame | `NombreAnimacion_on.anim` y `NombreAnimacion_off.anim` |
| 2 frames | `NombreAnimacion_A.anim` y `NombreAnimacion_B.anim` |
| 3+ frames | `NombreAnimacion_lin.anim` (animacion lineal) |

---

### 4.3 MR Radial Illumination

**Que hace:** Controla la iluminacion de materiales lilToon, permitiendo transicion de iluminacion normal a "unlit" (sin sombras).

**Requisitos:** Materiales con shader lilToon

**Como agregarlo:**
1. Selecciona un GameObject
2. `Component > Bender Dios > MR Radial Illumination`

**Opciones en el Inspector:**

| Campo | Descripcion |
|-------|-------------|
| **Root Object** | Objeto raiz desde donde buscar materiales lilToon |
| **Animation Name** | Nombre de la animacion |
| **Animation Path** | Carpeta de salida |
| **As Unlit** | Que tan "sin iluminacion" (0=normal, 1=totalmente unlit) |
| **Light Max Limit** | Limite maximo de luz |
| **Shadow Border** | Dureza del borde de sombras |
| **Shadow Strength** | Intensidad de las sombras |

**Como usarlo:**
1. Asigna el Root Object (normalmente el objeto raiz del avatar)
2. Click en "Scan Materials" para detectar materiales lilToon
3. Ajusta los valores para ver el efecto
4. Usa "Preview" para ver el resultado en tiempo real

**Valores por defecto de la transicion:**

| Posicion Slider | As Unlit | Light Max | Shadow Border | Shadow Strength |
|-----------------|----------|-----------|---------------|-----------------|
| 0% (Normal) | 0 | 0.15 | 1 | 1 |
| 50% (Intermedio) | 0 | 1 | 0.05 | 0.5 |
| 100% (Unlit) | 1 | 1 | 0.05 | 0 |

---

### 4.4 MR Unify Material

**Que hace:** Permite cambiar entre multiples materiales usando un solo slider.

**Como agregarlo:**
1. Selecciona un GameObject
2. `Component > Bender Dios > MR Unify Material`

**Opciones en el Inspector:**

| Campo | Descripcion |
|-------|-------------|
| **Animation Name** | Nombre de la animacion |
| **Animation Path** | Carpeta de salida |
| **Alternative Materials** | Lista de componentes MR Alternative Material |

**Como usarlo:**
1. En los objetos con materiales que quieres cambiar, agrega `MR Alternative Material`
2. En cada `MR Alternative Material`, define los grupos de materiales alternativos
3. En `MR Unify Material`, agrega referencias a esos componentes
4. El slider distribuira automaticamente los materiales

---

### 4.5 MR Control Menu

**Que hace:** Es el componente principal que orquesta todo el sistema. Genera los archivos finales para VRChat.

**Como agregarlo:**
1. Selecciona el GameObject raiz del avatar
2. `Component > Bender Dios > MR Control Menu`

**Opciones en el Inspector:**

| Campo | Descripcion |
|-------|-------------|
| **Menu Name** | Nombre del menu en VRChat |
| **Write Defaults** | Configuracion de Write Defaults para animaciones |
| **Auto Update Paths** | Actualizar rutas automaticamente |
| **Slots** | Los botones/controles del menu (maximo 8) |

**El Menu Circular:**

En el Inspector veras un menu circular visual con:
- **Slots vacios**: Espacios grises disponibles
- **Slots configurados**: Muestran icono y nombre
- **Click en slot**: Previsualiza ese control en la escena

**Como agregar un slot:**
1. Click en "Add Slot" o en un slot vacio del menu circular
2. Configura:
   - **Name**: Nombre que aparecera en VRChat
   - **Icon**: Icono del boton (imagen cuadrada recomendada)
   - **Target**: El componente a controlar (MR Radial Menu, MR Radial Illumination, etc.)

**Generar archivos VRChat:**
1. Verifica que todos los slots esten configurados (sin errores en rojo)
2. Click en **"Create VRChat Files"**
3. Los archivos se generan en `Assets/Bender_Dios/Generated/`:
   - `FX_Menu_Radial.controller` - Controlador de animaciones
   - `Parametro_Menu_Radial.asset` - Parametros de VRChat
   - `Menu_Menu_Radial.asset` - Menu de expresiones

---

## 5. Tutorial Paso a Paso

### Crear un toggle simple (mostrar/ocultar gafas)

**Paso 1: Preparar el objeto**
1. En tu avatar, asegurate de tener las gafas como un GameObject separado
2. Verifica que las gafas esten activas en la escena

**Paso 2: Crear el Frame**
1. Selecciona las gafas en la jerarquia
2. `Component > Bender Dios > MR Frame Object`
3. En "Frame Name" escribe: "Gafas Activas"
4. En la seccion "Objects", las gafas ya deberian aparecer
5. Asegurate de que la casilla este marcada (activo)

**Paso 3: Crear el Radial Menu**
1. Selecciona el GameObject raiz del avatar
2. `Component > Bender Dios > MR Radial Menu`
3. En "Animation Name" escribe: "Gafas"
4. En "Frames", click en "+" y arrastra el MR Frame Object de las gafas

**Paso 4: Configurar el Control Menu**
1. En el avatar, agrega `Component > Bender Dios > MR Control Menu`
2. Click en "Add Slot"
3. En el slot:
   - Name: "Gafas"
   - Icon: (arrastra una imagen de gafas)
   - Target: Arrastra el MR Radial Menu que creaste

**Paso 5: Generar y probar**
1. Click en "Create VRChat Files"
2. Los archivos se generan automaticamente
3. Sube tu avatar a VRChat
4. En el menu radial, tendras un toggle para las gafas

---

### Crear un selector de outfits (3 opciones)

**Paso 1: Preparar los outfits**
- Asegurate de tener los diferentes outfits como GameObjects separados
- Ejemplo: "Outfit_Casual", "Outfit_Formal", "Outfit_Deportivo"

**Paso 2: Crear un Frame por cada outfit**

Para cada outfit:
1. Crea un nuevo GameObject vacio (ej: "Frame_Casual")
2. Agrega `MR Frame Object`
3. Nombra el frame (ej: "Outfit Casual")
4. En "Objects":
   - Agrega "Outfit_Casual" como ACTIVO
   - Agrega "Outfit_Formal" como INACTIVO
   - Agrega "Outfit_Deportivo" como INACTIVO

Repite para los otros outfits, activando solo el correspondiente.

**Paso 3: Crear el Radial Menu**
1. En el avatar, agrega `MR Radial Menu`
2. Animation Name: "Outfits"
3. Agrega los 3 frames en orden

**Paso 4: Agregar al Control Menu**
1. En MR Control Menu, agrega un slot
2. Name: "Outfits"
3. Target: El MR Radial Menu de outfits

**Resultado:** En VRChat tendras un slider que va de 0% a 100%, pasando por los 3 outfits.

---

## 6. Ejemplos Practicos

### Ejemplo 1: Toggle de orejas de gato

```
MR Frame Object "Orejas"
    Objects:
        - CatEars (activo)
```

```
MR Radial Menu
    Animation Name: "CatEars"
    Frames: [MR Frame Object "Orejas"]
```

**Resultado:** Boton ON/OFF para las orejas

---

### Ejemplo 2: Cambio de color de ojos

```
MR Frame Object "Ojos Azules"
    Materials:
        - Eye_Renderer [0] -> Material_Ojos_Azules

MR Frame Object "Ojos Verdes"
    Materials:
        - Eye_Renderer [0] -> Material_Ojos_Verdes

MR Frame Object "Ojos Rojos"
    Materials:
        - Eye_Renderer [0] -> Material_Ojos_Rojos
```

```
MR Radial Menu
    Animation Name: "EyeColor"
    Frames: [Ojos Azules, Ojos Verdes, Ojos Rojos]
```

**Resultado:** Slider para cambiar entre 3 colores de ojos

---

### Ejemplo 3: Expresion facial con blendshapes

```
MR Frame Object "Expresion Normal"
    Blendshapes:
        - Body: "Smile" = 0
        - Body: "Angry" = 0

MR Frame Object "Expresion Feliz"
    Blendshapes:
        - Body: "Smile" = 100
        - Body: "Angry" = 0
```

```
MR Radial Menu
    Animation Name: "Expression"
    Frames: [Normal, Feliz]
```

**Resultado:** Boton para alternar entre expresion normal y feliz

---

## 7. Preguntas Frecuentes

### Los componentes MR se suben con el avatar?

**No.** Los componentes MR implementan `IEditorOnly`, lo que significa que VRChat SDK los elimina automaticamente al subir. Solo se suben los archivos generados (animaciones, controllers, etc.).

### Puedo tener mas de 8 opciones en el menu?

**Si.** Usa submenus. Puedes anidar MR Control Menu dentro de otros para crear jerarquias de menus.

### Que pasa si muevo o renombro un objeto?

Si tienes **Auto Update Paths** activado, las rutas se actualizan automaticamente. Si no, usa el boton "Update Paths" para recalcular.

### Puedo mezclar diferentes tipos de controles?

**Si.** Cada slot del MR Control Menu puede tener un tipo diferente:
- Slot 1: MR Radial Menu (toggle de accesorios)
- Slot 2: MR Radial Illumination (control de luz)
- Slot 3: MR Radial Menu (slider de outfits)

### El preview afecta mi avatar final?

**No.** El sistema de preview es no destructivo. Cuando cierras el Inspector o generas los archivos, todo vuelve a su estado original.

### Que es "Write Defaults"?

Es una configuracion de Unity para animaciones. Si no sabes que es, dejalo en el valor por defecto. Si tu avatar tiene problemas de animacion, prueba cambiarlo.

---

## 8. Solucion de Problemas

### "No hay frames configurados"

**Causa:** El MR Radial Menu no tiene frames asignados.
**Solucion:** Agrega al menos un MR Frame Object a la lista de frames.

### "Referencia invalida" (texto rojo)

**Causa:** Un objeto, material o blendshape ya no existe.
**Solucion:** Click en "Clear Invalid References" para limpiar las referencias rotas.

### El preview no muestra cambios

**Causas posibles:**
1. El objeto esta en otra escena
2. El objeto fue eliminado
3. Hay un error en la configuracion

**Solucion:** Verifica que todos los objetos existen y estan en la escena activa.

### Las animaciones no se generan

**Causas posibles:**
1. El nombre de animacion esta vacio
2. La ruta de salida no existe
3. Hay errores de validacion

**Solucion:** Revisa los mensajes de error en el Inspector y corrige los problemas indicados.

### El menu no aparece en VRChat

**Causas posibles:**
1. Los archivos no se generaron correctamente
2. El avatar no tiene los archivos asignados
3. Hubo un error al subir

**Solucion:**
1. Regenera los archivos con "Create VRChat Files"
2. Verifica que el VRC Avatar Descriptor tenga asignados los archivos generados
3. Revisa la consola de Unity por errores

### Los materiales no cambian correctamente

**Causa:** El material original no se capturo correctamente.
**Solucion:** En el MR Frame Object, click en "Update Original Materials" para recapturar los materiales actuales.

---

## Contacto y Soporte

Si encuentras bugs o tienes sugerencias, puedes reportarlos en el repositorio del proyecto.

---

*Manual creado para el Sistema MR (Menu Radial) - Desarrollado por Bender_Dios*
