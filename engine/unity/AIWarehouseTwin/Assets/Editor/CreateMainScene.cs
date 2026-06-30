#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
public static class CreateMainScene
{
    [MenuItem("Tools/Create Main Scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var uiDocGo = new GameObject("UIDocument");
        uiDocGo.AddComponent<UIDocument>();
        var playerGo = new GameObject("Player");
        var view = playerGo.AddComponent<AIWarehouseTwin.UI.RunArtifactPlayerView>();
        // Wire UIDocument via SerializedObject
        var so = new UnityEditor.SerializedObject(view);
        var prop = so.FindProperty("_document");
        prop.objectReferenceValue = uiDocGo.GetComponent<UIDocument>();
        so.ApplyModifiedProperties();
        System.IO.Directory.CreateDirectory(
            System.IO.Path.Combine(Application.dataPath, "Scenes"));
        EditorSceneManager.SaveScene(scene,
            "Assets/Scenes/MainScene.unity");
        AssetDatabase.Refresh();
        Debug.Log("MainScene created.");
    }
}
#endif
