# UnityUtil-AutoSpriteSlicer

## Intro.

##### * <b>SpriteMetaData</b> to TextureImporter.spritesheet is <b>Obsolte</b>.
##### * In the Unity Eiditor, 'Sprite Slice' is not simple. If they were many Sprites, should slice a lot of borders.
##### * So, I made up Texture Import Settings and Slice Settings on the Editor Window.

## Setup.

### 1. Click the menu which is 'Window / AutoSpriteSlice', Please.
### 2. Drag and drop any texture of the folder to the object field or Input the path which contains textures on the path input field.
### 3. Input texture settings and slice settings.
### 3 - 1. If you want load textures of all paths, could click the button 'Load All Paths'.
### 3 - 2. If you want clear textures of all paths, could click the button 'Clear Loaded Textures'.
### 3 - 3. If you want slice textures of all paths, could click the button 'Slice Textures'.
### 3 - 4. If you want slice each texture of path, could set 'Setting Apply Type' to 'Only This Texture' and then click the button 'Apply Only This Texture'.

## Snippet

```C#
private void Slice(){

                var factory = new SpriteDataProviderFactories();
                factory.Init();
                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(obj);
                dataProvider.InitSpriteEditorDataProvider();
                var textureImporter = (dataProvider.targetObject as TextureImporter);

                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spritePixelsPerUnit = 100;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.SaveAndReimport();
                
                var textureProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
                if (textureProvider != null)
                {
                    int width = 0, height = 0;
                    textureProvider.GetTextureActualWidthAndHeight(out width, out height);
                    //var rect = InternalSpriteUtility.GenerateGridSpriteRectangles(obj as Texture2D, Vector2.zero, new Vector2(64,64), Vector2.zero, true); 
                    var rect = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(obj as Texture2D, width,0);
                    List<SpriteRect> rects = new List<SpriteRect>();
                    for (int i = 0; i < rect.Length; i++)
                    {
                        SpriteRect r = new SpriteRect();

                        r.rect = rect[i];
                        r.alignment = SpriteAlignment.Center;
                        // left , bottom, right, top
                        r.border = new Vector4(width / 3, height / 3, width / 3,height / 3);
                        r.name = $"{obj.name}_{i}";
                        r.pivot = new Vector2(0.5f, 0.5f);
                        r.spriteID = GUID.Generate();
                        rects.Add(r);
                    }
                    dataProvider.SetSpriteRects(rects.ToArray());
                    dataProvider.Apply();
                }

                textureImporter.SaveAndReimport();
}
```

## explain by gif.

![bandicam 2023-01-30 01-58-06-495](https://user-images.githubusercontent.com/123732566/215343356-08556903-6e4d-49d4-9126-b077ddf28093.gif)

![bandicam 2023-01-30 01-59-00-177](https://user-images.githubusercontent.com/123732566/215343466-f37ea38a-ae89-421c-a019-82c2d2ec1c93.gif)

![bandicam 2023-01-30 01-59-23-648](https://user-images.githubusercontent.com/123732566/215343406-38e340ad-bed0-4261-892c-cc90f3b9f63f.gif)

![bandicam 2023-01-30 02-02-54-623](https://user-images.githubusercontent.com/123732566/215343533-df5daa11-7f07-4c3d-b329-7b28ef533414.gif)

