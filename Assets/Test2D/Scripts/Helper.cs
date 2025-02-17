public static class Helper
{
    public static float Remap(float currentValue, float min1, float max1, float min2, float max2)
    {
        return (currentValue - min1) / (max1 - min1) * (max2 - min2) + min2;
    }
}
