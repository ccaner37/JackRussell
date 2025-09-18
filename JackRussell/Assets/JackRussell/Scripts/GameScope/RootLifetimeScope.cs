using JackRussell.Audio;
using JackRussell.States.Action;
using JackRussell.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;

namespace JackRussell.GameScope
{
    public class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] private SoundDatabase _soundDatabase;
        [SerializeField] private GameObject _homingIndicatorPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            //builder.RegisterEntryPoint<RootEntryPoint>();

            builder.Register<AudioManager>(Lifetime.Singleton);

            builder.RegisterInstance(_soundDatabase);

            // Register homing indicator prefab and manager
            builder.RegisterInstance(_homingIndicatorPrefab);
            builder.Register<HomingIndicatorManager>(Lifetime.Singleton);

            // Register UI components
            builder.RegisterComponentInHierarchy<PressureBarUI>();

            // Vital Router //
            builder.RegisterVitalRouter(routing =>
            {
                // No specific routes needed here, services/UI subscribe directly
            });

            // After building the container, set the static resolver accessor
            builder.RegisterBuildCallback(container =>
            {
                //QFSWVContainerResolverAccessor.Resolver = container;
            });
        }
    }
}
