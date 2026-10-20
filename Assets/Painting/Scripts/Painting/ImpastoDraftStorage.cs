using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public sealed partial class ImpastoPaintSurface
{
    public void WriteArtistDraft(string path)
    {
        if (!IsReady || IsSpreading) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + ".tmp";
        using (var file = File.Create(temp))
        using (var gzip = new GZipStream(file, CompressionMode.Compress))
        using (var writer = new BinaryWriter(gzip))
        {
            writer.Write(1); writer.Write(width); writer.Write(height);
            writer.Write(DepositedVolume); writer.Write(PaperVolume); writer.Write(depositOrder);
            for (int i = 0; i < thickness.Length; i++)
            {
                writer.Write(thickness[i]); WriteVector(writer, pigment[i]);
                writer.Write(surfaceThickness[i]); WriteVector(writer, surfacePigment[i]);
                writer.Write(surfaceOrder[i]);
            }
        }
        string backup = path + ".bak";
        if (File.Exists(backup)) File.Delete(backup);
        if (File.Exists(path)) File.Move(path, backup);
        try { File.Move(temp, path); }
        catch
        {
            if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
            throw;
        }
    }

    public bool ReadArtistDraft(string path)
    {
        if (!IsReady) return false;
        if (!File.Exists(path) && File.Exists(path + ".bak")) path += ".bak";
        if (!File.Exists(path)) return false;
        // Restore into temporary arrays so a truncated file cannot partially overwrite the surface.
        try
        {
            using (var file = File.OpenRead(path))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            using (var reader = new BinaryReader(gzip))
            {
                if (reader.ReadInt32() != 1 || reader.ReadInt32() != width || reader.ReadInt32() != height) return false;
                double deposited = reader.ReadDouble(), storedPaper = reader.ReadDouble();
                int order = reader.ReadInt32();
                var pixels = new SavedPixel[thickness.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    float h = reader.ReadSingle(); Vector3 mass = ReadVector(reader);
                    float coat = reader.ReadSingle(); Vector3 coatMass = ReadVector(reader);
                    int pixelOrder = reader.ReadInt32();
                    if (!Finite(h) || h < 0 || !Finite(coat) || coat < 0 || !Finite(mass) || !Finite(coatMass))
                        throw new InvalidDataException("Invalid paint data.");
                    pixels[i] = new SavedPixel { height = h, pigment = mass, coatHeight = coat,
                        coatPigment = coatMass, order = pixelOrder };
                }
                ClearPaint();
                for (int i = 0; i < pixels.Length; i++)
                {
                    thickness[i] = pixels[i].height; pigment[i] = pixels[i].pigment;
                    surfaceThickness[i] = pixels[i].coatHeight; surfacePigment[i] = pixels[i].coatPigment;
                    surfaceOrder[i] = pixels[i].order;
                }
                depositOrder = order; DepositedVolume = deposited; PaperVolume = storedPaper;
                hasDepositColor = false;
                IncludeChanged(0, 0, width - 1, height - 1); dirty = true; Flush();
                return true;
            }
        }
        catch (Exception exception) when (exception is IOException || exception is InvalidDataException)
        {
            Debug.LogWarning("Atelier draft could not be restored: " + exception.Message);
            return false;
        }
    }

    private static void WriteVector(BinaryWriter writer, Vector3 value)
    { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
    private static Vector3 ReadVector(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
}
