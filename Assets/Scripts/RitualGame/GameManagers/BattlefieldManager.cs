using CardGame.Board.Slot;
using CardGame.CardObj;
using UnityEngine;
using System.Collections;
using CardGame.Manager.Main;

namespace CardGame.Manager.Battlefield
{
    public class BattlefieldManager : MonoBehaviour
    {
        public BoardSlot battlefieldSlot;
        public Transform valhallaTransform;
        public Transform graveyardTransform;

        [Header("Battle Settings")]
        public Transform playerBattlePosition;
        public Transform aiBattlePosition;
        public float cardDisplayTime = 1.5f; // How lang show before battle
        public float resultDisplayTime = 1f; // Time to show resulat

        private RitualGameManager _gameManager;

        public void ResolveBattle(Card playerCard, Card aiCard)
        {
            _gameManager = FindAnyObjectByType<RitualGameManager>();
            StartCoroutine(ResolveBattleSequence(playerCard, aiCard));
        }

        IEnumerator ResolveBattleSequence(Card playerCard, Card aiCard)
        {
            Debug.Log($"Battle: {playerCard.data.cardName} ({playerCard.GetEffectivePower()}) vs {aiCard.data.cardName} ({aiCard.GetEffectivePower()})");

            PlaceCardOnBattlefield(playerCard, playerBattlePosition);
            PlaceCardOnBattlefield(aiCard, aiBattlePosition);

            yield return new WaitForSeconds(cardDisplayTime);

            // Trigger battle abilities
            playerCard.TriggerAbility("OnBattle", aiCard);
            aiCard.TriggerAbility("OnBattle", playerCard);

            if(_gameManager.TypeBattle == TypeBattle.Destroy)
            {
                playerCard.SwapEffectivePower();
            }

            Card winner, loser;

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
                // Remis
                yield return new WaitForSeconds(resultDisplayTime);
                SendToGraveyard(playerCard);
                SendToGraveyard(aiCard);
                yield break;
            }

            if (_gameManager.TypeBattle == TypeBattle.Heal)
            {
                _gameManager.currentBilarHP += winner.GetEffectivePower();
            }
            else
            {
                _gameManager.currentBilarHP -= winner.GetEffectivePower();
            }

            yield return new WaitForSeconds(resultDisplayTime);

            SendToValhalla(winner);
            SendToGraveyard(loser);

            _gameManager.CheckForEndGame();
        }

        void PlaceCardOnBattlefield(Card card, Transform position)
        {
            card.transform.SetParent(battlefieldSlot.transform);

            if (position != null)
            {
                card.transform.position = position.position;
                card.transform.rotation = position.rotation;
            }
            else
            {
                card.transform.localPosition = Vector3.zero;
                card.transform.localRotation = Quaternion.identity;
            }

            card.transform.localScale = Vector3.one;

            Debug.Log($"{card.data.cardName} in battle!");
        }

        void SendToValhalla(Card card)
        {
            card.transform.SetParent(valhallaTransform);
            card.transform.localPosition = Vector3.zero;
            card.TriggerAbility("OnVictory", null);
            Debug.Log($"{card.data.cardName} Go to Valhalli!");
        }

        void SendToGraveyard(Card card)
        {
            card.transform.SetParent(graveyardTransform);
            card.transform.localPosition = Vector3.zero;
            card.TriggerAbility("OnDefeat", null);
            Debug.Log($"{card.data.cardName} Go to Cemeteries!");
        }
    }
}