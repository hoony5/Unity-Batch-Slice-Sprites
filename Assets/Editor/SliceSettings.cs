using UnityEngine;

[System.Serializable]
public class SliceSettings
{
    public bool keepEmptyRect;
    public int minRectSize;
    public int leftRightBorderController;
    public int topBottomBorderController;
    public SpriteAlignment alignment;
    public Vector2 offset;
    public Vector2 size;
    public Vector2 padding;
    public Vector2 pivot;
    public int top;
    public int bottom;
    public int left;
    public int right;

    public SliceSettings(Vector2 sliceSize)
    {
        minRectSize = (int)sliceSize.x;
        keepEmptyRect = false;
        offset = Vector2.zero;
        pivot = new Vector2(0.5f, 0.5f);
        size = sliceSize;
        alignment = SpriteAlignment.Center;
        padding = Vector2.zero;
        top = 0;
        bottom = 0;
        right = 0;
        left = 0;
        leftRightBorderController = 3;
        topBottomBorderController = 3;
    }
    
}