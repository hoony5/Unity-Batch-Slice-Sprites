using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
public enum SliceType
{
    Auto,
    Grid
}

public class AutoSpriteSlicer : EditorWindow
{
    private static List<Texture2D> _textures = new List<Texture2D>(256);
    
    private static SpriteDataProviderFactories factory;
    private static AutoSpriteSlicer window;
    private ReorderableList _pathList;

    private static Vector2 windowSize = new Vector2(1020, 720);
    private static List<SliceInfo> _sliceInfos = new List<SliceInfo>(16);
    private Vector2 _scrollPos;
    
    private const int SpaceLength = 10;
    private const int SpaceHalfLength = 5;
    private const int FieldWidth = 169;

    private static bool _doNotDraw;
    private static int _currentSliceInfoIndex = -1;
    private static Texture2D _currentSelectedSliceTexture;
    private static int _buildIndex;
    private static string[] _buildTarget = new string[] { "Standalone", "Web", "iPhone", "Android", "WebGL", "Windows Store Apps", "PS4", "PSM", "XboxOne", "Nintendo 3DS" ,"tvOS"};
    private static int _spritePivotIndex = 4;
    private static string[] _spritePivot = new string[] { "Top Left", "Top", "Top Right", "Center Left", "Center", "Center Right", "Bottom Left", "Bottom", "Bottom Right", "Custom"};
    private static int _sliceControlTypeIndex = 0;
    private static string[] _sliceControlType = new string[] { "Easy","Detail" };
    private static int _applySettingTypeIndex = 0;
    private static string[] _applySettingType = new string[] { "Loaded All Textures", "Only This Texture" };
    

    private const string Title = "AutoSpriteSlicer"; 
    private const string PathLabel = "Texture Path"; 
    private const string PathElementLabel = "{0}_Path"; 
    private const string ObejectElementLabel = "Find a path by the file"; 
    private const string GetPathButtonLabelText = "Load Texture"; 
    private const string TextureFilter = "t:Texture"; 
    
    [MenuItem("Window/AutoSpriteSlicer")]
    private static void init()
    {
        // Get existing open window or if none, make a new one:
        window = CreateWindow<AutoSpriteSlicer>(Title, typeof(AutoSpriteSlicer));
        window.minSize = windowSize;
        window.Show();
    }

    private void OnEnable()
    {
        factory = new SpriteDataProviderFactories();
        _pathList = new ReorderableList(_sliceInfos, typeof(string), true,true,true,true);
        _pathList.drawHeaderCallback = drawHeader;
        _pathList.drawElementCallback = drawElement;
        _pathList.drawElementBackgroundCallback = drawBackground;
        _pathList.drawNoneElementCallback = drawNonElement;
        _pathList.multiSelect = true;
        _pathList.headerHeight = _pathList.elementHeight = SpaceLength * 2f;
        _pathList.showDefaultBackground = false;
    }
    private static Vector2 GetSpritePivot(int index)
    {
        switch (index)
        {
            default:
                return new Vector2(0.5f, 0.5f);
            case 0:
                return new Vector2(1f, 0f);
            case 1:
                return new Vector2(1f, 0.5f);
            case 2:
                return new Vector2(1f, 1f);
            case 3:
                return new Vector2(0.5f, 0f);
            case 4:
                return new Vector2(0.5f, 0.5f);
            case 5:
                return new Vector2(0.5f, 1f);
            case 6:
                return new Vector2(0f, 0f);
            case 7:
                return new Vector2(0f, 0.5f);
            case 8:
                return new Vector2(0f, 1f);
        }
    }
    private void OnGUI()
    {
        // Init
        factory.Init();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        space(SpaceLength);
        drawHorizontalLine(2, Color.green);
        space(SpaceLength);
        _pathList.DoLayoutList();
        space(SpaceLength);
        // Event Buttons
        if (_doNotDraw)
        {
            EditorGUILayout.EndVertical();
            return;
        }
        if (_currentSliceInfoIndex is not -1)
        {
            EditorGUILayout.BeginHorizontal();
            space(SpaceHalfLength);
            if (GUILayout.Button("Loaded All Paths", GUILayout.Width(400), GUILayout.Height(24)))
            {
                for (int index = 0; index < _sliceInfos.Count; index++)
                {
                    LoadTexture(index);
                }
            }

            space(SpaceHalfLength);
            if (_applySettingTypeIndex == 0 &&
                GUILayout.Button("Slice Textures", GUILayout.Width(400), GUILayout.Height(24)))
            {
                for (int index = 0; index < _textures.Count; index++)
                {
                    Texture2D texture = _textures[index];
                    ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);
                    dataProvider.InitSpriteEditorDataProvider();
                    Slice(dataProvider, texture, index);
                    dataProvider.Apply();
                }

                AssetDatabase.Refresh();
            }

            space(SpaceHalfLength);
            if (GUILayout.Button("Clear Loaded Textures", GUILayout.Width(400), GUILayout.Height(24)))
            {
                _textures.Clear();
                _currentSelectedSliceTexture = null;
                _currentSliceInfoIndex = -1;
            }

            space(SpaceHalfLength);
            EditorGUILayout.EndHorizontal();

            // label
            space(3 * SpaceLength);
            EditorGUILayout.LabelField("Result List",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 });
            EditorGUILayout.LabelField("▼",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
            space(SpaceLength);
            
            // setting types
            _applySettingTypeIndex = EditorGUILayout.Popup("Setting Apply Type", _applySettingTypeIndex,
                _applySettingType, EditorStyles.popup, GUILayout.Width(400), GUILayout.Height(24));
            space(SpaceLength);
            if(_sliceInfos.Count > _currentSliceInfoIndex && _currentSliceInfoIndex >= 0)
            {
                _sliceInfos[_currentSliceInfoIndex].textureSettings.sliceType
                    = (SliceType)EditorGUILayout.EnumPopup("Slice Type"
                        , _sliceInfos[_currentSliceInfoIndex].textureSettings.sliceType
                        , new GUIStyle(EditorStyles.popup) { alignment = TextAnchor.MiddleCenter }
                        , GUILayout.Width(400), GUILayout.Height(24));
            }
        }
        space(SpaceLength);
        EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUI.skin.scrollView);
        space(SpaceLength);

        if (_textures.Count != 0)
        {
            EditorGUILayout.LabelField("Loaded Textures",
                new GUIStyle(GUI.skin.label)
                    { fontStyle = FontStyle.Bold, fontSize = 14, alignment = TextAnchor.MiddleCenter });

            space(SpaceHalfLength);
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(_scrollPos.x / 2), GUILayout.Height(200));
            EditorGUILayout.BeginVertical();
            space(SpaceHalfLength);
            if (_textures.Count is not 0)
            {
                foreach (Texture2D _texture in _textures)
                {
                    if (GUILayout.Button(
                            $"name : <color=lime>{_texture.name}</color> | Size : <color=lime>({_texture.width},{_texture.height})</color>",
                            new GUIStyle(GUI.skin.button) { richText = true, fontSize = 14 }))
                    {
                        _currentSelectedSliceTexture = _texture;
                        Selection.SetActiveObjectWithContext(_texture, _texture);
                    }
                }
            }

            space(SpaceHalfLength);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        space(SpaceLength);
        
        EditorGUILayout.BeginVertical();
        if (_currentSliceInfoIndex is not -1)
        {
            drawSilceSettings(_currentSliceInfoIndex);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();
        if (_currentSliceInfoIndex is not -1)
        {
            drawTextureSettings(_currentSliceInfoIndex);
        }
        space(SpaceLength);
        EditorGUILayout.EndHorizontal();
        space(SpaceLength);
        EditorGUILayout.EndVertical();
    }

    #region draw list
    private static void drawNonElement(Rect rect)
    {
        _doNotDraw = true;
        _currentSelectedSliceTexture = null;
        _currentSliceInfoIndex = -1;
        _textures.Clear();
    }

    private static void drawBackground(Rect rect, int index, bool isactive, bool isfocused)
    {
        float value = 0.15f;
        if (index % 2 == 1) value = 0.125f; 
        if (isactive)
        {
            EditorGUI.DrawRect(rect, new Color(0,0.5f - value,0.5f - value,1));
            return;
        }
        EditorGUI.DrawRect(rect, new Color(value,value,value,1));
    }

    private void drawSilceSettings(int index)
    {
        Color boxColor = new Color(0.25f, 0.25f, 0.25f, 1);
        Rect settingHeaderAreaRect = EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.DrawRect(settingHeaderAreaRect, boxColor);
        EditorGUILayout.LabelField("Texture Slice Settings", new GUIStyle(GUI.skin.label){fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter});
        space(SpaceHalfLength);
        EditorGUILayout.LabelField("Normal Settings", EditorStyles.boldLabel);
        space(SpaceHalfLength);
            Rect sliceSettingAreaRect = EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.DrawRect(sliceSettingAreaRect, boxColor);
        space(SpaceHalfLength);
        _sliceControlTypeIndex = EditorGUILayout.Popup("Control Type", _sliceControlTypeIndex,_sliceControlType, EditorStyles.popup);
        space(SpaceHalfLength);
        _sliceInfos[index].sliceSettings.alignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Alignment", _sliceInfos[index].sliceSettings.alignment);
        space(SpaceHalfLength);
        
        if (_sliceInfos[index].textureSettings.sliceType is SliceType.Auto)
        {
            _sliceInfos[index].sliceSettings.minRectSize = EditorGUILayout.IntField("Minimum Rect Size", _sliceInfos[index].sliceSettings.minRectSize < 0 ? 0 : _sliceInfos[index].sliceSettings.minRectSize);
        }
        else
        {
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.keepEmptyRect = EditorGUILayout.Toggle("Keep Empty Rect", _sliceInfos[index].sliceSettings.keepEmptyRect);
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.pivot = EditorGUILayout.Vector2Field("Pivot", _sliceInfos[index].sliceSettings.pivot);
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.padding = EditorGUILayout.Vector2Field("Padding", _sliceInfos[index].sliceSettings.padding);
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.offset = EditorGUILayout.Vector2Field("Offset", _sliceInfos[index].sliceSettings.offset);
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.size = EditorGUILayout.Vector2Field("Grid Rect Size", _sliceInfos[index].sliceSettings.size);
        }

        space(SpaceHalfLength);
        if (_sliceControlTypeIndex == 0)
        {
            _sliceInfos[index].sliceSettings.leftRightBorderController = EditorGUILayout.IntSlider("Left Right Slice Controller", _sliceInfos[index].sliceSettings.leftRightBorderController, 1 ,10);
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.topBottomBorderController = EditorGUILayout.IntSlider("Top Bottom Slice Controller", _sliceInfos[index].sliceSettings.topBottomBorderController, 1 , 10);
            space(SpaceHalfLength);
            if(_currentSelectedSliceTexture)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                int width = _currentSelectedSliceTexture.width /
                            _sliceInfos[index].sliceSettings.leftRightBorderController;
                int height = _currentSelectedSliceTexture.height /
                             _sliceInfos[index].sliceSettings.topBottomBorderController;
                EditorGUILayout.LabelField($"Border Left : {width}");
                EditorGUILayout.LabelField($"Border Right : {width}");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Border Top : {height}");
                EditorGUILayout.LabelField($"Border Bottom : {height}");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            space(SpaceHalfLength);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();    
            EditorGUILayout.BeginVertical();
            _sliceInfos[index].sliceSettings.top = EditorGUILayout.IntField("Top Border", Mathf.Clamp(_sliceInfos[index].sliceSettings.top, 0, 100));
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.bottom =EditorGUILayout.IntField("Bottom Border",Mathf.Clamp(_sliceInfos[index].sliceSettings.bottom, 0,100));
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();    
            _sliceInfos[index].sliceSettings.left =EditorGUILayout.IntField("Left Border", Mathf.Clamp(_sliceInfos[index].sliceSettings.left, 0, 100));
            space(SpaceHalfLength);
            _sliceInfos[index].sliceSettings.right =  EditorGUILayout.IntField("Right Border", Mathf.Clamp(_sliceInfos[index].sliceSettings.right, 0, 100));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        
        space(SpaceHalfLength);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();
        space(SpaceLength);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Slice Preview", new GUIStyle(GUI.skin.label){fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft});
        space(25 * SpaceHalfLength);
        Rect previewRect = EditorGUILayout.BeginVertical();
        space(5 * SpaceHalfLength);

        drawPreviewSlicedTexture(previewRect, index);
        
        space(5 * SpaceHalfLength);
        EditorGUILayout.EndVertical();
        space(10 * SpaceHalfLength);
        EditorGUILayout.BeginVertical();
        if (_applySettingTypeIndex == 1)
        {
            if (GUILayout.Button("Apply Only This Texture", GUILayout.Width(150), GUILayout.Height(24)) && _currentSelectedSliceTexture is not null)
            {
                ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(_currentSelectedSliceTexture);
                dataProvider.InitSpriteEditorDataProvider();
                Slice(dataProvider, _currentSelectedSliceTexture, _currentSliceInfoIndex);
                AssetDatabase.Refresh();
                dataProvider.Apply();
            }
        }
        EditorGUILayout.EndVertical();
        space(SpaceLength);
        EditorGUILayout.EndHorizontal();
    }

    private void drawPreviewSlicedTexture(Rect previewRect, int index)
    {
        int previewWidth = 100;
        int previewHeight = 100;
        int previewSliceLineThick = 2;  
                
        if(_currentSelectedSliceTexture is null)
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength, previewRect.y, previewWidth, previewHeight), new Color(0.15f, 0.15f, 0.15f, 1)); // base
        else
            GUI.DrawTexture(new Rect(previewRect.x + SpaceLength, previewRect.y, previewWidth, previewHeight),_currentSelectedSliceTexture as Texture2D); // base
        
        // easy
        if (_sliceControlTypeIndex == 0)
        {
            int divideWidthLine = previewWidth / _sliceInfos[index].sliceSettings.leftRightBorderController;
            int divideHeightLine = previewHeight / _sliceInfos[index].sliceSettings.topBottomBorderController;
        
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength , previewRect.y + divideHeightLine, previewWidth, previewSliceLineThick) , Color.green); // top
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength, previewRect.y + previewHeight - divideHeightLine, previewWidth, previewSliceLineThick) , Color.green); // bottom
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength - divideWidthLine + previewWidth, previewRect.y , previewSliceLineThick, previewHeight) , Color.green); // right
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength + divideWidthLine, previewRect.y, previewSliceLineThick, previewHeight) , Color.green); // left
        }
        // detail
        else
        {
            int leftLine = _sliceInfos[index].sliceSettings.left;
            int rightLine = _sliceInfos[index].sliceSettings.right;
            int bottomLine = _sliceInfos[index].sliceSettings.bottom;
            int topLine = _sliceInfos[index].sliceSettings.top;
            
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength , previewRect.y + topLine, previewWidth, previewSliceLineThick) , Color.green); // top
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength, previewRect.y + previewHeight - bottomLine, previewWidth, previewSliceLineThick) , Color.green); // bottom
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength - rightLine + previewWidth, previewRect.y , previewSliceLineThick, previewHeight) , Color.green); // right
            EditorGUI.DrawRect(new Rect(previewRect.x + SpaceLength + leftLine, previewRect.y, previewSliceLineThick, previewHeight) , Color.green); // left
        }
    }
    private void drawTextureSettings(int index)
    {
        Color boxColor = new Color(0.25f, 0.25f, 0.25f, 1);
        Rect settingHeaderAreaRect = EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.DrawRect(settingHeaderAreaRect, boxColor);
        EditorGUILayout.LabelField("Texture Import Settings", new GUIStyle(GUI.skin.label){fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter});
        space(SpaceHalfLength);
        EditorGUILayout.LabelField("Normal Settings", EditorStyles.boldLabel);
        space(SpaceHalfLength);
        // normal Settings
            Rect importSettingAreaRect = EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUI.DrawRect(importSettingAreaRect, boxColor);
        EditorGUILayout.BeginVertical();
        space(SpaceHalfLength);
        drawTextureImportSettings(index);
        space(SpaceHalfLength);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        // platform Settings
        Rect platformSettingAreaRect = EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUI.DrawRect(platformSettingAreaRect, boxColor);
        EditorGUILayout.BeginVertical();
        space(SpaceHalfLength);
        
        drawPlatformSettings(index);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void drawTextureImportSettings(int index)
    {
        _sliceInfos[index].textureSettings.importMode = (SpriteImportMode)EditorGUILayout.EnumPopup("Import Mode", _sliceInfos[index].textureSettings.importMode);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.meshType = (SpriteMeshType)EditorGUILayout.EnumPopup("Mesh Type", _sliceInfos[index].textureSettings.meshType);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.spritePixelsPerUnit = EditorGUILayout.IntField("Pixel Per Unit", _sliceInfos[index].textureSettings.spritePixelsPerUnit);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.extrudeSize = EditorGUILayout.IntSlider("Extrude Size", _sliceInfos[index].textureSettings.extrudeSize, 0, 32);
        space(SpaceHalfLength);
        _spritePivotIndex = EditorGUILayout.Popup("Sprite Pivot", _spritePivotIndex, _spritePivot, EditorStyles.popup);
        _sliceInfos[index].textureSettings.spritePivot = GetSpritePivot(_spritePivotIndex);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.sRGBTexture = EditorGUILayout.Toggle("sRGBTexture", _sliceInfos[index].textureSettings.sRGBTexture);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.alphaSource = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup("Alpha Source", _sliceInfos[index].textureSettings.alphaSource);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.alphaIsTransparency = EditorGUILayout.Toggle("Alpha Is Transparency", _sliceInfos[index].textureSettings.alphaIsTransparency);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", _sliceInfos[index].textureSettings.wrapMode);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", _sliceInfos[index].textureSettings.filterMode);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.anisoLevel = EditorGUILayout.IntSlider("Aniso Level", _sliceInfos[index].textureSettings.anisoLevel, 0, 16);
    }
    private void drawPlatformSettings(int index)
    {
        EditorGUILayout.LabelField("Platform Settings", EditorStyles.boldLabel);
        space(SpaceHalfLength);
        _buildIndex = EditorGUILayout.Popup("Build Target", _buildIndex, _buildTarget, EditorStyles.popup);
        _sliceInfos[index].textureSettings.buildTarget = _buildTarget[_buildIndex];
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.maxTextureSize = EditorGUILayout.IntField("Max Size", _sliceInfos[index].textureSettings.maxTextureSize);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.resizeType = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", _sliceInfos[index].textureSettings.resizeType);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.importerFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", _sliceInfos[index].textureSettings.importerFormat);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.compressionType = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", _sliceInfos[index].textureSettings.compressionType);
        space(SpaceHalfLength);
        _sliceInfos[index].textureSettings.useCrunchCompression = EditorGUILayout.Toggle("Use Crunch Compression", _sliceInfos[index].textureSettings.useCrunchCompression);
        if (_sliceInfos[index].textureSettings.useCrunchCompression)
        {
            space(SpaceHalfLength);
            _sliceInfos[index].textureSettings.compressionQuality = EditorGUILayout.IntSlider("Compression Quality", _sliceInfos[index].textureSettings.compressionQuality, 0, 100);
            space(SpaceHalfLength);
        }
    }
    private static void drawHeader(Rect rect)
    {
        EditorGUI.DrawRect(rect,new Color(0.1f,0.1f, 0.1f ,1));
        EditorGUI.LabelField(rect, PathLabel, new GUIStyle(GUI.skin.label){alignment = TextAnchor.MiddleLeft, fontSize = 14});
    }
    private static void drawElement(Rect rect, int index, bool isactive, bool isfocused)
    {
        _doNotDraw = false;
        _sliceInfos[index].path = EditorGUI.TextField(
            new Rect(rect.position
                , new Vector2(2.5f * FieldWidth, EditorGUIUtility.singleLineHeight))
            , string.Format(PathElementLabel, index)
            , _sliceInfos[index].path
            , EditorStyles.textField);
        
        _sliceInfos[index].file = EditorGUI.ObjectField(
            new Rect(rect.position.x + SpaceLength + 2.75f * FieldWidth, rect.position.y
                , 2 * FieldWidth, EditorGUIUtility.singleLineHeight)
            , ObejectElementLabel
            , _sliceInfos[index].file
            , typeof(Texture2D)
            , false);
        
        if (GUI.Button(
                new Rect
                (rect.position.x + 5f * (SpaceLength + FieldWidth)
                    , rect.position.y
                    , FieldWidth
                    , EditorGUIUtility.singleLineHeight),
                GetPathButtonLabelText))
        {
            LoadTexture(index);
        }
    }

    private static void LoadTexture(int index)
    {
        string fullPath = null;
        int fileNameIndex;
        string[] textureGuids;
        // if object field is null
        if (!string.IsNullOrEmpty(_sliceInfos[index].path))
        {
            textureGuids = AssetDatabase.FindAssets(TextureFilter, new[] {_sliceInfos[index].path});
        }
        else if (_sliceInfos[index].file is null)
        {
            EditorUtility.DisplayDialog("There is no file.",
                $"If you want to find the path which is '{index}_Path', should assign file to 'ObjectField'", "OK");
            return;
        }
        else
        {
            // process path to texture
            fullPath = AssetDatabase.GetAssetPath(_sliceInfos[index].file);
            fileNameIndex = fullPath.IndexOf($"/{_sliceInfos[index].file.name}");
            _sliceInfos[index].path = fullPath.Remove(fileNameIndex);
            textureGuids = AssetDatabase.FindAssets(TextureFilter, new[] {_sliceInfos[index].path});
        }
        // if there was no files
        if (textureGuids.Length is 0)
        {
            EditorUtility.DisplayDialog("There is no files.",
                $"If you want to find the files which is 'Texture Type' should assign file to 'ObjectField'", "OK");
            return;
        }
            
        // else
        foreach (string guid in textureGuids)
        {
            string uniquePath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath(uniquePath, typeof(Texture2D)) as Texture2D;
                
            if(_textures.Contains(texture)) continue;

            _textures.Add(texture);
        }
        if(_textures.Count != 0)
            _currentSelectedSliceTexture = _textures[0];

        _currentSliceInfoIndex = index;
        Debug.Log($" Load Success({index}) : current / total Loaded Textures ---> {textureGuids.Length} / {_textures.Count}");
    }
    private void space(int space) => EditorGUILayout.Space(space);

    private void drawHorizontalLine(int thick, Color color)
    {
        Rect current = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(new Rect(current.position,new Vector2(current.width, thick)), color);
        EditorGUILayout.EndVertical();
    }
    #endregion

    private TextureImporter SetTextureImporter(ISpriteEditorDataProvider dataProvider,TextureSettings textureSettings)
    {
        TextureImporter textureImporter = dataProvider.targetObject as TextureImporter;
        textureImporter.spriteImportMode = textureSettings.importMode;
        textureImporter.anisoLevel = textureSettings.anisoLevel;
        textureImporter.spriteImportMode = textureSettings.importMode;
        textureImporter.anisoLevel = textureSettings.anisoLevel;

        // apply Settings
        TextureImporterSettings importerSettings = new TextureImporterSettings();
        importerSettings.readable = true;
        textureImporter.ReadTextureSettings(importerSettings); // for hidden property values
        importerSettings.textureType = textureSettings.textureType;
        importerSettings.spriteMeshType = textureSettings.meshType;
        importerSettings.spritePixelsPerUnit = textureSettings.spritePixelsPerUnit;
        importerSettings.wrapMode = textureSettings.wrapMode;
        importerSettings.filterMode = textureSettings.filterMode;
        importerSettings.spritePivot = textureSettings.spritePivot;
        importerSettings.alphaSource = textureSettings.alphaSource;
        importerSettings.sRGBTexture = textureSettings.sRGBTexture;
        importerSettings.alphaIsTransparency = textureSettings.alphaIsTransparency;
        // platform Settings
        TextureImporterPlatformSettings platformSettings = textureImporter.GetPlatformTextureSettings(_buildTarget[_buildIndex]);
        platformSettings.maxTextureSize = textureSettings.maxTextureSize;
        platformSettings.compressionQuality = textureSettings.compressionQuality;
        platformSettings.format = textureSettings.importerFormat;
        platformSettings.resizeAlgorithm = textureSettings.resizeType;
        platformSettings.textureCompression = textureSettings.compressionType;
        platformSettings.crunchedCompression = textureSettings.useCrunchCompression;
        platformSettings.overridden = textureSettings.overriden;
        platformSettings.name = textureSettings.buildTarget;
        textureImporter.SetPlatformTextureSettings(platformSettings);
        textureImporter.SetTextureSettings(importerSettings);
        return textureImporter;
    }

    private Rect[] GetRectangles(ISpriteEditorDataProvider dataProvider,Texture2D texture, TextureSettings textureSettings, SliceSettings sliceSettings)
    {
        ITextureDataProvider textureProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
        if (textureProvider is null) return null;
        int width = 0;
        int height = 0;
        textureProvider.GetTextureActualWidthAndHeight(out width, out height);

        if (textureSettings.importMode is SpriteImportMode.Single)
        {
            sliceSettings.minRectSize = texture.width > texture.height ? texture.width : texture.height;
        }
        // Slice Rect
        Rect[] rectangles = textureSettings.sliceType switch
        {
            SliceType.Auto => InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, sliceSettings.minRectSize,
                textureSettings.extrudeSize),
            SliceType.Grid => InternalSpriteUtility.GenerateGridSpriteRectangles(texture,
                sliceSettings.offset, sliceSettings.size, sliceSettings.padding, sliceSettings.keepEmptyRect),
            _ => InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, sliceSettings.minRectSize,
                textureSettings.extrudeSize),
        };
        return rectangles;
    }

    private SpriteRect[] SetSpriteRects(Texture2D texture, Rect[] rectangles, SliceSettings sliceSettings)
    {
        int length = rectangles.Length;
        List<SpriteRect> rects = new List<SpriteRect>(length == 0 ? 1 : length);

        if (length == 0)
        {
            SpriteRect r = CreateSpriteRect($"{texture.name}", new Rect(0, 0, texture.width, texture.height),
                sliceSettings);
            rects.Add(r);
            return rects.ToArray();
        }
        
        for (int i = 0; i < length; i++)
        {
            SpriteRect r = CreateSpriteRect($"{texture.name}_{i}", rectangles[i], sliceSettings);
            rects.Add(r);
        }

        return rects.ToArray();
    }

    private SpriteRect CreateSpriteRect(string name, Rect rect, SliceSettings sliceSettings)
    {
        SpriteRect r = new SpriteRect();

        r.rect = rect;
        r.alignment = sliceSettings.alignment;

        if (_sliceControlTypeIndex == 0)
        {
            int divideWidth = (int)rect.width / sliceSettings.leftRightBorderController;
            int divideHeight = (int)rect.height / sliceSettings.topBottomBorderController;

            r.border = new Vector4(divideWidth, divideHeight, divideWidth, divideHeight);
        }
        else
        {
            r.border = new Vector4(sliceSettings.left, sliceSettings.bottom, sliceSettings.right,
                sliceSettings.top);
        }
        // left , bottom, right, top

        r.name = name;
        r.pivot = sliceSettings.pivot;
        r.spriteID = GUID.Generate();
        return r;
    }
    private void Slice(ISpriteEditorDataProvider dataProvider, Texture2D texture, int index)
    {
        TextureSettings textureSettings = _sliceInfos[index].textureSettings;
        SliceSettings sliceSettings = _sliceInfos[index].sliceSettings;
        TextureImporter textureImporter = SetTextureImporter(dataProvider, textureSettings);
        
        dataProvider.Apply();
        textureImporter.SaveAndReimport();

        // ISpritePhysicsOutlineDataProvider physicsOutlineDataProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        //var rects = dataProvider.GetSpriteRects();
        //foreach (var rect in rects)
        //{
        //    var outlines = physicsOutlineDataProvider.GetOutlines(rect.spriteID);
        //    // Do changes
        //    physicsOutlineDataProvider.SetOutlines(rect.spriteID, outlines);
        //}

        // Get Texture Data
        // Slice Rect
        Rect[] rectangles = GetRectangles(dataProvider, texture, textureSettings, sliceSettings);
        // Other Slice Settings
        SpriteRect[] rects = SetSpriteRects(texture, rectangles, sliceSettings);
        // Apply
        dataProvider.SetSpriteRects(rects);
        dataProvider.Apply();
        textureImporter.SaveAndReimport();
    }

}