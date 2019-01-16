﻿using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Loom.ZombieBattleground.Editor
{
    public class AotHintFileUpdateAssetPostprocessor : AssetPostprocessor
    {
        private const string MustRegenerateAotHintKey = "ZB_MustRegenerateAotHint";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Any(s => s.EndsWith("zb.cs")))
            {
                EditorPrefs.SetBool(MustRegenerateAotHintKey, true);
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
            if (EditorPrefs.GetBool(MustRegenerateAotHintKey))
            {
                EditorPrefs.SetBool(MustRegenerateAotHintKey, false);
                Debug.Log("zb.cs changed, updating AOT hint");
                AotHintFileUpdater.UpdateAotHint();
            }
        }
    }
}
