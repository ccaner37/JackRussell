using System.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace JackRussell
{
    public class RootEntryPoint : IStartable
    {
        [Inject] private readonly SceneLoaderService _sceneLoaderService;

        public async void Start()
        {
            await _sceneLoaderService.LoadSceneAdditiveAndSetActive("MainMenuScene");
        }
    }
}
