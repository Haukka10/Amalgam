using CardGame.Board.Slot;
using CardGame.Manager.Main;
using UnityEngine;

namespace CardGame.UI.SlotClick
{
    public class SlotClickHandler : MonoBehaviour
    {
        private BoardSlot slot;

        void Awake()
        {
            slot = GetComponent<BoardSlot>();
        }

        void OnMouseDown()
        {
            if (RitualGameManager.Instance != null)
            {
                RitualGameManager.Instance.OnSlotClicked(slot);
            }
        }
    }
}
