using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VitalRouter;

namespace JackRussell.UI
{
    public class PressureBarUI : MonoBehaviour
    {
        [SerializeField] private Image _pressureBarImage;

        [Inject] private readonly ICommandSubscribable _commandSubscribable;

        private void Start()
        {
            _commandSubscribable.Subscribe<PressureUpdateCommand>((cmd, ctx) => OnPressureUpdate(cmd));
        }

        private void OnPressureUpdate(PressureUpdateCommand command)
        {
            if (_pressureBarImage != null)
            {
                _pressureBarImage.fillAmount = command.Pressure / 100f;
            }
        }
    }
}