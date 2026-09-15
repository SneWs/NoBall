using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NoBall.Editor
{
    [InitializeOnLoad]
    public static class AppIconSetup
    {
        const string IconPath = "Assets/Icons/AppIcon.png";

        static AppIconSetup()
        {
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        [MenuItem("NoBall/Assign App Icon")]
        public static void AssignFromMenu()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogWarning("NoBall app icon missing at " + IconPath);
                return;
            }

            Apply(icon);
        }

        static void ApplyIfNeeded()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
                return;

            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
            var current = PlayerSettings.GetIcons(NamedBuildTarget.Standalone, IconKind.Application);
            if (current != null && sizes != null && current.Length == sizes.Length && current.Length > 0 && current[0] == icon)
                return;

            Apply(icon);
        }

        static void Apply(Texture2D icon)
        {
            Set(NamedBuildTarget.Standalone, icon);
            try
            {
                Set(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Unknown), icon);
            }
            catch (System.Exception)
            {
            }

            Set(NamedBuildTarget.iOS, icon);
            Set(NamedBuildTarget.Android, icon);
            AssetDatabase.SaveAssets();
        }

        static void Set(NamedBuildTarget target, Texture2D icon)
        {
            try
            {
                int[] sizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
                if (sizes == null || sizes.Length == 0)
                    return;
                var icons = new Texture2D[sizes.Length];
                for (int i = 0; i < icons.Length; i++)
                    icons[i] = icon;
                PlayerSettings.SetIcons(target, icons, IconKind.Application);
            }
            catch (System.Exception)
            {
            }
        }
    }
}
