using CardGame.CardObj;
using CardGame.Manager.Main;
using UnityEngine;

namespace CardGame.UI
{
    public class UIHandlers : MonoBehaviour
    {
        private Card card;

        void Awake()
        {
            card = GetComponent<Card>();
        }

        void OnMouseDown()
        {
            if (RitualGameManager.Instance != null)
            {
                RitualGameManager.Instance.OnCardClicked(card);
            }
        }
    }
}
