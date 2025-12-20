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
using System;
using Unity.Collections;

namespace CardGame.Manager.Main
{
    public enum WhoWin : int
    {
        Player = 0,
        AI = 1
    }

    public enum TypeBattle : int
    {
        Heal = 0,
        Destroy = 1
    }
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

        [Header("Stats Of Game")]
        public TextMeshProUGUI BilarHPText;
        public int MaxBilarHP;
        [ReadOnly]
        public WhoWin WhoWin;
        public TypeBattle TypeBattle;

        public bool allCardsFaceDown;

        [HideInInspector]
        public int currentBilarHP;

        private int turnCount;
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

            SetType();
            if(TypeBattle == TypeBattle.Destroy)
                currentBilarHP = MaxBilarHP;

            BilarHPText.text = currentBilarHP.ToString();

            UpdateUI();
        }

        public void SetType()
        {
            var r = UnityEngine.Random.Range(0, 1);
            TypeBattle = (TypeBattle)r;
        }

        public void OnCardClicked(Card card)
        {
            if (currentState != GameState.PlayerTurn) return;
            if (card.owner != Player.Human) return;

            selectedCard = card;

            if (TypeBattle == TypeBattle.Destroy)
                selectedCard.SwapEffectivePower();

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
            
            playerDeck.RemoveFromHand(card);
            slot.PlaceCard(card);

            
            playerDeck.DrawCardFromDomain(card.data.domain);

            ClearHighlights();
            selectedCard = null;
        }

        public void OnDomainPileClicked(CardDomain domain)
        {
            if (currentState != GameState.PlayerTurn) return;
            //TODO
            Debug.Log($"Kliknięto kupkę domeny: {domain}");

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
            BroadcastTurnEndEvent();
        }

        void BroadcastTurnEndEvent()
        {
            BroadcastToLane(playerLane);
            BroadcastToLane(aiLane);
        }

        void BroadcastToLane(PlayerLane lane)
        {
            if (lane.backSlot.currentCard != null)
                lane.backSlot.currentCard.TriggerAbility("OnTurnEnd", null);

            if (lane.midSlot.currentCard != null)
                lane.midSlot.currentCard.TriggerAbility("OnTurnEnd", null);

            if (lane.frontSlot.currentCard != null)
                lane.frontSlot.currentCard.TriggerAbility("OnTurnEnd", null);

            if (lane.modSlot1.currentCard != null)
                lane.modSlot1.currentCard.TriggerAbility("OnTurnEnd", null);

            if (lane.modSlot2.currentCard != null)
                lane.modSlot2.currentCard.TriggerAbility("OnTurnEnd", null);

            if (allCardsFaceDown)
            {
                foreach(var la in lane.slots)
                {
                    la.currentCard.SetHidden(true);
                }
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

            currentState = GameState.PlayerTurn;
            playerTurn = true;
            UpdateUI();
        }

        //TODO
        public void CheckForEndGame()
        {
            if(TypeBattle == TypeBattle.Heal)
            {
                if(currentBilarHP == MaxBilarHP)
                {
                    Debug.Log("Win in Heal mode");
                    currentState = GameState.GameEnd;
                }
            }
            else
            {
                if(currentBilarHP <= 0)
                {
                    Debug.Log("Win in Destroy mode");
                    currentState = GameState.GameEnd;
                }
            }
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

            BilarHPText.text = currentBilarHP.ToString();
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
