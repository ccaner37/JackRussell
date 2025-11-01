using UnityEngine;

namespace JackRussell.Collectibles
{
    /// <summary>
    /// Abstract base class for collectible items in the game.
    /// Handles trigger detection with player and calls OnCollected when collected.
    /// </summary>
    public abstract class CollectibleItem : MonoBehaviour
    {
        private const string PLAYER_TAG = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                OnCollected();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called when the player collects this item.
        /// Implement specific collection logic in derived classes.
        /// </summary>
        protected abstract void OnCollected();
    }
}