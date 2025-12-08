using CardGame.Manager.Main;
using UnityEngine;
using static CardGame.Structures.Structures;

namespace CardGame.UI.DomainPileClick
{
    public class DomainPileClickHandler : MonoBehaviour
    {
        public CardDomain domain;

        void OnMouseDown()
        {
            if (RitualGameManager.Instance != null)
            {
                RitualGameManager.Instance.OnDomainPileClicked(domain);
            }
        }
    }
}
