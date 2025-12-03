using System.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace JackRussell
{
    public class MainMenuController : MonoBehaviour
    {
        [Inject] private readonly SceneLoaderService _sceneLoaderService;

        public void Init()
        {
            Debug.Log("Main Menu Init");
        }

        public async void OnPlayClicked()
        {
            _sceneLoaderService.UnloadScene("MainMenuScene");
            await _sceneLoaderService.LoadSceneAdditiveAndSetActive("GameplayScene");
        }
    }
}
