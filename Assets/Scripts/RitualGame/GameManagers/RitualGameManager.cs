using TMPro;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections.Generic;

using CardGame.AI;
using CardGame.CardObj;
using CardGame.Board.Lane;
using CardGame.Board.Slot;
using CardGame.Manager.Deck;
using CardGame.Manager.Battlefield;

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
        public GameObject Board = null;

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

        private int _turnCount;
        private Card _selectedCard;
        private bool playerTurn = true;

        public GameState currentState = GameState.PlayerTurn;
        public Dictionary<string, object> gameVariables = new Dictionary<string, object>();
        private bool _FarcePass;
        private AdvancedAI _AiComp;

        public enum GameState { PlayerTurn, AITurn, Battle, GameEnd }

        void Awake()
        {
            Instance = this;
        }

        public void StartRitualGame()
        {
            moveButton.onClick.AddListener(OnMoveButtonClicked);
            passButton.onClick.AddListener(OnPassButtonClicked);

            if (TypeBattle == TypeBattle.Destroy)
                currentBilarHP = MaxBilarHP;

            AdvancedAI ai = GetComponent<AdvancedAI>();
            if (ai == null)
            {
                _AiComp = gameObject.AddComponent<AdvancedAI>();
                _AiComp.Initialize(aiLane, aiDeck);
            }

            //BilarHPText.text = currentBilarHP.ToString();

            UpdateUI();

            Board.SetActive(true);
        }

        public void SetType(int Balance)
        {
            var r = UnityEngine.Random.Range(0, 1);
            TypeBattle = (TypeBattle)r;
        }

        public void OnCardClicked(Card card)
        {
            if (currentState != GameState.PlayerTurn) return;
            if (card.owner != Player.Human) return;

            _selectedCard = card;

            if (TypeBattle == TypeBattle.Destroy)
                _selectedCard.SwapEffectivePower();

            HighlightValidSlots(card);
        }

        public void OnSlotClicked(BoardSlot slot)
        {
            if (_selectedCard == null) return;
            if (!slot.CanPlaceCard(_selectedCard)) return;

            PlayCardOnSlot(_selectedCard, slot);
        }

        public void PlayCardOnSlot(Card card, BoardSlot slot)
        {

            playerDeck.RemoveFromHand(card);
            slot.PlaceCard(card);

            var cardCheck = playerDeck.DrawCardFromDomain(card.data.domain);
            if (cardCheck == null)
            {
                currentState = GameState.GameEnd;
            }

            ClearHighlights();
            _selectedCard = null;
        }

        public void OnDomainPileClicked(CardDomain domain)
        {
            if (currentState != GameState.PlayerTurn) return;
            //TODO
            Debug.Log($"Domain pile clicked: {domain}");

            if (playerDeck.DrawCardFromDomain(domain) == null)
            {
                Debug.Log($"lost: {currentState.ToString()}");
            }

        }

        private void OnMoveButtonClicked()
        {
            playerLane.MoveCardsForward();
            EndPlayerTurn();
        }

        private void OnPassButtonClicked()
        {
            EndPlayerTurn();
        }

        private void EndPlayerTurn()
        {
            ProcessTurnEffects();
            currentState = GameState.AITurn;

            UpdateUI();
            Invoke(nameof(ExecuteAITurn), 1f);
        }

        private void ProcessTurnEffects()
        {
            List<string> keysToProcess = new List<string>();

            foreach (var kvp in gameVariables.ToList())
            {
                if (kvp.Key.Contains("opponentMoveBlocked") && kvp.Value is bool)
                {
                    _AiComp.PassTurn = true;
                    ForcePass();
                    gameVariables.Remove(kvp.Key);
                }

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

        private void BroadcastTurnEndEvent()
        {
            BroadcastToLane(playerLane);
            BroadcastToLane(aiLane);
        }

        private void BroadcastToLane(PlayerLane lane)
        {
            OnTurnEndTrigger(lane.backSlot);
            OnTurnEndTrigger(lane.modSlot1);
            OnTurnEndTrigger(lane.midSlot);
            OnTurnEndTrigger(lane.modSlot2);
            OnTurnEndTrigger(lane.frontSlot);

            if (allCardsFaceDown)
            {
                foreach (var la in lane.slots)
                {
                    la.currentCard.SetHidden(true);
                }
            }
        }

        private void OnTurnEndTrigger(BoardSlot slot)
        {
            if (slot.currentCard == null)
                return;

            slot.currentCard.TriggerAbility("OnTurnEnd", slot.currentCard);
        }


        private void TriggerDelayedEffect(string variableKey)
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

            battlefield.GraveyardSlot.currentCard.TriggerAbility("TriggerDelayedEffect", null);
            battlefield.ValhallaSlot.currentCard.TriggerAbility("TriggerDelayedEffect", null);
        }

        private void ForcePass()
        {
            moveButton.interactable = _FarcePass;
            _FarcePass = !_FarcePass;
        }

        private void RevealAllCards()
        {
            RevealLaneCards(playerLane);
            RevealLaneCards(aiLane);
        }

        private void RevealLaneCards(PlayerLane lane)
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

        private void ExecuteAITurn()
        {
            _AiComp.ExecuteTurn();

            CheckForBattle();
        }

        private void CheckForBattle()
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

            CheckForEndGame();
        }

        //TODO
        public void CheckForEndGame()
        {
            if (TypeBattle == TypeBattle.Heal)
            {
                if (currentBilarHP == MaxBilarHP)
                {
                    Debug.Log("Win in Heal mode");
                    currentState = GameState.GameEnd;
                }
            }
            else
            {
                if (currentBilarHP <= 0)
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

        private void UpdateUI()
        {
            turnText.text = currentState == GameState.PlayerTurn ? "Your turn" : "Enemy turn";
            moveButton.interactable = currentState == GameState.PlayerTurn;
            passButton.interactable = currentState == GameState.PlayerTurn;

            //BilarHPText.text = currentBilarHP.ToString();
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
        public Card GetBattlefieldCard() => battlefield.BattlefieldSlot.currentCard;
        public PlayerLane GetPlayerLane() => playerLane;
        public PlayerLane GetEnemyLane() => aiLane;
    }
}
