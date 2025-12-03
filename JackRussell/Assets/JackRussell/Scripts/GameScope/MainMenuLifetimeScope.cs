using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JackRussell
{
    public class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenuController _mainMenuController;
         
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MainMenuEntryPoint>();
            builder.RegisterComponent(_mainMenuController);
        }
    }
}
