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
        // UIDocument GameObject
        var uiDocGo = new GameObject("UIDocument");
        var uiDoc = uiDocGo.AddComponent<UIDocument>();
        // Bind UXML asset
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI/RunArtifactPlayerView.uxml");
        if (uxml == null)
            throw new System.Exception(
                "RunArtifactPlayerView.uxml not found at Assets/UI/RunArtifactPlayerView.uxml");
        uiDoc.visualTreeAsset = uxml;
        // Player GameObject
        var playerGo = new GameObject("Player");
        var view = playerGo.AddComponent<AIWarehouseTwin.UI.RunArtifactPlayerView>();
        // Wire _document field
        var so = new SerializedObject(view);
        var prop = so.FindProperty("_document");
        prop.objectReferenceValue = uiDoc;
        so.ApplyModifiedProperties();
        // Save scene
        System.IO.Directory.CreateDirectory(
            System.IO.Path.Combine(Application.dataPath, "Scenes"));
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
        AssetDatabase.Refresh();
        Debug.Log("MainScene created with UXML binding.");
    }
}
#endif
