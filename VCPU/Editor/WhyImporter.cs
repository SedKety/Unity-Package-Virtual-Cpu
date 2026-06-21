using UnityEditor;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "why")]
public class WhyImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string text = File.ReadAllText(ctx.assetPath);

        TextAsset asset = new TextAsset(text);

        ctx.AddObjectToAsset("main", asset);
        ctx.SetMainObject(asset);
    }
}