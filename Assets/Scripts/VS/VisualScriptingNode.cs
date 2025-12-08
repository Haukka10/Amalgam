using UnityEngine;
using Unity.VisualScripting;

using CardGame.CardObj;
using CardGame.Board.Lane;
using CardGame.Manager.Deck;
using CardGame.Manager.Main;
using System.Collections.Generic;

using static CardGame.Structures.Structures;

[UnitTitle("Apply Power Modifier")]
[UnitCategory("Rytuały/Card Effects")]
public class ApplyPowerModifierNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput target;
    [DoNotSerialize] public ValueInput powerChange;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            Card targetCard = flow.GetValue<Card>(target);
            int power = flow.GetValue<int>(powerChange);

            if (targetCard != null)
            {
                targetCard.ApplyModifier(power);
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        target = ValueInput<Card>(nameof(target), null);
        powerChange = ValueInput<int>(nameof(powerChange), 0);

        Succession(inputTrigger, outputTrigger);
    }
}

[UnitTitle("Get Card Power")]
[UnitCategory("Rytuały/Card Info")]
public class GetCardPowerNode : Unit
{
    [DoNotSerialize] public ValueInput card;
    [DoNotSerialize] public ValueOutput power;

    protected override void Definition()
    {
        card = ValueInput<Card>(nameof(card), null);
        power = ValueOutput<int>(nameof(power), (flow) =>
        {
            Card c = flow.GetValue<Card>(card);
            return c != null ? c.currentPower : 0;
        });
    }
}

[UnitTitle("Draw Card From Domain")]
[UnitCategory("Rytuały/Deck")]
public class DrawCardNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput domain;
    [DoNotSerialize] public ValueOutput drawnCard;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            CardDomain dom = flow.GetValue<CardDomain>(domain);

            // Znajdź DeckManager gracza
            DeckManager deck = Object.FindObjectOfType<DeckManager>();
            Card card = deck?.DrawCardFromDomain(dom);

            flow.SetValue(drawnCard, card);

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        domain = ValueInput<CardDomain>(nameof(domain), CardDomain.K);
        drawnCard = ValueOutput<Card>(nameof(drawnCard));

        Succession(inputTrigger, outputTrigger);
    }
}

// ============================================
// NOWE CUSTOM NODES DLA BATTLEFIELD I RĘKI
// ============================================

[UnitTitle("Get Player Hand")]
[UnitCategory("Rytuały/Game Info")]
public class GetPlayerHandNode : Unit
{
    [DoNotSerialize] public ValueOutput hand;

    protected override void Definition()
    {
        hand = ValueOutput<List<Card>>(nameof(hand), (flow) =>
        {
            return RitualGameManager.Instance?.GetPlayerHand();
        });
    }
}

[UnitTitle("Get Enemy Hand")]
[UnitCategory("Rytuały/Game Info")]
public class GetEnemyHandNode : Unit
{
    [DoNotSerialize] public ValueOutput hand;

    protected override void Definition()
    {
        hand = ValueOutput<List<Card>>(nameof(hand), (flow) =>
        {
            return RitualGameManager.Instance?.GetEnemyHand();
        });
    }
}

[UnitTitle("Get Battlefield Card")]
[UnitCategory("Rytuały/Game Info")]
public class GetBattlefieldCardNode : Unit
{
    [DoNotSerialize] public ValueOutput card;

    protected override void Definition()
    {
        card = ValueOutput<Card>(nameof(card), (flow) =>
        {
            return RitualGameManager.Instance?.GetBattlefieldCard();
        });
    }
}

[UnitTitle("Get Player Lane")]
[UnitCategory("Rytuały/Game Info")]
public class GetPlayerLaneNode : Unit
{
    [DoNotSerialize] public ValueOutput lane;

    protected override void Definition()
    {
        lane = ValueOutput<PlayerLane>(nameof(lane), (flow) =>
        {
            return RitualGameManager.Instance?.GetPlayerLane();
        });
    }
}

[UnitTitle("Get Enemy Lane")]
[UnitCategory("Rytuały/Game Info")]
public class GetEnemyLaneNode : Unit
{
    [DoNotSerialize] public ValueOutput lane;

    protected override void Definition()
    {
        lane = ValueOutput<PlayerLane>(nameof(lane), (flow) =>
        {
            return RitualGameManager.Instance?.GetEnemyLane();
        });
    }
}

[UnitTitle("Get Card From Slot")]
[UnitCategory("Rytuały/Lane")]
public class GetCardFromSlotNode : Unit
{
    [DoNotSerialize] public ValueInput lane;
    [DoNotSerialize] public ValueInput slotType;
    [DoNotSerialize] public ValueOutput card;

    protected override void Definition()
    {
        lane = ValueInput<PlayerLane>(nameof(lane), null);
        slotType = ValueInput<SlotType>(nameof(slotType), SlotType.BACK);

        card = ValueOutput<Card>(nameof(card), (flow) =>
        {
            PlayerLane playerLane = flow.GetValue<PlayerLane>(lane);
            SlotType slot = flow.GetValue<SlotType>(slotType);

            if (playerLane == null) return null;

            switch (slot)
            {
                case SlotType.BACK: return playerLane.backSlot.currentCard;
                case SlotType.MID: return playerLane.midSlot.currentCard;
                case SlotType.FRONT: return playerLane.frontSlot.currentCard;
                default: return null;
            }
        });
    }
}

[UnitTitle("Destroy Card In Hand")]
[UnitCategory("Rytuały/Hand Effects")]
public class DestroyCardInHandNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput targetCard;
    [DoNotSerialize] public ValueInput isPlayerHand;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            Card card = flow.GetValue<Card>(targetCard);
            bool playerHand = flow.GetValue<bool>(isPlayerHand);

            if (card != null && RitualGameManager.Instance != null)
            {
                DeckManager deck = playerHand ?
                    RitualGameManager.Instance.playerDeck :
                    RitualGameManager.Instance.aiDeck;

                deck.RemoveFromHand(card);
                Object.Destroy(card.gameObject);
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        targetCard = ValueInput<Card>(nameof(targetCard), null);
        isPlayerHand = ValueInput<bool>(nameof(isPlayerHand), true);

        Succession(inputTrigger, outputTrigger);
    }
}

[UnitTitle("Get Random Card From Hand")]
[UnitCategory("Rytuały/Hand Effects")]
public class GetRandomCardFromHandNode : Unit
{
    [DoNotSerialize] public ValueInput isPlayerHand;
    [DoNotSerialize] public ValueOutput randomCard;

    protected override void Definition()
    {
        isPlayerHand = ValueInput<bool>(nameof(isPlayerHand), true);

        randomCard = ValueOutput<Card>(nameof(randomCard), (flow) =>
        {
            bool playerHand = flow.GetValue<bool>(isPlayerHand);

            List<Card> hand = playerHand ?
                RitualGameManager.Instance?.GetPlayerHand() :
                RitualGameManager.Instance?.GetEnemyHand();

            if (hand != null && hand.Count > 0)
            {
                return hand[UnityEngine.Random.Range(0, hand.Count)];
            }

            return null;
        });
    }
}

[UnitTitle("Get Hand Size")]
[UnitCategory("Rytuały/Hand Effects")]
public class GetHandSizeNode : Unit
{
    [DoNotSerialize] public ValueInput isPlayerHand;
    [DoNotSerialize] public ValueOutput size;

    protected override void Definition()
    {
        isPlayerHand = ValueInput<bool>(nameof(isPlayerHand), true);

        size = ValueOutput<int>(nameof(size), (flow) =>
        {
            bool playerHand = flow.GetValue<bool>(isPlayerHand);

            List<Card> hand = playerHand ?
                RitualGameManager.Instance?.GetPlayerHand() :
                RitualGameManager.Instance?.GetEnemyHand();

            return hand?.Count ?? 0;
        });
    }
}

[UnitTitle("Affect All Cards In Hand")]
[UnitCategory("Rytuały/Hand Effects")]
public class AffectAllCardsInHandNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput isPlayerHand;
    [DoNotSerialize] public ValueInput powerChange;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            bool playerHand = flow.GetValue<bool>(isPlayerHand);
            int power = flow.GetValue<int>(powerChange);

            List<Card> hand = playerHand ?
                RitualGameManager.Instance?.GetPlayerHand() :
                RitualGameManager.Instance?.GetEnemyHand();

            if (hand != null)
            {
                foreach (Card card in hand)
                {
                    card.ApplyModifier(power);
                }
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        isPlayerHand = ValueInput<bool>(nameof(isPlayerHand), true);
        powerChange = ValueInput<int>(nameof(powerChange), 0);

        Succession(inputTrigger, outputTrigger);
    }
}

[UnitTitle("Get Effective Power")]
[UnitCategory("Rytuały/Card Info")]
public class GetEffectivePowerNode : Unit
{
    [DoNotSerialize] public ValueInput card;
    [DoNotSerialize] public ValueOutput power;

    protected override void Definition()
    {
        card = ValueInput<Card>(nameof(card), null);
        power = ValueOutput<int>(nameof(power), (flow) =>
        {
            Card c = flow.GetValue<Card>(card);
            return c != null ? c.GetEffectivePower() : 0;
        });
    }
}