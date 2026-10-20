using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Gallery
{
// "KAYDET" button: stores the current painting (with its match score on the result screen) in the gallery.
public sealed class PaintingSaver : MonoBehaviour
{
    public UIManager ui;
    public Camera mainCamera;
    public SpriteRenderer paper;
    public Button saveButton;
    public TMP_Text saveLabel;
    public string saveText = "KAYDET";
    public string savedText = "KAYDEDİLDİ";
    [Min(256)] public int maxImageWidth = 1024;

    private long savedVersion = -1;

    private void OnEnable() { if (saveButton != null) saveButton.onClick.AddListener(Save); }
    private void OnDisable() { if (saveButton != null) saveButton.onClick.RemoveListener(Save); }

    private void Update()
    {
        if (saveButton == null || ui == null) return;
        bool saved = savedVersion == ui.ArtworkVersion;
        saveButton.interactable = ui.CanSaveArtwork && !saved;
        if (saveLabel != null)
        {
            string text = saved ? savedText : saveText;
            if (saveLabel.text != text) saveLabel.text = text;
        }
    }

    public void Save()
    {
        if (ui == null || !ui.CanSaveArtwork || savedVersion == ui.ArtworkVersion) return;
        Texture2D image = null;
        try
        {
            image = PaintingCapture.CapturePaper(mainCamera != null ? mainCamera : Camera.main, paper, maxImageWidth);
            if (image == null) return;
            bool scored = ui.IsShowingResult && ui.HasResultScore;
            PaintingGallery.Save(image, PaintingSession.ActiveTitle, PaintingSession.ActiveId, scored, scored ? ui.ResultScore : 0);
            savedVersion = ui.ArtworkVersion;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            if (saveLabel != null) saveLabel.text = "HATA";
        }
        finally { if (image != null) Destroy(image); }
    }
}
}
