using System;
using System.Collections.Generic;
using PaintIn2D;
using UnityEngine;
using UnityEngine.UI;

public class ColorSelection : MonoBehaviour
{
    public List<ColorPaintDecalClass> ColorPaintDecalClassList;
    
    public ColorType ColorType;
    private Color originalColor;
    
    public Button RedButton, YellowButton, GreenButton, PinkButton, BlueButton, OrangeButton, WhiteButton;
    
    public Color RedColor, YellowColor, GreenColor, BlueColor, OrangeColor, WhiteColor, PinkColor;

    public CwPaintDecal2D PaintDecal2D;
    public MeshDrawer_MatClone MeshDrawer_MatClone;
    public SegmentedLineDrawer SegmentedLineDrawer;
    
    private void Start()
    {
        originalColor = PaintDecal2D.Color;
        RedButton.onClick?.AddListener(RedButton_OnClick);
        YellowButton.onClick?.AddListener(YellowButton_OnClick);
        GreenButton.onClick?.AddListener(GreenButton_OnClick);
        PinkButton.onClick?.AddListener(PinkButton_OnClick);
        BlueButton.onClick?.AddListener(BlueButton_OnClick);
        OrangeButton.onClick?.AddListener(OrangeButton_OnClick);
        WhiteButton.onClick?.AddListener(WhiteButton_OnClick);
    }

    public Color GetOriginalColor() => originalColor;

    private void RedButton_OnClick()
    {
        originalColor = RedColor;
        ColorType = ColorType.Red;
        SetColor();
    }
    private void YellowButton_OnClick()
    {
        originalColor = YellowColor;
        ColorType = ColorType.Yellow;
        SetColor();
    }
    private void GreenButton_OnClick()
    {
        originalColor = GreenColor;
        ColorType = ColorType.Green;
        SetColor();
    }
    private void PinkButton_OnClick()
    {
        originalColor = PinkColor;
        ColorType = ColorType.Pink;
        SetColor();
    }
    private void WhiteButton_OnClick()
    {
        originalColor = WhiteColor;
        ColorType = ColorType.White;
        SetColor();
    }
    private void OrangeButton_OnClick()
    {
        originalColor = OrangeColor;
        ColorType = ColorType.Orange;
        SetColor();
    }
    private void BlueButton_OnClick()
    {
        originalColor = BlueColor;
        ColorType = ColorType.Blue;
        SetColor();
    }
    public void SetColor()
    {
       // PaintDecal2D.Color = originalColor;
        MeshDrawer_MatClone.SetColor(originalColor);
        MeshDrawer_MatClone.ColorType = ColorType;
        SelectColor(ColorType);
        SegmentedLineDrawer.SetMainColor(originalColor);
    }

    public void SelectColor(ColorType colorType)
    {
        foreach (ColorPaintDecalClass colorPaintDecalClass in ColorPaintDecalClassList)
        {
            if (colorPaintDecalClass.ColorType == colorType)
            {
                colorPaintDecalClass.PaintDecal.SetActive(true);
            }
            else
            {
                colorPaintDecalClass.PaintDecal.SetActive(false);
            }
        }
    }

    public void PaintDecalsAreDeactive()
    {
        foreach (ColorPaintDecalClass colorPaintDecalClass in ColorPaintDecalClassList)
        {
            colorPaintDecalClass.PaintDecal.SetActive(false);
        }
    }
}

[Serializable]
public class ColorPaintDecalClass
{
    public ColorType ColorType;
    public GameObject PaintDecal;
}