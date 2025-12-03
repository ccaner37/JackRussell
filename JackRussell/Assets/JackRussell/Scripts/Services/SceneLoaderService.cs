using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace JackRussell
{
    public class SceneLoaderService
    {
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void LoadSceneAdditive(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        public async Task LoadSceneAdditiveAndSetActive(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }

        public void UnloadScene(string sceneName)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
