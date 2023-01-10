using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Slices Multiple Sprites At Once
/// </summary>
public class MultiSpriteSlicer : EditorWindow
{

    private enum SliceMode { Single_Automatic, Grid }

    private SliceMode SelectedSliceMode = SliceMode.Single_Automatic;
    private int CellWidth = 40;
    private int CellHeight = 40;
    //Position In Pixels To Start The Grid At
    private Vector2Int cellsStartPosition = new Vector2Int(0, 2040);
    //Only Detect Pixels With Oppacity Of
    private float Threshold = 1;
    //Sprites Pivot
    private Vector2 Pivot = new Vector2(0.5f, 0.5f);
    //Whether To Auto Close Or Not
    private bool closeOnSlice;

    private bool _readWriteEnabled;
    private bool _isSpriteSelected;

    [MenuItem("Multi Sprite Slicer/Open")]
    static void OpenSlicer() => EditorWindow.GetWindow<MultiSpriteSlicer>("Batch Slicer");

    private void OnEnable() => Selection.selectionChanged += Repaint;

    private void OnDisable() => Selection.selectionChanged -= Repaint;

    void OnGUI()
    {
        //Setup GUI
        SelectedSliceMode = (SliceMode)EditorGUILayout.EnumPopup("Slice Mode:", SelectedSliceMode);
        Threshold = Mathf.Clamp(EditorGUILayout.FloatField("Threshold: ", Threshold), 0, 1);

        if (SelectedSliceMode == SliceMode.Grid)
        {
            CellWidth = EditorGUILayout.IntField("Cell Width: ", CellWidth);
            CellHeight = EditorGUILayout.IntField("Cell Height: ", CellHeight);
            cellsStartPosition = EditorGUILayout.Vector2IntField("Cells Start Position: In Pixels", cellsStartPosition);
        }

        Pivot = EditorGUILayout.Vector2Field("Pivot: ", Pivot);
        closeOnSlice = EditorGUILayout.Toggle("Close On Slice: ", closeOnSlice);

        if (!_isSpriteSelected)
        {
            //Message GUI
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("NO SPRITE SELECTED", MessageType.Warning, true);
            return;
        }

        //If read write is enabled on texture2D then Slice
        if (!_readWriteEnabled)
        {
            //Message GUI
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("READ/WRITE IS NOT ENABLED", MessageType.Warning, true);

            //Enable Read Wirte On Texture2D
            if (GUILayout.Button("Enable?"))
            {
                //get all selected tex2Ds
                Object[] tex2Ds = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

                if (tex2Ds.Length > 0)
                {
                    //Set read write enabled on all tex2Ds
                    for (int i = 0; i < tex2Ds.Length; i++)
                    {
                        string path = AssetDatabase.GetAssetPath(tex2Ds[i]);
                        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                        ti.isReadable = true;
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }

                    _readWriteEnabled = true;
                }
            }

            return;
        }

        if (GUILayout.Button("Slice"))
        {
            OnSelectionChange();

            if (!_readWriteEnabled)
                return;

            Slice();

            if (closeOnSlice)
                EditorWindow.GetWindow(typeof(MultiSpriteSlicer)).Close();
        }

        this.Repaint();
    }

    /// <summary>
    /// For GUI
    /// </summary>
    private void OnSelectionChange()
    {
        // Get all selected textures
        Object[] tex2Ds = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        if (tex2Ds.Length > 0)
        {
            /// Get path to first selected texture
            string path = AssetDatabase.GetAssetPath(tex2Ds[0]);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;

            /// Store the first sprite's pivot
            if (ti.spritesheet.Length > 0)
                Pivot = ti.spritesheet[0].pivot;

            /// Store if its readable
            _readWriteEnabled = ti.isReadable;
            _isSpriteSelected = true;
        }
        else
            _isSpriteSelected = false;
    }

    /// <summary>
    /// Loops througth all seleacted textures and slices them
    /// </summary>
    private void Slice()
    {
        // Get all selected textures
        Object[] spriteSheets = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        // Loop through all selected textures
        for (int i = 0; i < spriteSheets.Length; i++)
        {
            // Get path
            string path = AssetDatabase.GetAssetPath(spriteSheets[i]);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;

            // Set import mode to single
            if (ti.spriteImportMode != SpriteImportMode.Single)
                ti.spriteImportMode = SpriteImportMode.Single;

            //then multiple for slicing
            ti.spriteImportMode = SpriteImportMode.Multiple;

            List<SpriteMetaData> newData = new List<SpriteMetaData>();

            //Convert object to Texture2D
            Texture2D spriteSheet = spriteSheets[i] as Texture2D;

            //Current Mode
            switch (SelectedSliceMode)
            {
                case SliceMode.Single_Automatic:

                    int maxY = 0;
                    int maxX = 0;

                    int minY = spriteSheet.height;
                    int minX = spriteSheet.width;

                    //Loops through texture width and height and gets first and last Non Transparent Pixels Found
                    for (int x = 0; x < spriteSheet.width; x++)
                    {
                        for (int y = spriteSheet.height; y >= 0; y--)
                        {
                            if (spriteSheet.GetPixel(x, y).a >= Threshold && y > maxY)
                                maxY = y;

                            if (spriteSheet.GetPixel(x, y).a >= Threshold && y < minY)
                                minY = y;

                            if (spriteSheet.GetPixel(x, y).a >= Threshold && x > maxX)
                                maxX = x;

                            if (spriteSheet.GetPixel(x, y).a >= Threshold && x < minX)
                                minX = x;
                        }
                    }

                    //Setup Sprite Data (Pivot, Name)
                    SpriteMetaData smd1 = new SpriteMetaData();
                    smd1.pivot = Pivot;
                    smd1.alignment = 9;
                    smd1.name = spriteSheet.name;

                    //Use minX, maxX and minY, MaxY to create rect around Non Transparent Pixels
                    smd1.rect = new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);

                    newData.Add(smd1);

                    break;
                    
                case SliceMode.Grid:

                    //For naming each rect
                    var index = 1;

                    //Move in chunks of CellWidth, CellHeight
                    for (int x = Mathf.Clamp(cellsStartPosition.x, 0, spriteSheet.width); x < spriteSheet.width - CellWidth; x += CellWidth)
                    {
                        for (int y = Mathf.Clamp(cellsStartPosition.y, 0, spriteSheet.height - CellHeight); y > 0; y -= CellHeight)
                        {
                            //Get pixels inside each chunk
                            Color[] pixels = spriteSheet.GetPixels(x, y, CellWidth, CellHeight);

                            //See if pixels have a opacity greater than the Threshold
                            if (pixels.Any(p => p.a >= Threshold))
                            {
                                SpriteMetaData smd2 = new SpriteMetaData();
                                smd2.pivot = new Vector2(0.5f, 0.5f);
                                smd2.alignment = 9;
                                smd2.name = $"{spriteSheet.name}_{index}";
                                smd2.rect = new Rect(x, y, CellWidth, CellHeight);
                                newData.Add(smd2);
                                index++;
                            }
                        }
                    }

                    break;
            }

            //Add new Sprite Data
            ti.spritesheet = newData.ToArray();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}