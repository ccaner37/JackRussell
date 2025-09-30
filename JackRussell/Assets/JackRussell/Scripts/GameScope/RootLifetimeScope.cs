using JackRussell.Audio;
using JackRussell.States.Action;
using JackRussell.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;
using JackRussell;

namespace JackRussell.GameScope
{
    public class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] private SoundDatabase _soundDatabase;
        [SerializeField] private GameObject _homingIndicatorPrefab;
        [SerializeField] private HomingExitAnimationConfig _homingExitConfig;
        [SerializeField] private AudioManager _audioManager;

        protected override void Configure(IContainerBuilder builder)
        {
            //builder.RegisterEntryPoint<RootEntryPoint>();

            builder.RegisterComponent(_audioManager);

            builder.RegisterInstance(_soundDatabase);

            // Register homing exit animation config
            builder.RegisterInstance(_homingExitConfig);

            // Register homing indicator prefab and manager
            builder.RegisterInstance(_homingIndicatorPrefab);
            builder.Register<HomingIndicatorManager>(Lifetime.Singleton);

            // Register UI components
            builder.RegisterComponentInHierarchy<PressureBarUI>();

            // Register renderer controller
            builder.RegisterComponentInHierarchy<RendererController>();

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
