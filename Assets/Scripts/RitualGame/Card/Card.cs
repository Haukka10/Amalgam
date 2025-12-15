using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

using CardGame.Board.Slot;
using static CardGame.Structures.Structures;

namespace CardGame.CardObj
{
    public class Card : MonoBehaviour
    {
        public CardData data;
        public int currentPower;
        public Player owner;
        public bool isHidden = false;

        public Image artworkImage;
        public Text powerText;
        public Text nameText;
        public Image cardBack;
        private CanvasGroup canvasGroup;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (!canvasGroup) 
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(CardData cardData, Player cardOwner)
        {
            data = cardData;
            owner = cardOwner;
            currentPower = data.basePower;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (isHidden)
            {
                // Pokaż rewers karty
                if (artworkImage) 
                    artworkImage.enabled = false;

                if (powerText) 
                    powerText.enabled = false;

                if (nameText) 
                    nameText.enabled = false;

                if (cardBack) 
                    cardBack.enabled = true;
            }
            else
            {
                // Pokaż awers karty
                if (artworkImage)
                {
                    artworkImage.enabled = true;
                    artworkImage.sprite = data.artwork;
                }
                if (powerText)
                {
                    powerText.enabled = true;
                    powerText.text = currentPower.ToString();
                }
                if (nameText)
                {
                    nameText.enabled = true;
                    nameText.text = data.cardName;
                }
                if (cardBack) cardBack.enabled = false;
            }
        }

        public void SetHidden(bool hidden)
        {
            isHidden = hidden;
            UpdateVisuals();
        }

        // Wywołanie zdolności karty przez Visual Scripting
        public void TriggerAbility(string eventName, object target = null)
        {
            if (data.abilityGraph != null)
            {
                CustomEvent.Trigger(gameObject, eventName, target, this);
            }
        }

        public void ApplyModifier(int powerChange)
        {
            currentPower += powerChange;
            UpdateVisuals();
        }
        
        // Zwraca prawdziwą moc karty (z bonusem +1 jeśli jest na BACK)
        public int GetEffectivePower()
        {
            int power = currentPower;

            // Sprawdź czy karta jest na BACK
            BoardSlot parentSlot = GetComponentInParent<BoardSlot>();
            if (parentSlot != null && parentSlot.slotType == SlotType.BACK)
            {
                power += 1; // Bonus +1 na BACK
            }

            return power;
        }
    }
}
