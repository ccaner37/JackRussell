using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JackRussell
{
    public class MainMenuEntryPoint : IStartable
    {
        [Inject] private readonly MainMenuController _mainMenuController;

        public void Start()
        {
            Debug.Log("Main Menu Created");
            // Steam connections in the future etc.
            _mainMenuController.Init();
        }
    }
}
