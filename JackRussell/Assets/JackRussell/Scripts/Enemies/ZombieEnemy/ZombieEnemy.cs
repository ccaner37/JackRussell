using System.Collections;
using JackRussell.Audio;
using JackRussell.Enemies;
using UnityEngine;
using VContainer;

namespace JackRussell
{
    public class ZombieEnemy : Enemy
    {
        [SerializeField] private GameObject _mouthSmokeParticle;
        [Inject] private readonly AudioManager _audioManager;

        public override bool IsActive => IsEnemyActive;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Player>(out Player player))
            {
                if (player.IsSprinting)
                {
                    OnSprintKill(player);
                }
            }
        }

        public override void OnDeath()
        {
            base.OnDeath();
            _mouthSmokeParticle.SetActive(false);
            _audioManager.PlaySound(SoundType.ZombieVaporize);
            StartCoroutine(EnableParticleTest());
        }

        private IEnumerator EnableParticleTest()
        {
            yield return new WaitForSeconds(2f);
            _mouthSmokeParticle.SetActive(true);
        }
    }
}
