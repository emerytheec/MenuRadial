#!/bin/bash
# generate-booth-description.sh
# Genera booth-description.txt con la descripcion del producto en 6 idiomas
# Uso: bash tools/generate-booth-description.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PACKAGE_JSON="$REPO_ROOT/package.json"
CHANGELOG="$REPO_ROOT/CHANGELOG.md"
OUTPUT="$REPO_ROOT/booth-description.txt"

# --- Extraer version de package.json ---
VERSION=$(grep '"version"' "$PACKAGE_JSON" | head -1 | sed 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
if [ -z "$VERSION" ]; then
    echo "ERROR: No se pudo extraer la version de $PACKAGE_JSON"
    exit 1
fi
echo "Version: $VERSION"

# --- Extraer ultimo changelog ---
# Captura todo entre el primer ## [x.y.z] y el siguiente ## [x.y.z]
LATEST_CHANGELOG=$(awk '
    /^## \[/ {
        if (found) exit
        found = 1
        next
    }
    found { print }
' "$CHANGELOG" | sed -e '/^$/N;/^\n$/d' -e 's/[[:space:]]*$//')
# Quitar lineas vacias al inicio y final
LATEST_CHANGELOG=$(echo "$LATEST_CHANGELOG" | sed '/./,$!d' | sed -e :a -e '/^\n*$/{$d;N;ba' -e '}')

if [ -z "$LATEST_CHANGELOG" ]; then
    echo "WARN: No se encontro changelog. Se usara placeholder."
    LATEST_CHANGELOG="Sin cambios documentados."
fi

echo "Changelog extraido ($(echo "$LATEST_CHANGELOG" | wc -l) lineas)"

# --- Links ---
LINK_VPM="https://emerytheec.github.io/vpm-listing/"
LINK_GITHUB="https://github.com/emerytheec/MenuRadial"
LINK_KOFI="https://ko-fi.com/bender_dios"

# --- Generar booth-description.txt ---
cat > "$OUTPUT" << 'HEADER'
🇯🇵 日本語 / 🇺🇸 English / 🇪🇸 Español / 🇨🇳 中文 / 🇰🇷 한국어 / 🇷🇺 Русский
HEADER

# =====================================================================
# JAPONES
# =====================================================================
cat >> "$OUTPUT" << LANG_JP

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🇯🇵 日本語
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}

VRChatアバター用の自動ラジアルメニュー生成システムです。
衣装や髪型の切り替えメニューをワンクリックで作成します。

✨ 特徴
• アバターに衣装を追加するだけで自動的にラジアルメニューを生成
• 衣装のオン/オフ、A/B切り替え、リニアスライダーに対応
• 代替マテリアルの切り替え（カラーバリエーション）
• ブレンドシェイプ制御
• lilToonシェーダーの照明コントロール
• PhysBoneの自動整理
• Boundsの自動統一
• Modular Avatar完全対応（非破壊的）
• NDMFによる非破壊的なビルドパイプライン

📋 要件
• Unity 2022.3.22f1
• VRChat Avatars SDK 3.5.0+
• NDMF 1.4.0+

📝 最新の変更 (v${VERSION})
${LATEST_CHANGELOG}

🔗 リンク
• VPM: ${LINK_VPM}
• GitHub: ${LINK_GITHUB}
• Ko-fi: ${LINK_KOFI}
LANG_JP

# =====================================================================
# ENGLISH
# =====================================================================
cat >> "$OUTPUT" << LANG_EN

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🇺🇸 English
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}

Automated radial menu generation system for VRChat avatars.
Creates outfit and hairstyle toggle menus with one click.

✨ Features
• Automatically generates radial menus just by adding outfits to your avatar
• Supports On/Off toggles, A/B switches, and Linear sliders
• Alternative material switching (color variations)
• Blendshape control
• lilToon shader lighting control
• Automatic PhysBone organization
• Automatic Bounds unification
• Full Modular Avatar support (non-destructive)
• Non-destructive build pipeline via NDMF

📋 Requirements
• Unity 2022.3.22f1
• VRChat Avatars SDK 3.5.0+
• NDMF 1.4.0+

📝 Latest changes (v${VERSION})
${LATEST_CHANGELOG}

🔗 Links
• VPM: ${LINK_VPM}
• GitHub: ${LINK_GITHUB}
• Ko-fi: ${LINK_KOFI}
LANG_EN

# =====================================================================
# ESPANOL
# =====================================================================
cat >> "$OUTPUT" << LANG_ES

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🇪🇸 Español
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}

Sistema automatizado de generacion de menus radiales para avatares VRChat.
Crea menus de alternado de ropa y peinados con un solo clic.

✨ Caracteristicas
• Genera automaticamente menus radiales con solo agregar ropa al avatar
• Soporta toggles On/Off, switches A/B y sliders Lineales
• Cambio de materiales alternativos (variaciones de color)
• Control de blendshapes
• Control de iluminacion de shaders lilToon
• Organizacion automatica de PhysBones
• Unificacion automatica de Bounds
• Compatibilidad total con Modular Avatar (no destructivo)
• Pipeline de build no destructivo via NDMF

📋 Requisitos
• Unity 2022.3.22f1
• VRChat Avatars SDK 3.5.0+
• NDMF 1.4.0+

📝 Ultimos cambios (v${VERSION})
${LATEST_CHANGELOG}

🔗 Enlaces
• VPM: ${LINK_VPM}
• GitHub: ${LINK_GITHUB}
• Ko-fi: ${LINK_KOFI}
LANG_ES

# =====================================================================
# CHINO
# =====================================================================
cat >> "$OUTPUT" << LANG_ZH

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🇨🇳 中文
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}

VRChat虚拟形象的自动径向菜单生成系统。
一键创建服装和发型切换菜单。

✨ 功能
• 只需将服装添加到虚拟形象即可自动生成径向菜单
• 支持开/关切换、A/B切换和线性滑块
• 替代材质切换（颜色变体）
• 形态键控制
• lilToon着色器照明控制
• PhysBone自动整理
• Bounds自动统一
• 完全兼容Modular Avatar（非破坏性）
• 通过NDMF实现非破坏性构建管线

📋 要求
• Unity 2022.3.22f1
• VRChat Avatars SDK 3.5.0+
• NDMF 1.4.0+

📝 最新更改 (v${VERSION})
${LATEST_CHANGELOG}

🔗 链接
• VPM: ${LINK_VPM}
• GitHub: ${LINK_GITHUB}
• Ko-fi: ${LINK_KOFI}
LANG_ZH

# =====================================================================
# COREANO
# =====================================================================
cat >> "$OUTPUT" << LANG_KO

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🇰🇷 한국어
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}

VRChat 아바타용 자동 래디얼 메뉴 생성 시스템입니다.
의상 및 헤어스타일 전환 메뉴를 원클릭으로 생성합니다.

✨ 기능
• 아바타에 의상을 추가하기만 하면 자동으로 래디얼 메뉴 생성
• 온/오프 토글, A/B 전환, 리니어 슬라이더 지원
• 대체 머티리얼 전환 (색상 변형)
• 블렌드셰이프 제어
• lilToon 셰이더 조명 제어
• PhysBone 자동 정리
• Bounds 자동 통합
• Modular Avatar 완벽 호환 (비파괴적)
• NDMF를 통한 비파괴적 빌드 파이프라인

📋 요구사항
• Unity 2022.3.22f1
• VRChat Avatars SDK 3.5.0+
• NDMF 1.4.0+

📝 최신 변경사항 (v${VERSION})
${LATEST_CHANGELOG}

🔗 링크
• VPM: ${LINK_VPM}
• GitHub: ${LINK_GITHUB}
• Ko-fi: ${LINK_KOFI}
LANG_KO

# =====================================================================
# RUSO
# =====================================================================
cat >> "$OUTPUT" << LANG_RU

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🇷🇺 Русский
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Menu Radial (MR) v${VERSION}

Автоматическая система генерации радиальных меню для аватаров VRChat.
Создает меню переключения нарядов и причесок одним кликом.

✨ Возможности
• Автоматически генерирует радиальные меню при добавлении одежды к аватару
• Поддержка переключателей Вкл/Выкл, A/B и линейных слайдеров
• Переключение альтернативных материалов (цветовые вариации)
• Управление блендшейпами
• Управление освещением шейдера lilToon
• Автоматическая организация PhysBone
• Автоматическое объединение Bounds
• Полная совместимость с Modular Avatar (неразрушающий)
• Неразрушающий конвейер сборки через NDMF

📋 Требования
• Unity 2022.3.22f1
• VRChat Avatars SDK 3.5.0+
• NDMF 1.4.0+

📝 Последние изменения (v${VERSION})
${LATEST_CHANGELOG}

🔗 Ссылки
• VPM: ${LINK_VPM}
• GitHub: ${LINK_GITHUB}
• Ko-fi: ${LINK_KOFI}
LANG_RU

echo ""
echo "Generado: $OUTPUT"
echo "Idiomas: JP, EN, ES, ZH, KO, RU"
echo "Version: v$VERSION"
