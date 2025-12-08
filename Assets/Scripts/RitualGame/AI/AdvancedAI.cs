using CardGame.Board.Lane;
using CardGame.Board.Slot;
using CardGame.CardObj;
using CardGame.Manager.Deck;
using UnityEngine;
using static CardGame.Structures.Structures;

namespace CardGame.AI
{
    public class AdvancedAI : MonoBehaviour
    {
        private PlayerLane aiLane;
        private DeckManager aiDeck;

        public void Initialize(PlayerLane lane, DeckManager deck)
        {
            aiLane = lane;
            aiDeck = deck;
        }

        public void ExecuteTurn()
        {
            // Strategia AI:
            // 1. Zagraj MOD karty jeśli są dostępne
            // 2. Zagraj najsilniejszą kartę
            // 3. Przesuń karty jeśli BACK jest zajęty

            var hand = aiDeck.GetHand();

            // Znajdź najlepszą kartę do zagrania
            Card bestCard = null;
            BoardSlot bestSlot = null;
            int bestScore = -1;

            foreach (Card card in hand)
            {
                BoardSlot targetSlot = GetBestSlotForCard(card);
                if (targetSlot != null)
                {
                    int score = EvaluatePlay(card, targetSlot);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCard = card;
                        bestSlot = targetSlot;
                    }
                }
            }

            // Zagraj najlepszą kartę
            if (bestCard != null && bestSlot != null)
            {
                aiDeck.RemoveFromHand(bestCard);
                bestSlot.PlaceCard(bestCard);
                aiDeck.DrawCardFromDomain(bestCard.data.domain);
            }

            // Zdecyduj czy przesunąć karty
            if (ShouldMoveCards())
            {
                aiLane.MoveCardsForward();
            }
        }

        BoardSlot GetBestSlotForCard(Card card)
        {
            if (card.data.cardType == CardType.Modifier)
            {
                if (aiLane.modSlot1.CanPlaceCard(card))

                    return aiLane.modSlot1;
                if (aiLane.modSlot2.CanPlaceCard(card)) 
                    return aiLane.modSlot2;
            }
            else
            {
                if (aiLane.backSlot.CanPlaceCard(card)) 

                    return aiLane.backSlot;
                if (aiLane.midSlot.CanPlaceCard(card)) 

                    return aiLane.midSlot;
            }
            return null;
        }

        int EvaluatePlay(Card card, BoardSlot slot)
        {
            int score = card.currentPower;

            // Preferuj MOD karty
            if (card.data.cardType == CardType.Modifier)
                score += 5;

            // Preferuj BACK slot
            if (slot.slotType == SlotType.BACK)
                score += 3;

            return score;
        }

        bool ShouldMoveCards()
        {
            return aiLane.backSlot.currentCard != null || Random.value > 0.6f;
        }
    }
}
