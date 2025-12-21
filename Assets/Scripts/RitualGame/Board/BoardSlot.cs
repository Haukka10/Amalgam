using CardGame.CardObj;
using CardGame.Manager.Main;
using UnityEngine;
using UnityEngine.UI;
using static CardGame.Structures.Structures;


namespace CardGame.Board.Slot
{
    public class BoardSlot : MonoBehaviour
    {
        public SlotType slotType;
        public Player owner;
        public Card currentCard;

        private Image highlightImage;

        void Awake()
        {
            highlightImage = GetComponent<Image>();
        }

        public bool CanPlaceCard(Card card)
        {
            if (currentCard != null) return false;

            if(owner == Player.AI && (slotType == SlotType.FRONT || slotType == SlotType.BATTLEFIELD))
                return false;

            if (owner == Player.Human && (slotType == SlotType.FRONT || slotType == SlotType.BATTLEFIELD))
                return false;

            if (card.data.cardType == CardType.Modifier && slotType != SlotType.MOD)
                return false;

            if (card.data.cardType == CardType.Normal && slotType == SlotType.MOD)
                return false;

            return true;
        }

        public void PlaceCard(Card card)
        {
            currentCard = card;
            card.transform.SetParent(transform);
            card.transform.localPosition = Vector3.zero;

            // Ukryj kartę jeśli jest na BACK
            if (slotType == SlotType.BACK)
            {
                card.SetHidden(true);
            }
            else
            {
                card.SetHidden(false);
            }

            // Trigger ability on placement
            card.TriggerAbility("OnPlaced", this);
        }

        public Card RemoveCard()
        {
            Card card = currentCard;
            currentCard = null;

            // Odkryj kartę gdy opuszcza BACK
            if (card != null)
            {
                card.SetHidden(false);
            }

            return card;
        }

        public void SetHighlight(bool active)
        {
            if (highlightImage)
                highlightImage.color = active ? new Color(1, 1, 0, 0.3f) : Color.clear;
        }
    }
}
