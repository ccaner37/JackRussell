using VContainer;
using VContainer.Unity;

namespace JackRussell
{
    public class GameplayEntryPoint : IStartable
    {
        [Inject] private readonly GameManager _gameManager;

        public void Start()
        {
            // In the future load the game
            _gameManager.Init();
        }
    }
}
