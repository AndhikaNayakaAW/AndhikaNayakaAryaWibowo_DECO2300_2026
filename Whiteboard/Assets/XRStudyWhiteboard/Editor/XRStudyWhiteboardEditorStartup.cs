using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace XRStudyWhiteboard.Editor
{
    [InitializeOnLoad]
    internal static class XRStudyWhiteboardEditorStartup
    {
        private const string MainScenePath = "Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity";

        static XRStudyWhiteboardEditorStartup()
        {
            EditorApplication.delayCall += OpenMainSceneWhenUntitled;
        }

        private static void OpenMainSceneWhenUntitled()
        {
            EditorApplication.delayCall -= OpenMainSceneWhenUntitled;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path) || !File.Exists(MainScenePath))
                return;

            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }
    }
}
