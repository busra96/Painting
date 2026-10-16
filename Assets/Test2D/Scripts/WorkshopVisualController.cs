using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Artwork supplies the visual shell; existing controls and paint remain live.
[DefaultExecutionOrder(-500)]
public sealed class WorkshopVisualController : MonoBehaviour
{
    private const float ArtworkWidth = 941f, ArtworkHeight = 1672f;
    public static readonly Rect PaperViewport = new Rect(0.115f, 0.308f, 0.79f, 0.477f);
    private readonly Image[] amountSegments = new Image[7];
    private readonly float[] paletteCenters = { 110f, 253f, 398f, 545f, 686f, 834f };
    private Camera mainCamera;
    private PaintAmount paintAmount;
    private ColorSelection selection;
    private RectTransform uiRoot, selectedMark;
    private Image retryCover;
    private TMP_Text doneLabel;
    private Sprite backgroundSprite, retrySprite;
    private float previousRemaining = -1;
    private ColorType previousColor = (ColorType)(-1);
    public bool IsReady { get; private set; }
    public int AmountSegmentCount => amountSegments.Length;
    public SpriteRenderer BackgroundRenderer { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= Install;
        SceneManager.sceneLoaded += Install;
    }

    private static void Install(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Test 1" && FindFirstObjectByType<WorkshopVisualController>() == null)
            new GameObject("Workshop Visual Style").AddComponent<WorkshopVisualController>();
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        UIManager manager = FindFirstObjectByType<UIManager>();
        Canvas canvas = manager != null ? manager.GetComponent<Canvas>() : null;
        selection = FindFirstObjectByType<ColorSelection>();
        paintAmount = FindFirstObjectByType<PaintAmount>();
        Texture2D texture = Resources.Load<Texture2D>("Workshop/WorkshopBackground");
        if (mainCamera == null || canvas == null || selection == null || texture == null) return;
        SpriteRenderer paper = FindRenderer("Paintable Book  Paint");
        if (paper == null) return;
        // Project the original world dimensions into the artwork; resizing never changes paint volume.
        float worldHeight = paper.bounds.size.y / PaperViewport.height;
        float worldWidth = paper.bounds.size.x / PaperViewport.width;
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = worldHeight * 0.5f;
        mainCamera.aspect = worldWidth / worldHeight;
        Vector3 center = paper.bounds.center;
        mainCamera.transform.position = new Vector3(center.x - (PaperViewport.center.x - 0.5f) * worldWidth,
            center.y - (PaperViewport.center.y - 0.5f) * worldHeight, mainCamera.transform.position.z);
        paper.GetComponent<ImpastoPaintSurface>().RevealBackdropCanvas = true;
        backgroundSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        backgroundSprite.name = "Workshop Background";
        GameObject background = new GameObject("Workshop Background");
        background.transform.SetParent(transform, false);
        BackgroundRenderer = background.AddComponent<SpriteRenderer>();
        BackgroundRenderer.sprite = backgroundSprite;
        BackgroundRenderer.sortingLayerID = paper.sortingLayerID;
        BackgroundRenderer.sortingOrder = paper.sortingOrder - 100;
        RefreshLayout();
        SpriteRenderer guide = FindRenderer("Paintable Book  BlackLine");
        if (guide != null) guide.enabled = false;
        SpriteRenderer reference = FindRenderer("Last Image Sprite Renderer  Grid Processors");
        if (reference != null) reference.enabled = false;
        uiRoot = new GameObject("Workshop Controls", typeof(RectTransform)).GetComponent<RectTransform>();
        uiRoot.gameObject.layer = LayerMask.NameToLayer("UI");
        uiRoot.SetParent(canvas.transform, false);
        Stretch(uiRoot);
        SetupPalette();
        SetupAmountAndStatus();
        SetupDoneButton(manager);
        GridDataComparer comparer = FindFirstObjectByType<GridDataComparer>();
        if (comparer != null)
        {
            // This old score panel also hosts the scoring coroutine; keep its behaviour alive.
            comparer.transform.SetParent(uiRoot, false);
            foreach (Graphic graphic in comparer.GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;
            comparer.slider = paintAmount != null ? paintAmount.PaintAmountSlider : null;
            comparer.yuzdelikText = null;
        }
        // Retire old panels and score bar only after moving the live controls.
        foreach (Transform child in canvas.transform)
            if (child != uiRoot && child.GetComponent<EventSystem>() == null) child.gameObject.SetActive(false);
        IsReady = true;
        UpdateVisuals();
    }

    private void SetupPalette()
    {
        selection.UseArtworkButtons = true;
        Button[] buttons = { selection.RedButton, selection.YellowButton, selection.GreenButton,
            selection.PinkButton, selection.BlueButton, selection.OrangeButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            RectTransform rect = buttons[i].GetComponent<RectTransform>();
            rect.SetParent(uiRoot, false);
            Place(rect, paletteCenters[i], 1340, 140, 160);
            if (buttons[i].targetGraphic != null) buttons[i].targetGraphic.color = new Color(1, 1, 1, 0.01f);
        }
        Image marker = NewImage("Selected Paint", uiRoot);
        marker.color = new Color(1f, 0.91f, 0.57f);
        marker.raycastTarget = false;
        selectedMark = marker.rectTransform;
    }

    private void SetupAmountAndStatus()
    {
        if (paintAmount != null && paintAmount.PaintAmountSlider != null)
        {
            Slider slider = paintAmount.PaintAmountSlider;
            RectTransform rect = slider.GetComponent<RectTransform>();
            rect.SetParent(uiRoot, false);
            Place(rect, 580, 272, 288, 55);
            // PaintAmount changes fill colors later, so disable the old images rather than tint them.
            foreach (Image image in slider.GetComponentsInChildren<Image>(true)) image.enabled = false;
            TMP_Text label = slider.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.transform.SetParent(slider.transform, false);
                Stretch(label.rectTransform);
                label.color = new Color(1f, 0.88f, 0.6f);
                label.alignment = TextAlignmentOptions.Center;
                label.fontStyle = FontStyles.Bold;
                label.fontSize = 25;
                label.outlineWidth = 0;
                label.raycastTarget = false;
            }
        }
        float[] centers = { 389f, 448f, 510f, 576f, 642f, 707f, 766f };
        for (int i = 0; i < amountSegments.Length; i++)
        {
            Image cover = NewImage("Amount Segment " + (i + 1), uiRoot);
            Place(cover.rectTransform, centers[i], 141, 54, 35);
            cover.raycastTarget = false;
            amountSegments[i] = cover;
        }
    }

    private void SetupDoneButton(UIManager manager)
    {
        if (manager.DoneButton == null) return;
        RectTransform rect = manager.DoneButton.GetComponent<RectTransform>();
        rect.SetParent(uiRoot, false);
        Place(rect, 474, 1541, 405, 110);
        if (manager.DoneButton.targetGraphic != null) manager.DoneButton.targetGraphic.color = new Color(1, 1, 1, 0.01f);
        doneLabel = manager.DoneButton.GetComponentInChildren<TMP_Text>(true);
        retryCover = NewImage("Retry Label Backing", rect);
        retryCover.rectTransform.anchorMin = new Vector2(-0.025f, -0.10f);
        retryCover.rectTransform.anchorMax = new Vector2(1.025f, 1.10f);
        retryCover.rectTransform.offsetMin = retryCover.rectTransform.offsetMax = Vector2.zero;
        Texture2D buttonTexture = Resources.Load<Texture2D>("Workshop/BlankButton");
        if (buttonTexture != null)
        {
            retrySprite = Sprite.Create(buttonTexture,
                new Rect(buttonTexture.width * 0.035f, buttonTexture.height * 0.09f,
                    buttonTexture.width * 0.93f, buttonTexture.height * 0.81f),
                new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
            retryCover.sprite = retrySprite;
        }
        retryCover.color = Color.white;
        retryCover.raycastTarget = false;
        if (doneLabel != null)
        {
            doneLabel.transform.SetAsLastSibling();
            Stretch(doneLabel.rectTransform);
            doneLabel.alignment = TextAlignmentOptions.Center;
            doneLabel.fontStyle = FontStyles.Bold;
            doneLabel.fontSize = 64;
            doneLabel.outlineWidth = 0;
        }
    }

    private void LateUpdate() { if (IsReady) UpdateVisuals(); }

    private void UpdateVisuals()
    {
        float remaining = paintAmount != null ? paintAmount.RemainingFraction : 1;
        if (!Mathf.Approximately(previousRemaining, remaining))
        {
            for (int i = 0; i < amountSegments.Length; i++)
                amountSegments[i].color = new Color(0.12f, 0.035f, 0.012f,
                    (1 - Mathf.Clamp01(remaining * amountSegments.Length - i)) * 0.87f);
            previousRemaining = remaining;
        }
        if (previousColor != selection.ColorType)
        {
            ColorType[] types = { ColorType.Red, ColorType.Yellow, ColorType.Green,
                ColorType.Pink, ColorType.Blue, ColorType.Orange };
            for (int i = 0; i < types.Length; i++)
                if (types[i] == selection.ColorType) Place(selectedMark, paletteCenters[i], 1432, 32, 4);
            previousColor = selection.ColorType;
        }
        selectedMark.gameObject.SetActive(selection.RedButton != null && selection.RedButton.gameObject.activeSelf);
        bool retry = doneLabel != null && doneLabel.text != "DONE";
        retryCover.gameObject.SetActive(retry);
        if (doneLabel != null) doneLabel.color = retry ? new Color32(70, 29, 10, 255) : Color.clear;
    }

    public void RefreshLayout()
    {
        if (BackgroundRenderer == null) return;
        float depth = 2f - mainCamera.transform.position.z;
        BackgroundRenderer.transform.position = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        Vector2 size = BackgroundRenderer.sprite.bounds.size;
        BackgroundRenderer.transform.localScale = new Vector3(
            2 * mainCamera.orthographicSize * mainCamera.aspect / size.x,
            2 * mainCamera.orthographicSize / size.y, 1);
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        Vector2 center = new Vector2(x / ArtworkWidth, 1 - y / ArtworkHeight);
        Vector2 half = new Vector2(width / ArtworkWidth, height / ArtworkHeight) * 0.5f;
        rect.anchorMin = center - half;
        rect.anchorMax = center + half;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Image NewImage(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<Image>();
    }

    private static SpriteRenderer FindRenderer(string objectName)
    {
        foreach (SpriteRenderer renderer in FindObjectsOfType<SpriteRenderer>(true))
            if (renderer.name == objectName) return renderer;
        return null;
    }

    private void OnDestroy()
    {
        if (backgroundSprite != null) Destroy(backgroundSprite);
        if (retrySprite != null) Destroy(retrySprite);
    }
}
