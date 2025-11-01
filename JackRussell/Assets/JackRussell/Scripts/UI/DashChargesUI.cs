using UnityEngine;
using VContainer;
using VitalRouter;

namespace JackRussell.UI
{
    public class DashChargesUI : MonoBehaviour
    {
        [SerializeField] private GameObject[] _chargeImages;

        [Inject] private readonly ICommandSubscribable _commandSubscribable;

        private void Start()
        {
            _commandSubscribable.Subscribe<DashChargesUpdateCommand>((cmd, ctx) => OnDashChargesUpdate(cmd));
        }

        private void OnDashChargesUpdate(DashChargesUpdateCommand command)
        {
            int charges = command.CurrentCharges;
            for (int i = 0; i < _chargeImages.Length; i++)
            {
                if (_chargeImages[i] != null)
                {
                    _chargeImages[i].SetActive(i < charges);
                }
            }
        }
    }
}