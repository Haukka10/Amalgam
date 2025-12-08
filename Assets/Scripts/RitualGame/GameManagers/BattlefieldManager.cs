using CardGame.Board.Slot;
using CardGame.CardObj;
using UnityEngine;

namespace CardGame.Manager.Battlefield
{
    public class BattlefieldManager : MonoBehaviour
    {
        public BoardSlot battlefieldSlot;
        public Transform valhallaTransform;
        public Transform graveyardTransform;

        public void ResolveBattle(Card playerCard, Card aiCard)
        {
            Debug.Log($"Battle: {playerCard.data.cardName} ({playerCard.GetEffectivePower()}) vs {aiCard.data.cardName} ({aiCard.GetEffectivePower()})");

            // Trigger battle abilities
            playerCard.TriggerAbility("OnBattle", aiCard);
            aiCard.TriggerAbility("OnBattle", playerCard);

            Card winner, loser;

            // Użyj GetEffectivePower() zamiast currentPower (uwzględnia bonus +1 z BACK)
            if (playerCard.GetEffectivePower() > aiCard.GetEffectivePower())
            {
                winner = playerCard;
                loser = aiCard;
            }
            else if (aiCard.GetEffectivePower() > playerCard.GetEffectivePower())
            {
                winner = aiCard;
                loser = playerCard;
            }
            else
            {
                // Remis - obie do cmentarza
                SendToGraveyard(playerCard);
                SendToGraveyard(aiCard);
                return;
            }

            SendToValhalla(winner);
            SendToGraveyard(loser);
        }

        void SendToValhalla(Card card)
        {
            card.transform.SetParent(valhallaTransform);
            card.TriggerAbility("OnVictory", null);
            Debug.Log($"{card.data.cardName} idzie do Valhalli!");
        }

        void SendToGraveyard(Card card)
        {
            card.transform.SetParent(graveyardTransform);
            card.transform.SetParent(battlefieldSlot.transform); // Zostaje na battlefield
            card.TriggerAbility("OnDefeat", null);
            Debug.Log($"{card.data.cardName} idzie do Cmentarzyska!");
        }
    }
}
