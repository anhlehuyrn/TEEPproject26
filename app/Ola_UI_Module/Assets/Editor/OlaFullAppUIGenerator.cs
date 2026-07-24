using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OlaFullAppUIGenerator
{
    private static readonly Color ArchivalWhite = Hex("#fdf8f8");
    private static readonly Color DeepCharcoal = Hex("#111111");
    private static readonly Color Gold = Hex("#735c00");
    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);

    [MenuItem("Ola/Generate Full App UI")]
    public static void GenerateFullAppUI()
    {
        GameObject canvasObject = CreateUIObject("Ola Refined Canvas", null);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchToParent(canvasRect);

        GameObject homePanel = CreatePanel("Home_Panel", canvasRect, ArchivalWhite);
        GameObject explorePanel = CreatePanel("Explore_Panel", canvasRect, Transparent);
        GameObject passportPanel = CreatePanel("Passport_Panel", canvasRect, ArchivalWhite);
        homePanel.SetActive(true);
        explorePanel.SetActive(false);
        passportPanel.SetActive(false);

        AddHomeContent(homePanel.GetComponent<RectTransform>());
        AddExploreContent(explorePanel.GetComponent<RectTransform>());
        CreateFloatingBottomNav(canvasRect);
        EnsureEventSystem();

        Selection.activeGameObject = canvasObject;
        Undo.RegisterCreatedObjectUndo(canvasObject, "Generate Ola Refined UI");
        EditorUtility.SetDirty(canvasObject);
    }

    private static void AddHomeContent(RectTransform parent)
    {
        Component title = CreateText("Title", parent, "Three Worlds, One Journey", 48f, DeepCharcoal, TextAnchor.MiddleCenter);

        RectTransform titleRect = title.transform as RectTransform;
        titleRect.anchorMin = new Vector2(0.08f, 0.72f);
        titleRect.anchorMax = new Vector2(0.92f, 0.86f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    private static void AddExploreContent(RectTransform parent)
    {
        Button askAIButton = CreateButton("Btn_AskAI", parent, "Ask AI", DeepCharcoal, Color.white);

        RectTransform buttonRect = askAIButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-64f, 172f);
        buttonRect.sizeDelta = new Vector2(220f, 72f);

        Image image = askAIButton.GetComponent<Image>();
        image.type = Image.Type.Sliced;
    }

    private static void CreateFloatingBottomNav(RectTransform canvasRect)
    {
        GameObject navObject = CreateUIObject("FloatingBottomNav", canvasRect);
        Image navImage = navObject.AddComponent<Image>();
        navImage.color = Color.white;
        navImage.type = Image.Type.Sliced;

        Shadow shadow = navObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.14f);
        shadow.effectDistance = new Vector2(0f, -6f);
        shadow.useGraphicAlpha = true;

        HorizontalLayoutGroup layout = navObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 8f;

        RectTransform navRect = navObject.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0.5f, 0f);
        navRect.anchorMax = new Vector2(0.5f, 0f);
        navRect.pivot = new Vector2(0.5f, 0f);
        navRect.anchoredPosition = new Vector2(0f, 100f);
        navRect.sizeDelta = new Vector2(760f, 112f);

        CreateNavButton("Btn_Home", navRect, "Home", true);
        CreateNavButton("Btn_Explore", navRect, "Explore", false);
        CreateNavButton("Btn_Passport", navRect, "Passport", false);
    }

    private static Button CreateNavButton(string name, RectTransform parent, string label, bool active)
    {
        Button button = CreateButton(name, parent, label, Transparent, active ? Gold : DeepCharcoal);
        button.targetGraphic.color = Transparent;

        LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 1f;
        layoutElement.minHeight = 72f;

        ColorBlock colors = button.colors;
        colors.normalColor = Transparent;
        colors.highlightedColor = new Color(0.45f, 0.36f, 0f, 0.08f);
        colors.pressedColor = new Color(0.45f, 0.36f, 0f, 0.16f);
        colors.selectedColor = new Color(0.45f, 0.36f, 0f, 0.10f);
        colors.disabledColor = new Color(0f, 0f, 0f, 0.05f);
        button.colors = colors;

        return button;
    }

    private static GameObject CreatePanel(string name, RectTransform parent, Color color)
    {
        GameObject panel = CreateUIObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = color.a > 0f;
        StretchToParent(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static Button CreateButton(string name, RectTransform parent, string label, Color backgroundColor, Color textColor)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = backgroundColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Component text = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, 28f, textColor, TextAnchor.MiddleCenter);
        StretchToParent(text.transform as RectTransform);

        return button;
    }

    private static Component CreateText(string name, RectTransform parent, string value, float fontSize, Color color, TextAnchor fallbackAlignment)
    {
        GameObject textObject = CreateUIObject(name, parent);

        // Prefer TextMeshProUGUI when the package is present; fall back to Unity UI Text for clean compilation.
        Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            Component tmpText = textObject.AddComponent(tmpType);
            SetProperty(tmpText, "text", value);
            SetProperty(tmpText, "fontSize", fontSize);
            SetProperty(tmpText, "color", color);
            SetProperty(tmpText, "raycastTarget", false);
            SetProperty(tmpText, "enableAutoSizing", false);

            Type alignmentType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
            if (alignmentType != null)
            {
                SetProperty(tmpText, "alignment", Enum.Parse(alignmentType, "Center"));
            }

            return tmpText;
        }

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.fontSize = Mathf.RoundToInt(fontSize);
        text.color = color;
        text.alignment = fallbackAlignment;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.raycastTarget = false;
        return text;
    }

    private static void SetProperty(Component component, string propertyName, object value)
    {
        component.GetType().GetProperty(propertyName)?.SetValue(component, value, null);
    }

    private static GameObject CreateUIObject(string name, RectTransform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static Color Hex(string value)
    {
        if (ColorUtility.TryParseHtmlString(value, out Color color))
        {
            return color;
        }

        return Color.magenta;
    }
}
