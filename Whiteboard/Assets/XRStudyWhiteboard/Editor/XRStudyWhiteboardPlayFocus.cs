using UnityEditor;

namespace XRStudyWhiteboard.Editor
{
    /// <summary>
    /// Opens and focuses the Game view when Play mode starts so desktop
    /// keyboard testing receives WASD input immediately.
    /// </summary>
    [InitializeOnLoad]
    internal static class XRStudyWhiteboardPlayFocus
    {
        static XRStudyWhiteboardPlayFocus()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

            EditorApplication.delayCall += FocusGameView;
        }

        private static void FocusGameView()
        {
            System.Type gameViewType = System.Type.GetType("UnityEditor.GameView, UnityEditor");
            if (gameViewType == null)
                return;

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView != null)
                gameView.Focus();
        }
    }
}
