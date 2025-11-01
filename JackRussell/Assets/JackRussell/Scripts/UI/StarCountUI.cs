using UnityEngine;
using TMPro;
using VContainer;
using VitalRouter;

namespace JackRussell.UI
{
    public class StarCountUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _starCountText;

        [Inject] private readonly ICommandSubscribable _commandSubscribable;

        private void Start()
        {
            _commandSubscribable.Subscribe<StarCollectedUpdateCommand>((cmd, ctx) => OnStarCollectedUpdate(cmd));
        }

        private void OnStarCollectedUpdate(StarCollectedUpdateCommand command)
        {
            if (_starCountText != null)
            {
                _starCountText.text = command.CollectedCount.ToString();
            }
        }
    }
}