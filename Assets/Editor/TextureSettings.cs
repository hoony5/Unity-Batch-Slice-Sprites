using UnityEditor;
using UnityEngine;

[System.Serializable]
public class TextureSettings
{
    public bool useCrunchCompression;
    public bool overriden;
    public bool sRGBTexture;
    public bool alphaIsTransparency;
    public int spritePixelsPerUnit;
    public int compressionQuality;
    public int extrudeSize;
    public int maxTextureSize;
    public int anisoLevel;
    public SpriteMeshType meshType;
    public SpriteImportMode importMode;
    public SliceType sliceType;
    public TextureImporterFormat importerFormat;
    public TextureImporterCompression compressionType;
    public TextureWrapMode wrapMode;
    public TextureResizeAlgorithm resizeType;
    public FilterMode filterMode;
    public TextureImporterAlphaSource alphaSource;
    public TextureImporterType textureType;
    public Vector2 spritePivot;
    public string buildTarget;

    public TextureSettings(SliceType slicingType = SliceType.Auto,TextureImporterType textureImporterType = TextureImporterType.Sprite)
    {
        useCrunchCompression = false;
        overriden = false;
        sRGBTexture = true;
        alphaIsTransparency = true;
        spritePixelsPerUnit = 100;
        compressionQuality = 50;
        extrudeSize = 0;
        maxTextureSize = 128;
        anisoLevel = 1;
        meshType = SpriteMeshType.Tight;
        importMode = SpriteImportMode.Single;
        sliceType = slicingType;
        importerFormat = TextureImporterFormat.Automatic;
        compressionType = TextureImporterCompression.CompressedHQ;
        wrapMode = TextureWrapMode.Clamp;
        filterMode = FilterMode.Point;
        resizeType = TextureResizeAlgorithm.Mitchell;
        alphaSource = TextureImporterAlphaSource.FromInput;
        textureType = textureImporterType;
        spritePivot = new Vector2(0.5f, 0.5f);
        buildTarget = "Standalone";
    }
}