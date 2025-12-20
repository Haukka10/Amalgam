using CardGame.Board.Slot;
using CardGame.CardObj;
using System.Collections.Generic;
using UnityEngine;
using static CardGame.Structures.Structures;

namespace CardGame.Board.Lane
{
    public class PlayerLane : MonoBehaviour
    {
        public Player owner;
        public BoardSlot backSlot;
        public BoardSlot modSlot1;
        public BoardSlot midSlot;
        public BoardSlot modSlot2;
        public BoardSlot frontSlot;

        [HideInInspector]
        public List<BoardSlot> slots;

        void Awake()
        {
            slots = new List<BoardSlot> { backSlot, modSlot1, midSlot, modSlot2, frontSlot };
        }

        public void MoveCardsForward()
        {
            // Przesuń karty do przodu: BACK → MOD → MID → MOD → FRONT
            Card frontCard = frontSlot.RemoveCard();
            Card mod2Card = modSlot2.currentCard;
            Card midCard = midSlot.RemoveCard();
            Card mod1Card = modSlot1.currentCard;
            Card backCard = backSlot.RemoveCard();

            // Apply modifiers podczas przechodzenia
            if (backCard != null && mod1Card != null)
            {
                mod1Card.TriggerAbility("OnCardPass", backCard);
            }

            if (midCard != null && mod2Card != null)
            {
                mod2Card.TriggerAbility("OnCardPass", midCard);
            }

            // Move cards
            if (midCard != null) frontSlot.PlaceCard(midCard);
            if (backCard != null) midSlot.PlaceCard(backCard);
        }

        public bool HasCardOnFront()
        {
            return frontSlot.currentCard != null;
        }

        public Card GetFrontCard()
        {
            return frontSlot.currentCard;
        }
    }
}
