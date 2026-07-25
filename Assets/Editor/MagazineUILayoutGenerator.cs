using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MagazineUILayoutGenerator
{
    [MenuItem("Tools/Generate Magazine UI")]
    public static void GenerateMagazineUI()
    {
        Canvas canvas = GetActiveCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Generate Magazine UI",
                "No active Canvas was found in the current scene. Please create or select a Canvas first.",
                "OK");
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Transform existingScrollView = canvasRect.Find("Scroll View");
        if (existingScrollView != null)
        {
            Undo.DestroyObjectImmediate(existingScrollView.gameObject);
        }

        GameObject scrollView = CreateUIObject("Scroll View", canvasRect);
        Stretch(scrollView.GetComponent<RectTransform>());

        Image scrollBackground = scrollView.AddComponent<Image>();
        scrollBackground.color = new Color(0.96f, 0.95f, 0.92f, 1f);
        scrollBackground.raycastTarget = true;

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 24f;

        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        Stretch(viewport.GetComponent<RectTransform>());

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;

        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(40, 40, 40, 40);
        contentLayout.spacing = 30f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentSizeFitter = content.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;

        CreateImageBlock("Hero_Banner", content.transform, new Color(0.12f, 0.15f, 0.16f), 400f);

        GameObject row = CreateUIObject("Row_1", content.transform);
        LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
        rowLayoutElement.minHeight = 350f;
        rowLayoutElement.preferredHeight = 350f;

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 30f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        CreateImageBlock("Card_Food", row.transform, new Color(0.70f, 0.36f, 0.22f), 350f, 1.5f);
        CreateImageBlock("Card_Fest", row.transform, new Color(0.18f, 0.34f, 0.56f), 350f, 1f);

        CreateImageBlock("Card_Cloth", content.transform, new Color(0.86f, 0.73f, 0.45f), 350f);

        Undo.RegisterCreatedObjectUndo(scrollView, "Generate Magazine UI");
        Selection.activeGameObject = scrollView;
        EditorUtility.SetDirty(canvas.gameObject);
    }

    private static Canvas GetActiveCanvas()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            Canvas selectedCanvas = selectedObject.GetComponentInParent<Canvas>();
            if (selectedCanvas != null && selectedCanvas.isActiveAndEnabled)
            {
                return selectedCanvas;
            }
        }

        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.isActiveAndEnabled && canvas.gameObject.activeInHierarchy)
            {
                return canvas;
            }
        }

        return null;
    }

    private static GameObject CreateImageBlock(string name, Transform parent, Color color, float minHeight, float flexibleWidth = 0f)
    {
        GameObject block = CreateUIObject(name, parent);

        Image image = block.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        LayoutElement layoutElement = block.AddComponent<LayoutElement>();
        layoutElement.minHeight = minHeight;
        layoutElement.preferredHeight = minHeight;
        layoutElement.flexibleWidth = flexibleWidth;

        return block;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localPosition = Vector3.zero;
        rectTransform.sizeDelta = Vector2.zero;
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
