using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NoBall.Editor
{
    [InitializeOnLoad]
    public static class JezzBallSceneBootstrap
    {
        const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        const string GamePath = "Assets/Scenes/Game.unity";

        static JezzBallSceneBootstrap()
        {
            EditorApplication.delayCall += TryCreateMissingScenes;
        }

        [MenuItem("NoBall/Setup Scenes")]
        public static void SetupScenes()
        {
            CreateScene(MainMenuPath, "MainMenu", typeof(MainMenuController));
            CreateScene(GamePath, "Game", typeof(GameController));
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NoBall scenes ready: MainMenu, Game.");
        }

        static void TryCreateMissingScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryCreateMissingScenes;
                return;
            }

            bool missing = !File.Exists(MainMenuPath) || !File.Exists(GamePath);
            if (missing)
                SetupScenes();
            else
                EnsureBuildSettings();
        }

        static void CreateScene(string path, string objectName, System.Type controllerType)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = Path.GetFileNameWithoutExtension(path);

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GameColors.CameraBg;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var extra = cameraGo.GetComponent<UniversalAdditionalCameraData>();
            extra.renderPostProcessing = false;
            extra.renderShadows = false;

            var bootstrap = new GameObject(objectName);
            bootstrap.AddComponent(controllerType);

            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.ImportAsset(path);
            EditorSceneManager.CloseScene(scene, true);
        }

        static void EnsureBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(GamePath, true)
            };
            EditorBuildSettings.scenes = scenes;

            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
            if (menu != null)
                EditorSceneManager.playModeStartScene = menu;
        }
    }
}
