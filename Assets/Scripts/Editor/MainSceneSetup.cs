using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoomDecor.EditorTools
{
    /// <summary>
    /// The app builds its whole scene from code (see DecorApp.Boot), but an
    /// iOS build still needs one scene in Build Settings. This creates an
    /// empty Main scene and adds it the first time the project opens.
    /// </summary>
    [InitializeOnLoad]
    static class MainSceneSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static MainSceneSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorBuildSettings.scenes.Length == 0) CreateMainScene();
            };
        }

        [MenuItem("Room Decor/Set Up Main Scene")]
        static void CreateMainScene()
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            if (EditorBuildSettings.scenes.All(s => s.path != ScenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Append(new EditorBuildSettingsScene(ScenePath, true))
                    .ToArray();
            }
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Room Decor: Main scene is ready. Press Play to try the app.");
        }
    }
}
