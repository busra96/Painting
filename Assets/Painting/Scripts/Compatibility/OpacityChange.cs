using PaintIn2D;
using UnityEngine;

public class OpacityChange : MonoBehaviour
{
    public SettingsContain SettingsContain;
    private CwPaintDecal2D _cwPaintDecal2D;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _lerpSpeed;
    private float density;
    private float startDensity;
    
    public void Init(Vector3 startPos, CwPaintDecal2D cwPaintDecal2D)
    {
        _cwPaintDecal2D = cwPaintDecal2D;
        _startPos = startPos;
        _lerpSpeed = Random.Range(SettingsContain.MinOpacityLerpSpeed, SettingsContain.MaxOpacityLerpSpeed);
    }

    public void SetEndPos(Vector3 endPos)
    {
        _endPos = endPos;
    }

    public void SetDensity(float density)
    {
        this.density = density;
        startDensity = density;
    }
    
   /* public void UpdateOpacityLinear()
    {
        float totalDistance = Vector3.Distance(_startPos, _endPos);
        float currentDistance = Vector3.Distance(transform.position, _endPos);

        float progress = 1 - (currentDistance / totalDistance);
      
        float targetOpacity  = Mathf.Lerp(1f, 0f, progress);
     
        _cwPaintDecal2D.Opacity = Mathf.Lerp(_cwPaintDecal2D.Opacity, targetOpacity, Time.deltaTime * _lerpSpeed);
    }*/
   
    public void UpdateOpacityLinear()
   {
       // Belirli bir yoğunluk aralığına göre yoğunluğu normalize ediyoruz
       float normalizedDensity = Mathf.Clamp01(density / SettingsContain.MaxDensity); // _maxDensity, yoğunluğun maksimum değeri olmalı.

       // Normalize edilmiş yoğunluğa bağlı olarak bir yavaşlık faktörü hesaplıyoruz
       // Yoğunluk yüksekse (normalizedDensity yaklaştıkça), daha yavaş bir geçiş sağlanır
       float lerpFactor = Mathf.Lerp(SettingsContain.MinDensityLerpSpeed, SettingsContain.MaxDensityLerpSpeed, 1f - normalizedDensity);

       // Hedef opaklığı belirliyoruz (örneğin: her zaman sıfıra yaklaşmak)
       float targetOpacity = Mathf.Lerp(1f, 0f, normalizedDensity);

       // Opaklığı yoğunluğa bağlı bir şekilde güncelliyoruz
       _cwPaintDecal2D.Opacity = Mathf.Lerp(_cwPaintDecal2D.Opacity, targetOpacity, Time.deltaTime * lerpFactor);
   }
    
    public void UpdateOpacityWithDistanceAndDensity()
    {
      /*  density -= (SettingsContain.OpacityMultiplier * (density / startDensity));
        density = Mathf.Clamp(density, 1, density);

        // Toplam mesafe ve mevcut mesafeyi hesaplıyoruz
        float totalDistance = Vector3.Distance(_startPos, _endPos);
        float currentDistance = Vector3.Distance(transform.position, _endPos);

        // Mesafeye bağlı olarak ilerleme oranını hesaplıyoruz (0 - 1 aralığında)
        float distanceProgress = 1 - (currentDistance / totalDistance);

        // Yoğunluğa bağlı bir hız faktörü hesaplıyoruz
        float normalizedDensity = Mathf.Clamp01(density /  SettingsContain.MaxDensity); // _maxDensity: yoğunluğun maksimum değeri
        float densityFactor = 1f - normalizedDensity; // Yoğunluk azaldıkça hız artar

        // Mesafe ve yoğunluk faktörlerini birleştirerek opaklık oranını belirliyoruz
        float targetOpacity = Mathf.Lerp(1f, 0f, distanceProgress * densityFactor);

        // Opaklığı yoğunluk ve mesafe oranına bağlı bir şekilde güncelliyoruz
        _cwPaintDecal2D.Opacity = Mathf.Lerp(_cwPaintDecal2D.Opacity, targetOpacity, Time.deltaTime * _lerpSpeed);*/
       
    }
}
