using JackRussell.Audio;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;

namespace JackRussell.GameScope
{
    public class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] private SoundDatabase _soundDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            //builder.RegisterEntryPoint<RootEntryPoint>();

            builder.Register<AudioManager>(Lifetime.Singleton);

            builder.RegisterInstance(_soundDatabase);
            
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
