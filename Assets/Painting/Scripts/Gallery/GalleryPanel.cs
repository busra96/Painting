using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Gallery
{
// Gallery overlay: "YENİ TABLO" lists the catalog (and a free canvas), "GALERİM" lists saved paintings.
// Every object is authored in the scene (Tools → Painting → Create Gallery Panel); only grid cards are
// cloned from the serialized template. The full-screen dim blocks painting input while the panel is open.
public sealed class GalleryPanel : MonoBehaviour
{
    public enum Tab { NewPainting, Saved }
    private enum DetailMode { None, Target, Saved }

    [Header("Data")]
    public PaintingCatalog catalog;
    public UIManager ui;
    [Tooltip("Width / height of the blank canvas preview shown for free painting.")]
    public float freePaintingAspect = 851f / 1261f;

    [Header("Panel")]
    public GameObject root;
    public Button openButton;
    public Button closeButton;
    public TMP_Text titleLabel;
    public Button newTabButton, savedTabButton;
    public Color selectedTabColor = Color.white;
    public Color idleTabColor = new Color(0.72f, 0.66f, 0.58f);
    public RectTransform content;
    public ScrollRect scroll;
    public GalleryItemView itemTemplate;
    public TMP_Text emptyLabel;

    [Header("Detail")]
    public GameObject detailRoot;
    public RawImage detailPicture;
    public AspectRatioFitter detailFitter;
    public TMP_Text detailTitle;
    public TMP_Text detailInfo;
    public Button primaryButton;
    public TMP_Text primaryLabel;
    public Button backButton;

    [Header("Texts")]
    public string newTabTitle = "YENİ TABLO";
    public string savedTabTitle = "GALERİM";
    public string emptyText = "Henüz kaydedilmiş tablo yok.";
    public string selectedText = "Seçili";
    public string startText = "BAŞLA";
    public string startInfo = "Başlayınca tuvaldeki boya silinir.";
    public string freeInfo = "Hedef resim yok, puanlanmaz.";
    public string deleteText = "SİL";
    public string confirmText = "EMİN MİSİN?";
    public string scoreFormat = "Puan %{0}";
    public string dateFormat = "dd.MM.yyyy HH:mm";

    public bool IsOpen => root != null && root.activeSelf;

    public event System.Action Closed;
    // True while closing because a new painting is being started (the scene is about to reload).
    public bool IsStartingPainting { get; private set; }

    private Tab tab = Tab.NewPainting;
    private bool tabChosen;
    private DetailMode detailMode;
    private string detailTargetId;
    private SavedPainting detailSaved;
    private bool confirmingDelete;
    private readonly List<GalleryItemView> cards = new List<GalleryItemView>();
    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();
    private Texture2D detailTexture;

    private void Awake()
    {
        if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
        if (root != null) root.SetActive(false);
    }

    private void OnEnable()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenDefault);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (newTabButton != null) newTabButton.onClick.AddListener(ShowNewTab);
        if (savedTabButton != null) savedTabButton.onClick.AddListener(ShowSavedTab);
        if (primaryButton != null) primaryButton.onClick.AddListener(PrimaryAction);
        if (backButton != null) backButton.onClick.AddListener(CloseDetail);
        PaintingGallery.Changed += OnGalleryChanged;
    }

    private void OnDisable()
    {
        if (openButton != null) openButton.onClick.RemoveListener(OpenDefault);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (newTabButton != null) newTabButton.onClick.RemoveListener(ShowNewTab);
        if (savedTabButton != null) savedTabButton.onClick.RemoveListener(ShowSavedTab);
        if (primaryButton != null) primaryButton.onClick.RemoveListener(PrimaryAction);
        if (backButton != null) backButton.onClick.RemoveListener(CloseDetail);
        PaintingGallery.Changed -= OnGalleryChanged;
    }

    private void OnDestroy() => ReleaseTextures();

    private void Update()
    {
        if (openButton != null) openButton.interactable = ui == null || !ui.IsBusy;
        if (!IsOpen) return;
        // Android back button / Escape.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (detailRoot != null && detailRoot.activeSelf) CloseDetail();
            else Close();
        }
    }

    public void OpenDefault()
    {
        // First time: show saved paintings if there are any, otherwise the catalog. Afterwards keep the last tab.
        Open(tabChosen ? tab : PaintingGallery.Items.Count > 0 ? Tab.Saved : Tab.NewPainting);
    }

    public void Open(Tab target)
    {
        if (root == null || (ui != null && ui.IsBusy)) return;
        root.SetActive(true);
        CloseDetail();
        ShowTab(target);
    }

    public void Close()
    {
        bool wasOpen = IsOpen;
        CloseDetail();
        if (root != null) root.SetActive(false);
        ReleaseTextures();
        if (wasOpen) Closed?.Invoke();
    }

    public void ShowNewTab() => ShowTab(Tab.NewPainting);
    public void ShowSavedTab() => ShowTab(Tab.Saved);

    private void ShowTab(Tab target)
    {
        tab = target;
        tabChosen = true;
        SetTabColor(newTabButton, tab == Tab.NewPainting);
        SetTabColor(savedTabButton, tab == Tab.Saved);
        if (titleLabel != null) titleLabel.text = tab == Tab.NewPainting ? newTabTitle : savedTabTitle;
        Rebuild();
        if (scroll != null) scroll.verticalNormalizedPosition = 1;
    }

    private void SetTabColor(Button button, bool selected)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = selected ? selectedTabColor : idleTabColor;
    }

    private void OnGalleryChanged() { if (IsOpen && tab == Tab.Saved) Rebuild(); }

    private void Rebuild()
    {
        ReleaseTextures();
        int used = 0;
        if (tab == Tab.NewPainting)
        {
            if (catalog != null)
            {
                foreach (PaintingCatalog.Entry entry in catalog.paintings)
                {
                    if (entry == null || entry.artwork == null) continue;
                    PaintingCatalog.Entry captured = entry;
                    GalleryItemView card = Card(used++);
                    card.SetPicture(entry.artwork.texture, GalleryItemView.SpriteUV(entry.artwork),
                        entry.artwork.rect.width / entry.artwork.rect.height);
                    Fill(card, entry.title, IsCurrent(entry.id) ? selectedText : "", IsCurrent(entry.id),
                        () => ShowTargetDetail(captured.id));
                }
                if (catalog.allowFreePainting)
                {
                    GalleryItemView card = Card(used++);
                    card.SetPicture(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), freePaintingAspect);
                    bool current = PaintingSession.ActiveIsFreePainting;
                    Fill(card, catalog.freePaintingTitle, current ? selectedText : "", current,
                        () => ShowTargetDetail(PaintingSession.FreePaintingId));
                }
            }
        }
        else
        {
            foreach (SavedPainting item in PaintingGallery.Items)
            {
                SavedPainting captured = item;
                GalleryItemView card = Card(used++);
                Texture2D thumb = PaintingGallery.LoadTexture(item, true);
                if (thumb != null) loadedTextures.Add(thumb);
                card.SetPicture(thumb, new Rect(0, 0, 1, 1), item.Aspect);
                Fill(card, item.title, SavedSummary(item, false), false, () => ShowSavedDetail(captured));
            }
        }
        for (int i = used; i < cards.Count; i++) cards[i].gameObject.SetActive(false);
        if (emptyLabel != null)
        {
            emptyLabel.text = emptyText;
            emptyLabel.gameObject.SetActive(used == 0);
        }
    }

    private bool IsCurrent(string id) => !PaintingSession.ActiveIsFreePainting && PaintingSession.ActiveId == id;

    private GalleryItemView Card(int index)
    {
        while (cards.Count <= index)
        {
            GalleryItemView card = Instantiate(itemTemplate, content);
            card.name = "Card " + cards.Count;
            cards.Add(card);
        }
        cards[index].gameObject.SetActive(true);
        cards[index].transform.SetSiblingIndex(index);
        return cards[index];
    }

    private static void Fill(GalleryItemView card, string caption, string detail, bool current,
        UnityEngine.Events.UnityAction onClick)
    {
        if (card.caption != null) card.caption.text = caption;
        if (card.detail != null) card.detail.text = detail;
        if (card.currentMark != null) card.currentMark.SetActive(current);
        if (card.button == null) return;
        card.button.onClick.RemoveAllListeners();
        card.button.onClick.AddListener(onClick);
    }

    private string SavedSummary(SavedPainting item, bool withTime)
    {
        string date = item.Created.ToString(withTime ? dateFormat : "dd.MM.yyyy");
        return item.hasScore ? string.Format(scoreFormat, Mathf.RoundToInt(item.score)) + (withTime ? "\n" : "  ") + date : date;
    }

    private void ShowTargetDetail(string id)
    {
        detailMode = DetailMode.Target;
        detailTargetId = id;
        detailSaved = null;
        bool free = id == PaintingSession.FreePaintingId;
        if (catalog == null) return;
        PaintingCatalog.Entry entry = free ? null : catalog.Find(id);
        if (!free && entry == null) return;
        if (free) SetDetailPicture(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), freePaintingAspect);
        else SetDetailPicture(entry.artwork.texture, GalleryItemView.SpriteUV(entry.artwork),
            entry.artwork.rect.width / entry.artwork.rect.height);
        ShowDetail(free ? catalog.freePaintingTitle : entry.title, free ? freeInfo + "\n" + startInfo : startInfo, startText);
    }

    private void ShowSavedDetail(SavedPainting item)
    {
        detailMode = DetailMode.Saved;
        detailSaved = item;
        ReleaseDetailTexture();
        detailTexture = PaintingGallery.LoadTexture(item, false);
        SetDetailPicture(detailTexture, new Rect(0, 0, 1, 1), item.Aspect);
        ShowDetail(item.title, SavedSummary(item, true), deleteText);
    }

    private void ShowDetail(string title, string info, string primary)
    {
        confirmingDelete = false;
        if (detailTitle != null) detailTitle.text = title;
        if (detailInfo != null) detailInfo.text = info;
        if (primaryLabel != null) primaryLabel.text = primary;
        if (detailRoot != null) detailRoot.SetActive(true);
    }

    private void SetDetailPicture(Texture texture, Rect uv, float aspect)
    {
        if (detailPicture != null)
        {
            detailPicture.texture = texture != null ? texture : Texture2D.whiteTexture;
            detailPicture.uvRect = uv;
        }
        if (detailFitter != null) detailFitter.aspectRatio = aspect > 0 ? aspect : 1;
    }

    private void PrimaryAction()
    {
        if (detailMode == DetailMode.Target)
        {
            string id = detailTargetId;
            IsStartingPainting = true;
            Close();
            PaintingSession.StartNewPainting(id);
        }
        else if (detailMode == DetailMode.Saved && detailSaved != null)
        {
            if (!confirmingDelete)
            {
                confirmingDelete = true;
                if (primaryLabel != null) primaryLabel.text = confirmText;
                return;
            }
            SavedPainting item = detailSaved;
            CloseDetail();
            PaintingGallery.Delete(item);
        }
    }

    public void CloseDetail()
    {
        detailMode = DetailMode.None;
        detailSaved = null;
        confirmingDelete = false;
        if (detailRoot != null) detailRoot.SetActive(false);
        if (detailPicture != null) detailPicture.texture = null;
        ReleaseDetailTexture();
    }

    private void ReleaseDetailTexture()
    {
        if (detailTexture != null) Destroy(detailTexture);
        detailTexture = null;
    }

    private void ReleaseTextures()
    {
        foreach (GalleryItemView card in cards)
            if (card != null && card.picture != null) card.picture.texture = null;
        foreach (Texture2D texture in loadedTextures) if (texture != null) Destroy(texture);
        loadedTextures.Clear();
        ReleaseDetailTexture();
    }
}
}
