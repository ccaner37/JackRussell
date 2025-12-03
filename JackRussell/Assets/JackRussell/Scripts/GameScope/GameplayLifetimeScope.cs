using JackRussell.UI;
using VContainer;
using VContainer.Unity;

namespace JackRussell
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GameManager>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameplayEntryPoint>();

            // Register UI components
            builder.RegisterComponentInHierarchy<PressureBarUI>();
            builder.RegisterComponentInHierarchy<ParticleEffectUI>();
        }
    }
}
