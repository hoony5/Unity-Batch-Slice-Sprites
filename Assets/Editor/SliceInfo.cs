using UnityEngine;

[System.Serializable]
public class SliceInfo
{
    public string path;
    public Object file;
    public TextureSettings textureSettings = new TextureSettings(SliceType.Auto);
    public SliceSettings sliceSettings = new SliceSettings(new Vector2(4,4));
}