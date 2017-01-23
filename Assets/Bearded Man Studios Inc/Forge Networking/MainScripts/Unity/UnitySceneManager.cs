
namespace BeardedManStudios.Network.Unity
{
	/// <summary>
	/// This Class abstracts the new SceneManager to guarantee compatability with
	/// older Unity versions before Unity 5.3.
	/// </summary>
	public static class UnitySceneManager
	{
		/// <summary>
		/// Loads the scene by its name or index.
		/// </summary>
		/// <param name="sceneName">Name of the scene.</param>
		public static void LoadScene(string sceneName)
		{
#if UNITY_5_3
			UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
#else
			UnityEngine.Application.LoadLevel(sceneName);
#endif
		}

		/// <summary>
		/// Loads the scene by its name or index.
		/// </summary>
		/// <param name="sceneBuildIndex">BuildIndex of the scene.</param>
		public static void LoadScene(int sceneBuildIndex)
		{
#if UNITY_5_3
			UnityEngine.SceneManagement.SceneManager.LoadScene(sceneBuildIndex);
#else
			UnityEngine.Application.LoadLevel(sceneBuildIndex);
#endif
		}

		/// <summary>
		/// The scene index that was last loaded.
		/// </summary>
		/// <returns>The index of the last loaded scene.</returns>
		public static int GetCurrentSceneBuildIndex()
		{
#if UNITY_5_3
			return UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
#else
			return UnityEngine.Application.loadedLevel;
#endif
		}

		/// <summary>
		/// The name of the scene that was last loaded
		/// </summary>
		/// <returns>The name of the last loaded scene.</returns>
		public static string GetCurrentSceneName()
		{
#if UNITY_5_3
			return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
#else
			return UnityEngine.Application.loadedLevelName;
#endif
		}

#if UNITY_EDITOR        
		/// <summary>
		/// The path of the scene that the user has currently open
		/// (Will be an empty string if no scene is currently open).
		/// (Editor Only)
		/// </summary>
		/// <returns>The name of the current scene.</returns>
		public static string GetCurrentEditorSceneName()
		{
#if UNITY_5_3
			return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
#else
			return UnityEditor.EditorApplication.currentScene;
#endif
		}
#endif
	}
}
