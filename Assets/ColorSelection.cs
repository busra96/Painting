using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ColorSelection : MonoBehaviour
{
    public List<ColorPaintDecalClass> ColorPaintDecalClassList;
    public ColorType ColorType;
    private Color originalColor;
    public Button RedButton, YellowButton, GreenButton, PinkButton, BlueButton, OrangeButton, WhiteButton;
    public Color RedColor, YellowColor, GreenColor, BlueColor, OrangeColor, WhiteColor, PinkColor;
    public GameObject ShadowObj;
    [HideInInspector] public bool UseArtworkButtons;
    private Button[] buttons;
    private ColorType[] types;
    private Color[] colors;
    private Vector3[] scales;
    private UnityAction[] listeners;
    private Tween[] selectionTweens;

    private void Start()
    {
        buttons = new[] { RedButton, YellowButton, GreenButton, PinkButton, BlueButton, OrangeButton, WhiteButton };
        types = new[] { ColorType.Red, ColorType.Yellow, ColorType.Green, ColorType.Pink, ColorType.Blue, ColorType.Orange, ColorType.White };
        colors = new[] { RedColor, YellowColor, GreenColor, PinkColor, BlueColor, OrangeColor, WhiteColor };
        scales = new Vector3[buttons.Length];
        listeners = new UnityAction[buttons.Length];
        selectionTweens = new Tween[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            int index = i;
            if (buttons[i].targetGraphic != null)
                buttons[i].targetGraphic.color = UseArtworkButtons ? new Color(1, 1, 1, 0.01f) : colors[i];
            scales[i] = buttons[i].transform.localScale;
            listeners[i] = () => Choose(index);
            buttons[i].onClick.AddListener(listeners[i]);
        }
        SetColor();
    }

    private void Choose(int index)
    {
        ColorType = types[index];
        SetColor();
    }

    public Color GetOriginalColor() => originalColor;

    public void SetColor() => SelectColor(ColorType);

    public void SelectColor(ColorType colorType)
    {
        ColorType = colorType;
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool selected = types[i] == colorType;
                if (selected) originalColor = colors[i];
                if (buttons[i] == null) continue;
                selectionTweens[i]?.Kill();
                selectionTweens[i] = buttons[i].transform.DOScale(scales[i] * (selected ? 1.12f : 1f), 0.16f).SetEase(Ease.OutQuad);
            }
        }
        if (ShadowObj != null) ShadowObj.SetActive(true);
        if (ColorPaintDecalClassList == null) return;
        foreach (ColorPaintDecalClass entry in ColorPaintDecalClassList)
            if (entry.PaintDecal != null) entry.PaintDecal.SetActive(entry.ColorType == colorType);
    }

    public void SelectEaser()
    {
        if (ShadowObj != null) ShadowObj.SetActive(false);
        PaintDecalsAreDeactive();
    }

    public void ColorsDeactive()
    {
        if (buttons == null) return;
        foreach (Button button in buttons)
            if (button != null) button.gameObject.SetActive(false);
    }

    public void PaintDecalsAreDeactive()
    {
        if (ColorPaintDecalClassList == null) return;
        foreach (ColorPaintDecalClass entry in ColorPaintDecalClassList)
            if (entry.PaintDecal != null) entry.PaintDecal.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            selectionTweens[i]?.Kill();
            if (buttons[i] != null) buttons[i].onClick.RemoveListener(listeners[i]);
        }
    }
}

[Serializable]
public class ColorPaintDecalClass
{
    public ColorType ColorType;
    public GameObject PaintDecal;
}
