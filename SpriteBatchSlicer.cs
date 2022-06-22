using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Splices Multiple Sprites At Once
/// </summary>
public class SpriteBatchSlicer : EditorWindow
{
    //Only Detect Pixels With Oppacity Of
    static float Threshold = 1;
    //Sprites Pivot
    static Vector2 Pivot = new Vector2(0.5f, 0.5f);
    //Whether To Auto Close Or Not
    static bool closeOnSlice;

    //Check If ReadWrite Was Enabled
    static bool _readWriteEnabled;

    void OnGUI()
    {
        //Setup GUI
        Threshold = Mathf.Clamp(EditorGUILayout.FloatField("Threshold: ", Threshold), 0, 1);
        Pivot = EditorGUILayout.Vector2Field("Pivot: ", Pivot);
        closeOnSlice = EditorGUILayout.Toggle("Close On Slice: ", closeOnSlice);

        //If read write is enabled on texture2D then Slice
        if (_readWriteEnabled)
        {
            if (GUILayout.Button("Slice"))
            {
                Slice();
                
                if (closeOnSlice)
                    EditorWindow.GetWindow(typeof(SpriteSlicer)).Close();
            }    
        } else
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

            /// Store its pivot
            if (ti.spritesheet.Length > 0)
                Pivot = ti.spritesheet[0].pivot;

            /// Store if its readable
            _readWriteEnabled = ti.isReadable;
        }
    }

    [MenuItem("Sprite Batch Slicer Slicer/Open Slicer")]
    static void OpenSlicer()
    {
        EditorWindow.GetWindow(typeof(SpriteSlicer));
    }

    /// <summary>
    /// Loops througth all seleacted textures and slices them
    /// </summary>
    static void Slice()
    {
        // Get all selected textures
        Object[] spriteSheets = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        // Loop through all selected textures
        for (int z = 0; z < spriteSheets.Length; z++)
        {
            // Get path
            string path = AssetDatabase.GetAssetPath(spriteSheets[z]);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;

            // Set import mode to single
            if (ti.spriteImportMode != SpriteImportMode.Single)
                ti.spriteImportMode = SpriteImportMode.Single;

            //then multiple for slicing
            ti.spriteImportMode = SpriteImportMode.Multiple;

            List<SpriteMetaData> newData = new List<SpriteMetaData>();

            //Convert object to Texture2D
            Texture2D spriteSheet = spriteSheets[z] as Texture2D;

            int maxY = 0;
            int maxX = 0;

            int minY = spriteSheet.height;
            int minX = spriteSheet.width;

            //Loops through texture width and height and gets first and last Non Transparent Pixels Found
            for (int i = 0; i < spriteSheet.width; i++)
            {
                for (int j = spriteSheet.height; j >= 0; j--)
                {
                    if (spriteSheet.GetPixel(i, j).a >= Threshold && j > maxY)
                        maxY = j;

                    if (spriteSheet.GetPixel(i, j).a >= Threshold && j < minY)
                        minY = j;

                    if (spriteSheet.GetPixel(i, j).a >= Threshold && i > maxX)
                        maxX = i;

                    if (spriteSheet.GetPixel(i, j).a >= Threshold && i < minX)
                        minX = i;
                }
            }

            //Setup Sprite Data (Pivot, Name)
            SpriteMetaData smd = new SpriteMetaData();
            smd.pivot = Pivot;
            smd.alignment = 9;
            smd.name = spriteSheet.name;

            //Use minX, maxX and minY, MaxY to create rect around Non Transparent Pixels
            smd.rect = new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);

            //Add new Sprite Data
            newData.Add(smd);
            ti.spritesheet = newData.ToArray();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}