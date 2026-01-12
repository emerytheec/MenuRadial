using UnityEditor;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Editor
{
    /// <summary>
    /// Ventana del editor para slider radial de puppet float
    /// Herramienta de desarrollo para testing de valores radiales
    /// </summary>
    public class RadialSliderWindow : EditorWindow
{
    private float angle = 0f; // Comenzar desde arriba (0 grados = 12 en punto)
    private float value = 0f;
    private Texture2D bgTexture;
    private GUIStyle percentageStyle;

    [MenuItem("Tools/Radial Puppet Float")]
    public static void ShowWindow()
    {
        var window = GetWindow<RadialSliderWindow>();
        window.titleContent = new GUIContent("Radial Puppet");
        window.minSize = new Vector2(300, 300);
        window.Init();
    }

    private void Init()
    {
        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, new Color(0.15f, 0.25f, 0.25f)); // Color de fondo más teal
        bgTexture.Apply();

        percentageStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24, // Fuente más grande para el porcentaje
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, position.width, position.height), bgTexture);

        GUILayout.Label("Radial Slider", EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetRect(220, 220, GUILayout.ExpandWidth(false));
        Vector2 center = rect.center;
        float radius = 100f;

        Event e = Event.current;
        Vector2 mouse = e.mousePosition;

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            if (Vector2.Distance(mouse, center) <= radius)
            {
                Vector2 dir = mouse - center;
                float rawAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                // Ajustar para que 0% esté arriba (12 en punto) y crezca en sentido horario
                angle = rawAngle + 90f;
                if (angle < 0) angle += 360f;
                if (angle >= 360f) angle -= 360f;
                
                // Convertir a valor 0-1, donde 0 es arriba y crece en sentido horario
                value = angle / 360f;
                Repaint();
            }
        }

        Handles.BeginGUI();

        // Sombra del disco exterior
        Handles.color = new Color(0f, 0.6f, 0.6f, 0.1f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius + 3);

        // Anillo exterior (fondo del slider)
        Handles.color = new Color(0.2f, 0.35f, 0.35f, 0.8f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        // Sector activo (progreso del slider)
        Handles.color = new Color(0f, 0.8f, 0.8f, 0.9f); // Color teal más vibrante
        
        if (angle > 0)
        {
            // Crear el sector como un polígono suave
            int segments = Mathf.Max(8, Mathf.RoundToInt(angle / 3f)); // Más segmentos para suavidad
            Vector3[] sectorPoints = new Vector3[segments + 2];
            
            // Primer punto: centro
            sectorPoints[0] = center;
            
            // Segundo punto: inicio del arco (arriba)
            float startRadians = -90f * Mathf.Deg2Rad;
            sectorPoints[1] = center + new Vector2(Mathf.Cos(startRadians), Mathf.Sin(startRadians)) * radius;
            
            // Puntos del arco desde arriba hasta el cursor
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                float currentAngle = t * angle;
                float currentRadians = (currentAngle - 90f) * Mathf.Deg2Rad;
                sectorPoints[i + 2] = center + new Vector2(Mathf.Cos(currentRadians), Mathf.Sin(currentRadians)) * radius;
            }
            
            // Dibujar como un polígono sólido
            Handles.DrawAAConvexPolygon(sectorPoints);
        }

        // Círculo interior más pequeño
        float innerRadius = radius * 0.4f; // 40% del radio exterior
        Handles.color = new Color(0.25f, 0.4f, 0.45f, 1f); // Color más oscuro para el centro
        Handles.DrawSolidDisc(center, Vector3.forward, innerRadius);

        // Borde del círculo interior
        Handles.color = new Color(0f, 0.6f, 0.6f, 0.6f);
        Handles.DrawWireDisc(center, Vector3.forward, innerRadius);

        // Borde exterior
        Handles.color = new Color(0f, 0.8f, 0.8f, 1f);
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        // Cursor (punto en el borde)
        float rad = (angle - 90f) * Mathf.Deg2Rad; // Ajustar para que comience desde arriba
        Vector2 handlePos = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        Handles.color = new Color(0f, 1f, 1f, 1f); // Cursor más brillante
        Handles.DrawSolidDisc(handlePos, Vector3.forward, 6);
        
        // Borde del cursor
        Handles.color = Color.white;
        Handles.DrawWireDisc(handlePos, Vector3.forward, 6);

        Handles.EndGUI();

        // Porcentaje en el centro
        string percentageText = (value * 100f).ToString("F0") + "%";
        Vector2 textSize = percentageStyle.CalcSize(new GUIContent(percentageText));
        Rect textRect = new Rect(center.x - textSize.x/2, center.y - textSize.y/2, textSize.x, textSize.y);
        GUI.Label(textRect, percentageText, percentageStyle);
    }

    }
}
