using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RegisterBattleSfx
{
    [MenuItem("Tools/Register Battle SFX")]
    public static void Execute()
    {
        const string libraryPath = "Assets/Data/SoundLibrary.asset";
        SoundLibrary library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(libraryPath);
        if (library == null)
        {
            Debug.LogError($"SoundLibrary not found at {libraryPath}");
            return;
        }

        var entries = new (string name, string assetPath)[]
        {
            // FX_Player
            ("PlayerMagicianI",     "Assets/Audio/FX_Player/PlayerMagicianI.mp3"),
            ("PlayerHealIII",       "Assets/Audio/FX_Player/PlayerHealIII.mp3"),
            ("PlayerGeneralBuff",   "Assets/Audio/FX_Player/PlayerGeneralBuff.mp3"),
            ("PlayerReviveVI",      "Assets/Audio/FX_Player/PlayerReviveVI.mp3"),
            ("PlayerWarpVII",       "Assets/Audio/FX_Player/PlayerWarpVII.mp3"),
            ("PlayerStrengthVIII",  "Assets/Audio/FX_Player/PlayerStrengthVIII.mp3"),
            ("PlayerEvadeIX",       "Assets/Audio/FX_Player/PlayerEvadeIX.mp3"),
            ("PlayerJusticeDeath1", "Assets/Audio/FX_Player/PlayerJusticeDeath1.mp3"),
            ("PlayerJustice2XI",    "Assets/Audio/FX_Player/PlayerJustice2XI.mp3"),
            ("PlayerDeath2XIII",    "Assets/Audio/FX_Player/PlayerDeath2XIII.mp3"),
            ("PlayerStar1XVII",     "Assets/Audio/FX_Player/PlayerStar1XVII.mp3"),
            ("PlayerStar2XVII",     "Assets/Audio/FX_Player/PlayerStar2XVII.mp3"),
            ("PlayerMoonXVIII",     "Assets/Audio/FX_Player/PlayerMoonXVIII.mp3"),
            ("PlayerSunXIX",        "Assets/Audio/FX_Player/PlayerSunXIX.mp3"),
            ("PlayerTempestXX",     "Assets/Audio/FX_Player/PlayerTempestXX.mp3"),
            ("PlayerWalk",          "Assets/Audio/FX_Player/PlayerWalk.mp3"),
            // FX1.1
            ("CardSelect",          "Assets/Audio/FX1.1/CardSelect.mp3"),
            ("CardShuffle",         "Assets/Audio/FX1.1/CardShuffle.mp3"),
            // Golem (already in Assets/Audio/)
            ("GolemGroundSlam",     "Assets/Audio/GolemGroundSlam.mp3"),
            ("GolemLandBlast",      "Assets/Audio/GolemLandBlast.mp3"),
        };

        SerializedObject so = new SerializedObject(library);
        SerializedProperty sfxProp = so.FindProperty("soundEffects");

        // Build existing name set
        var existing = new Dictionary<string, AudioClip>();
        for (int i = 0; i < sfxProp.arraySize; i++)
        {
            SerializedProperty element = sfxProp.GetArrayElementAtIndex(i);
            string name = element.FindPropertyRelative("soundEffectName").stringValue;
            AudioClip clip = element.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
            if (!string.IsNullOrEmpty(name))
                existing[name] = clip;
        }

        int added = 0;
        foreach ((string name, string assetPath) in entries)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
            {
                Debug.LogWarning($"[RegisterBattleSfx] AudioClip not found: {assetPath}");
                continue;
            }

            if (existing.TryGetValue(name, out AudioClip existingClip))
            {
                if (existingClip != clip)
                    Debug.LogWarning($"[RegisterBattleSfx] '{name}' already registered with different clip.");
                continue;
            }

            int index = sfxProp.arraySize;
            sfxProp.InsertArrayElementAtIndex(index);
            SerializedProperty newElement = sfxProp.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("soundEffectName").stringValue = name;
            newElement.FindPropertyRelative("clip").objectReferenceValue = clip;
            existing[name] = clip;
            added++;
        }

        if (added > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RegisterBattleSfx] {added} new SFX registered.");
        }
        else
        {
            Debug.Log("[RegisterBattleSfx] No new entries to register.");
        }
    }
}
