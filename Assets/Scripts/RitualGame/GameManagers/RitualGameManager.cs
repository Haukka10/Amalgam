using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using CardGame.AI;
using CardGame.CardObj;
using CardGame.Board.Slot;
using CardGame.Board.Lane;
using CardGame.Manager.Deck;
using CardGame.Manager.Battlefield;

using static CardGame.Structures.Structures;
using System.Linq;

namespace CardGame.Manager.Main
{
    public class RitualGameManager : MonoBehaviour
    {
        public static RitualGameManager Instance;

        [Header("References")]
        public PlayerLane playerLane;
        public PlayerLane aiLane;
        public DeckManager playerDeck;
        public DeckManager aiDeck;
        public BattlefieldManager battlefield;

        [Header("UI")]
        public Button moveButton;
        public Button passButton;
        public TextMeshProUGUI turnText;

        private Card selectedCard;
        private bool playerTurn = true;

        public GameState currentState = GameState.PlayerTurn;
        public Dictionary<string, object> gameVariables = new Dictionary<string, object>();

        public enum GameState { PlayerTurn, AITurn, Battle, GameEnd }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            moveButton.onClick.AddListener(OnMoveButtonClicked);
            passButton.onClick.AddListener(OnPassButtonClicked);

            UpdateUI();
        }

        public void OnCardClicked(Card card)
        {
            if (currentState != GameState.PlayerTurn) return;
            if (card.owner != Player.Human) return;

            selectedCard = card;
            HighlightValidSlots(card);
        }

        public void OnSlotClicked(BoardSlot slot)
        {
            if (selectedCard == null) return;
            if (!slot.CanPlaceCard(selectedCard)) return;

            PlayCardOnSlot(selectedCard, slot);
        }

        public void PlayCardOnSlot(Card card, BoardSlot slot)
        {
            // Zagraj kartę
            playerDeck.RemoveFromHand(card);
            slot.PlaceCard(card);

            // Dobierz nową kartę z tej samej domeny
            playerDeck.DrawCardFromDomain(card.data.domain);

            ClearHighlights();
            selectedCard = null;
        }

        public void OnDomainPileClicked(CardDomain domain)
        {
            if (currentState != GameState.PlayerTurn) return;

            // Opcjonalnie: dobierz dodatkową kartę z kupki
            Debug.Log($"Kliknięto kupkę domeny: {domain}");

            // Można tu dodać specjalną mechanikę, np. podgląd kupki
        }

        void OnMoveButtonClicked()
        {
            playerLane.MoveCardsForward();
            EndPlayerTurn();
        }

        void OnPassButtonClicked()
        {
            EndPlayerTurn();
        }

        void EndPlayerTurn()
        {
            ProcessTurnEffects();
            currentState = GameState.AITurn;
            UpdateUI();
            Invoke(nameof(ExecuteAITurn), 1f);
        }

        void ProcessTurnEffects()
        {
            // Sprawdź wszystkie karty z timerem
            List<string> keysToProcess = new List<string>();

            foreach (var kvp in gameVariables.ToList())
            {
                if (kvp.Key.Contains("TurnsLeft") && kvp.Value is int)
                {
                    int turns = (int)kvp.Value;
                    turns--;
                    gameVariables[kvp.Key] = turns;

                    if (turns <= 0)
                    {
                        keysToProcess.Add(kvp.Key);
                    }
                }
            }

            // Trigger delayed effects
            foreach (string key in keysToProcess)
            {
                TriggerDelayedEffect(key);
                gameVariables.Remove(key);
            }
        }

        void TriggerDelayedEffect(string variableKey)
        {
            Debug.Log($"Triggered delayed effect: {variableKey}");

            // Custom event for cards with delayed effects
            // Cards listening for this can react

            // Example: Darkness effect ending
            if (variableKey == "darknessTurnsLeft")
            {
                SetGameVariable("allCardsFaceDown", false);
                RevealAllCards();
            }
        }

        void RevealAllCards()
        {
            RevealLaneCards(playerLane);
            RevealLaneCards(aiLane);
        }

        void RevealLaneCards(PlayerLane lane)
        {
            if (lane.backSlot.currentCard != null)
            {
                lane.backSlot.currentCard.SetHidden(false);
            }

            if (lane.midSlot.currentCard != null)
            {
                lane.midSlot.currentCard.SetHidden(false);
            }

            if (lane.frontSlot.currentCard != null)
            {
                lane.frontSlot.currentCard.SetHidden(false);
            }
        }

        void ExecuteAITurn()
        {
            // Użyj zaawansowanego AI
            AdvancedAI ai = GetComponent<AdvancedAI>();
            if (ai == null)
            {
                ai = gameObject.AddComponent<AdvancedAI>();
                ai.Initialize(aiLane, aiDeck);
            }

            ai.ExecuteTurn();

            CheckForBattle();
        }

        void CheckForBattle()
        {
            if (playerLane.HasCardOnFront() && aiLane.HasCardOnFront())
            {
                currentState = GameState.Battle;

                Card playerCard = playerLane.frontSlot.RemoveCard();
                Card aiCard = aiLane.frontSlot.RemoveCard();

                battlefield.ResolveBattle(playerCard, aiCard);
            }

            // Powrót do tury gracza
            currentState = GameState.PlayerTurn;
            playerTurn = true;
            UpdateUI();
        }

        public void HighlightValidSlots(Card card)
        {
            ClearHighlights();

            foreach (BoardSlot slot in playerLane.GetComponentsInChildren<BoardSlot>())
            {
                if (slot.CanPlaceCard(card))
                    slot.SetHighlight(true);
            }
        }

        public void ClearHighlights()
        {
            foreach (BoardSlot slot in playerLane.GetComponentsInChildren<BoardSlot>())
            {
                slot.SetHighlight(false);
            }
        }

        void UpdateUI()
        {
            turnText.text = currentState == GameState.PlayerTurn ? "Your turn" : "Enemy turn";
            moveButton.interactable = currentState == GameState.PlayerTurn;
            passButton.interactable = currentState == GameState.PlayerTurn;
        }

        public void SetGameVariable(string name, object value)
        {
            if (gameVariables.ContainsKey(name))
                gameVariables[name] = value;
            else
                gameVariables.Add(name, value);
        }

        public object GetGameVariable(string name, object defaultValue = null)
        {
            if (gameVariables.ContainsKey(name))
                return gameVariables[name];
            return defaultValue;
        }

        // Publiczne metody do użycia w Visual Scripting
        public List<Card> GetPlayerHand() => playerDeck.GetHand();
        public List<Card> GetEnemyHand() => aiDeck.GetHand();
        public Card GetBattlefieldCard() => battlefield.battlefieldSlot.currentCard;
        public PlayerLane GetPlayerLane() => playerLane;
        public PlayerLane GetEnemyLane() => aiLane;
    }
}
