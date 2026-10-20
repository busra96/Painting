using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Gallery
{
// Applies the selected catalog painting before ImageGridProcessor builds its reference grid.
// The scene keeps its authored target; this only swaps sprites, so layout edits are preserved.
[DefaultExecutionOrder(-600)]
public sealed class PaintingTargetApplier : MonoBehaviour
{
    public PaintingCatalog catalog;
    public Image targetArtwork;
    public GameObject targetCard;
    public ImageGridProcessor reference;

    public PaintingCatalog.Entry ActiveEntry { get; private set; }

    private void Awake()
    {
        string selected = PaintingSession.SelectedId;
        if (catalog != null && catalog.allowFreePainting && selected == PaintingSession.FreePaintingId)
        {
            if (targetCard != null) targetCard.SetActive(false);
            PaintingSession.SetActive(PaintingSession.FreePaintingId, catalog.freePaintingTitle, true);
            return;
        }
        if (targetCard != null) targetCard.SetActive(true);
        ActiveEntry = catalog != null ? catalog.Find(selected) ?? catalog.Default : null;
        if (ActiveEntry == null)
        {
            // No catalog: keep the target authored in the scene.
            Sprite authored = targetArtwork != null ? targetArtwork.sprite : null;
            PaintingSession.SetActive("", authored != null ? authored.name : "", false);
            return;
        }
        if (targetArtwork != null) targetArtwork.sprite = ActiveEntry.artwork;
        if (reference != null && reference.spriteRenderer != null)
        {
            reference.spriteRenderer.sprite = ActiveEntry.artwork;
            reference.RebuildGrid();
        }
        PaintingSession.SetActive(ActiveEntry.id, ActiveEntry.title, false);
    }
}
}
