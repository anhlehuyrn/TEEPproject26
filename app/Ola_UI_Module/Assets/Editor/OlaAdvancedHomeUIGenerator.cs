using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Generates the advanced responsive Home screen for the Ola Refined cultural heritage app.
/// Requires Unity UI and TextMeshPro packages.
/// </summary>
public static class OlaAdvancedHomeUIGenerator
{
    private static readonly Color Background = Hex("#FDFBF7");
    private static readonly Color PrimaryText = Hex("#111111");
    private static readonly Color SecondaryText = Hex("#444748");
    private static readonly Color CardBackground = Hex("#F7F3EC");
    private static readonly Color CardBorder = Hex("#E5E1DA");
    private static readonly Color White = Color.white;

    [MenuItem("Ola/Generate Advanced Home UI")]
    public static void GenerateAdvancedHomeUI()
    {
        Canvas canvas = GetOrCreateActiveCanvas();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        GameObject existingHome = canvasRect.Find("Home_Panel")?.gameObject;
        if (existingHome != null)
        {
            Undo.DestroyObjectImmediate(existingHome);
        }

        GameObject homePanel = CreateUIObject("Home_Panel", canvasRect);
        Image homeBackground = homePanel.AddComponent<Image>();
        homeBackground.color = Background;
        Stretch(homePanel.GetComponent<RectTransform>());

        VerticalLayoutGroup homeLayout = homePanel.AddComponent<VerticalLayoutGroup>();
        homeLayout.padding = new RectOffset(20, 20, 20, 20);
        homeLayout.spacing = 24f;
        homeLayout.childAlignment = TextAnchor.UpperCenter;
        homeLayout.childControlWidth = true;
        homeLayout.childControlHeight = true;
        homeLayout.childForceExpandWidth = true;
        homeLayout.childForceExpandHeight = false;

        CreateTopHeader(homePanel.transform);
        CreateCulturalCarousel(homePanel.transform);
        CreateNarrativeSection(homePanel.transform);
        EnsureEventSystem();

        Undo.RegisterCreatedObjectUndo(homePanel, "Generate Advanced Ola Home UI");
        Selection.activeGameObject = homePanel;
        EditorUtility.SetDirty(canvas.gameObject);
    }

    private static void CreateTopHeader(Transform parent)
    {
        GameObject header = CreateUIObject("Top_Header", parent);
        LayoutElement headerLayoutElement = header.AddComponent<LayoutElement>();
        headerLayoutElement.minHeight = 80f;
        headerLayoutElement.preferredHeight = 80f;

        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 16f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        TMP_Dropdown languageDropdown = CreateDropdown("Dropdown_Language", header.transform, new[] { "EN", "VI", "ZH" });
        SetPreferredSize(languageDropdown.gameObject, 150f, 56f);

        GameObject logoObject = CreateUIObject("Logo_Image", header.transform);
        Image logoImage = logoObject.AddComponent<Image>();
        logoImage.preserveAspect = true;
        logoImage.color = White;
        logoImage.raycastTarget = false;

        LayoutElement logoLayout = logoObject.AddComponent<LayoutElement>();
        logoLayout.flexibleWidth = 1f;
        logoLayout.preferredHeight = 64f;

        TMP_Dropdown locationDropdown = CreateDropdown("Dropdown_Location", header.transform, new[] { "All", "Taiwan", "Kerala", "Bac Ninh" });
        SetPreferredSize(locationDropdown.gameObject, 220f, 56f);
    }

    private static void CreateCulturalCarousel(Transform parent)
    {
        GameObject scrollObject = CreateUIObject("Cultural_Carousel", parent);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.minHeight = 480f;
        scrollLayout.preferredHeight = 480f;
        scrollLayout.flexibleWidth = 1f;

        Image scrollBackground = scrollObject.AddComponent<Image>();
        scrollBackground.color = new Color(1f, 1f, 1f, 0f);
        scrollBackground.raycastTarget = true;

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.16f;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 24f;

        GameObject viewport = CreateUIObject("Viewport", scrollObject.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        viewportImage.raycastTarget = true;
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        Stretch(viewport.GetComponent<RectTransform>());

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 450f);

        HorizontalLayoutGroup contentLayout = content.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 24f;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        CreateCultureCard(content.transform, "Card_Kerala", "Kerala", "God's Own Country. A serene network of backwaters.", "Card_Photo_Kerala");
        CreateCultureCard(content.transform, "Card_Taiwan", "Taiwan", "Living Traditions in Taichung and dramatic gorges.", "Card_Photo_Taiwan");
        CreateCultureCard(content.transform, "Card_BacNinh", "Bắc Ninh, Vietnam", "The cradle of Quan Họ folk songs and Dong Ho paintings.", "Card_Photo_BacNinh");

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
    }

    private static void CreateCultureCard(Transform parent, string name, string title, string description, string photoName)
    {
        GameObject card = CreateUIObject(name, parent);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = CardBackground;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = CardBorder;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.minWidth = 320f;
        cardLayout.preferredWidth = 320f;
        cardLayout.minHeight = 450f;
        cardLayout.preferredHeight = 450f;

        VerticalLayoutGroup cardVerticalLayout = card.AddComponent<VerticalLayoutGroup>();
        cardVerticalLayout.spacing = 0f;
        cardVerticalLayout.padding = new RectOffset(0, 0, 0, 0);
        cardVerticalLayout.childControlWidth = true;
        cardVerticalLayout.childControlHeight = true;
        cardVerticalLayout.childForceExpandWidth = true;
        cardVerticalLayout.childForceExpandHeight = false;

        GameObject photo = CreateUIObject(photoName, card.transform);
        Image photoImage = photo.AddComponent<Image>();
        photoImage.color = Color.white;
        photoImage.preserveAspect = true;
        photoImage.raycastTarget = false;
        LayoutElement photoLayout = photo.AddComponent<LayoutElement>();
        photoLayout.minHeight = 250f;
        photoLayout.preferredHeight = 250f;

        GameObject copyPanel = CreateUIObject("Card_Copy", card.transform);
        Image copyBackground = copyPanel.AddComponent<Image>();
        copyBackground.color = CardBackground;
        copyBackground.raycastTarget = false;

        LayoutElement copyLayoutElement = copyPanel.AddComponent<LayoutElement>();
        copyLayoutElement.minHeight = 200f;
        copyLayoutElement.preferredHeight = 200f;

        VerticalLayoutGroup copyLayout = copyPanel.AddComponent<VerticalLayoutGroup>();
        copyLayout.padding = new RectOffset(20, 20, 20, 20);
        copyLayout.spacing = 12f;
        copyLayout.childAlignment = TextAnchor.UpperCenter;
        copyLayout.childControlWidth = true;
        copyLayout.childControlHeight = true;
        copyLayout.childForceExpandWidth = true;
        copyLayout.childForceExpandHeight = false;

        TextMeshProUGUI titleText = CreateTMPText("Title", copyPanel.transform, title, 24f, PrimaryText, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        SetPreferredSize(titleText.gameObject, 0f, 34f);

        TextMeshProUGUI descText = CreateTMPText("Description", copyPanel.transform, description, 16f, SecondaryText, TextAlignmentOptions.Center);
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Ellipsis;
        SetPreferredSize(descText.gameObject, 0f, 88f);
    }

    private static void CreateNarrativeSection(Transform parent)
    {
        GameObject section = CreateUIObject("Narrative_Section", parent);
        LayoutElement sectionLayoutElement = section.AddComponent<LayoutElement>();
        sectionLayoutElement.flexibleWidth = 1f;
        sectionLayoutElement.preferredHeight = 270f;

        VerticalLayoutGroup sectionLayout = section.AddComponent<VerticalLayoutGroup>();
        sectionLayout.spacing = 16f;
        sectionLayout.padding = new RectOffset(40, 40, 16, 16);
        sectionLayout.childAlignment = TextAnchor.UpperCenter;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = false;
        sectionLayout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateTMPText("Narrative_Title", section.transform, "Three Worlds, One Journey", 32f, PrimaryText, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        SetPreferredSize(title.gameObject, 900f, 48f);

        TextMeshProUGUI body = CreateTMPText(
            "Narrative_Body",
            section.transform,
            "Trace the subtle threads connecting the cultures. A curated exploration of shared human heritage.",
            18f,
            SecondaryText,
            TextAlignmentOptions.Center);
        body.enableWordWrapping = true;
        SetPreferredSize(body.gameObject, 820f, 78f);

        Button button = CreateButton("Btn_BeginReading", section.transform, "Begin Reading", PrimaryText, White);
        SetPreferredSize(button.gameObject, 200f, 50f);
    }

    private static TMP_Dropdown CreateDropdown(string name, Transform parent, IEnumerable<string> options)
    {
        GameObject dropdownObject = CreateUIObject(name, parent);
        Image background = dropdownObject.AddComponent<Image>();
        background.color = White;

        Outline outline = dropdownObject.AddComponent<Outline>();
        outline.effectColor = CardBorder;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = background;
        dropdown.options.Clear();

        foreach (string option in options)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));
        }

        TextMeshProUGUI label = CreateTMPText("Label", dropdownObject.transform, string.Empty, 18f, PrimaryText, TextAlignmentOptions.MidlineLeft);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f, 0f);
        labelRect.offsetMax = new Vector2(-42f, 0f);
        dropdown.captionText = label;

        TextMeshProUGUI arrow = CreateTMPText("Arrow", dropdownObject.transform, "▾", 18f, SecondaryText, TextAlignmentOptions.Center);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-18f, 0f);
        arrowRect.sizeDelta = new Vector2(20f, 24f);

        CreateDropdownTemplate(dropdownObject.transform, dropdown);
        dropdown.RefreshShownValue();

        return dropdown;
    }

    private static void CreateDropdownTemplate(Transform parent, TMP_Dropdown dropdown)
    {
        GameObject template = CreateUIObject("Template", parent);
        template.SetActive(false);
        Image templateImage = template.AddComponent<Image>();
        templateImage.color = White;
        ScrollRect templateScrollRect = template.AddComponent<ScrollRect>();
        templateScrollRect.horizontal = false;
        templateScrollRect.vertical = true;
        templateScrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, -4f);
        templateRect.sizeDelta = new Vector2(0f, 180f);

        GameObject viewport = CreateUIObject("Viewport", template.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        viewportImage.raycastTarget = true;
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        Stretch(viewport.GetComponent<RectTransform>());

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject item = CreateUIObject("Item", content.transform);
        Toggle itemToggle = item.AddComponent<Toggle>();
        Image itemBackground = item.AddComponent<Image>();
        itemBackground.color = White;
        itemToggle.targetGraphic = itemBackground;

        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.minHeight = 44f;
        itemLayout.preferredHeight = 44f;

        TextMeshProUGUI itemLabel = CreateTMPText("Item Label", item.transform, "Option", 18f, PrimaryText, TextAlignmentOptions.MidlineLeft);
        RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(18f, 0f);
        itemLabelRect.offsetMax = new Vector2(-18f, 0f);

        templateScrollRect.viewport = viewport.GetComponent<RectTransform>();
        templateScrollRect.content = contentRect;
        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image background = buttonObject.AddComponent<Image>();
        background.color = backgroundColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;

        TextMeshProUGUI buttonText = CreateTMPText("Label", buttonObject.transform, label, 16f, textColor, TextAlignmentOptions.Center);
        buttonText.fontStyle = FontStyles.Bold;
        Stretch(buttonText.GetComponent<RectTransform>());

        return button;
    }

    private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Canvas GetOrCreateActiveCanvas()
    {
        Canvas canvas = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<Canvas>()
            : null;

        if (canvas == null)
        {
            canvas = Object.FindObjectOfType<Canvas>();
        }

        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasObject = CreateUIObject("Ola Refined Canvas", null);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Stretch(canvasObject.GetComponent<RectTransform>());
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Ola Canvas");
        return canvas;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void SetPreferredSize(GameObject gameObject, float width, float height)
    {
        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        if (width > 0f)
        {
            layoutElement.preferredWidth = width;
        }

        if (height > 0f)
        {
            layoutElement.preferredHeight = height;
        }
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
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
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
}
