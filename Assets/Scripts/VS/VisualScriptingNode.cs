using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

using CardGame.CardObj;
using CardGame.Board.Lane;
using CardGame.Manager.Deck;
using CardGame.Manager.Main;
using System.Collections.Generic;

using static CardGame.Structures.Structures;
using CardGame.Board.Slot;
using CardGame.Manager.Battlefield;


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
            DeckManager deck = Object.FindAnyObjectByType<DeckManager>();
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

// Node 1: Count Cards In Valhalla
[UnitTitle("Count Cards In Valhalla")]
[UnitCategory("Rytuały/Advanced")]
public class CountCardsInValhallaNode : Unit
{
    [DoNotSerialize] public ValueInput cardNameInput;
    [DoNotSerialize] public ValueOutput countOutput;

    protected override void Definition()
    {
        cardNameInput = ValueInput<string>(nameof(cardNameInput), "");

        countOutput = ValueOutput<int>(nameof(countOutput), (flow) =>
        {
            string name = flow.GetValue<string>(cardNameInput);

            BattlefieldManager bf = RitualGameManager.Instance?.battlefield;
            if (bf == null) return 0;

            int c = 0;
            foreach (Transform child in bf.valhallaTransform)
            {
                Card card = child.GetComponent<Card>();
                if (card != null && card.data.cardName == name)
                {
                    c++;
                }
            }

            return c;
        });
    }
}

// Node 2: Return Card To Slot
[UnitTitle("Return Card To Slot")]
[UnitCategory("Rytuały/Advanced")]
public class ReturnCardToSlotNode : Unit
{
    [DoNotSerialize] public ControlInput returnTrigger;
    [DoNotSerialize] public ControlOutput returnOutput;

    [DoNotSerialize] public ValueInput cardInput;
    [DoNotSerialize] public ValueInput slotTypeReturn;

    protected override void Definition()
    {
        returnTrigger = ControlInput(nameof(returnTrigger), (flow) =>
        {
            Card c = flow.GetValue<Card>(cardInput);
            SlotType slot = flow.GetValue<SlotType>(slotTypeReturn);

            if (c != null && RitualGameManager.Instance != null)
            {
                PlayerLane lane = c.owner == Player.Human ?
                    RitualGameManager.Instance.playerLane :
                    RitualGameManager.Instance.aiLane;

                BoardSlot targetSlot = GetSlotByType(lane, slot);

                if (targetSlot != null && targetSlot.currentCard == null)
                {
                    targetSlot.PlaceCard(c);
                }
            }

            return returnOutput;
        });

        returnOutput = ControlOutput(nameof(returnOutput));
        cardInput = ValueInput<Card>(nameof(cardInput), null);
        slotTypeReturn = ValueInput<SlotType>(nameof(slotTypeReturn), SlotType.BACK);

        Succession(returnTrigger, returnOutput);
    }

    BoardSlot GetSlotByType(PlayerLane lane, SlotType type)
    {
        switch (type)
        {
            case SlotType.BACK: return lane.backSlot;
            case SlotType.MID: return lane.midSlot;
            case SlotType.FRONT: return lane.frontSlot;
            default: return null;
        }
    }
}

// Node 3: Destroy Card
[UnitTitle("Destroy Card")]
[UnitCategory("Rytuały/Advanced")]
public class DestroyCardNode : Unit
{
    [DoNotSerialize] public ControlInput destroyCardTrigger;
    [DoNotSerialize] public ControlOutput destroyCardOutput;

    [DoNotSerialize] public ValueInput cardToDestroy;

    protected override void Definition()
    {
        destroyCardTrigger = ControlInput(nameof(destroyCardTrigger), (flow) =>
        {
            Card c = flow.GetValue<Card>(cardToDestroy);

            if (c != null)
            {
                Object.Destroy(c.gameObject);
            }

            return destroyCardOutput;
        });

        destroyCardOutput = ControlOutput(nameof(destroyCardOutput));
        cardToDestroy = ValueInput<Card>(nameof(cardToDestroy), null);

        Succession(destroyCardTrigger, destroyCardOutput);
    }
}

// Node 4: Set Game Variable
[UnitTitle("Set Game Variable")]
[UnitCategory("Rytuały/Variables")]
public class SetGameVariableNode : Unit
{
    [DoNotSerialize] public ControlInput setVarTrigger;
    [DoNotSerialize] public ControlOutput setVarOutput;

    [DoNotSerialize] public ValueInput varName;
    [DoNotSerialize] public ValueInput varValue;

    protected override void Definition()
    {
        setVarTrigger = ControlInput(nameof(setVarTrigger), (flow) =>
        {
            string name = flow.GetValue<string>(varName);
            object val = flow.GetValue<object>(varValue);

            if (RitualGameManager.Instance != null)
            {
                if (!RitualGameManager.Instance.gameVariables.ContainsKey(name))
                {
                    RitualGameManager.Instance.gameVariables.Add(name, val);
                }
                else
                {
                    RitualGameManager.Instance.gameVariables[name] = val;
                }
            }

            return setVarOutput;
        });

        setVarOutput = ControlOutput(nameof(setVarOutput));
        varName = ValueInput<string>(nameof(varName), "");
        varValue = ValueInput<object>(nameof(varValue), null);

        Succession(setVarTrigger, setVarOutput);
    }
}

// Node 5: Get Game Variable
[UnitTitle("Get Game Variable")]
[UnitCategory("Rytuały/Variables")]
public class GetGameVariableNode : Unit
{
    [DoNotSerialize] public ValueInput getVarName;
    [DoNotSerialize] public ValueInput defaultVal;
    [DoNotSerialize] public ValueOutput varOutput;

    protected override void Definition()
    {
        getVarName = ValueInput<string>(nameof(getVarName), "");
        defaultVal = ValueInput<object>(nameof(defaultVal), null);

        varOutput = ValueOutput<object>(nameof(varOutput), (flow) =>
        {
            string name = flow.GetValue<string>(getVarName);
            object defVal = flow.GetValue<object>(defaultVal);

            if (RitualGameManager.Instance != null &&
                RitualGameManager.Instance.gameVariables.ContainsKey(name))
            {
                return RitualGameManager.Instance.gameVariables[name];
            }

            return defVal;
        });
    }
}

// Node 6: Find Card In Hand By Domain
[UnitTitle("Find Card In Hand By Domain")]
[UnitCategory("Rytuały/Hand Effects")]
public class FindCardInHandByDomainNode : Unit
{
    [DoNotSerialize] public ValueInput domainInput;
    [DoNotSerialize] public ValueInput isPlayerHandFind;
    [DoNotSerialize] public ValueOutput foundCardOutput;

    protected override void Definition()
    {
        domainInput = ValueInput<CardDomain>(nameof(domainInput), CardDomain.K);
        isPlayerHandFind = ValueInput<bool>(nameof(isPlayerHandFind), false);

        foundCardOutput = ValueOutput<Card>(nameof(foundCardOutput), (flow) =>
        {
            CardDomain dom = flow.GetValue<CardDomain>(domainInput);
            bool playerHand = flow.GetValue<bool>(isPlayerHandFind);

            List<Card> hand = playerHand ?
                RitualGameManager.Instance?.GetPlayerHand() :
                RitualGameManager.Instance?.GetEnemyHand();

            if (hand != null)
            {
                foreach (Card card in hand)
                {
                    if (card.data.domain == dom)
                    {
                        return card;
                    }
                }
            }

            return null;
        });
    }
}

// Node 7: Show Card Info UI
[UnitTitle("Show Card Info UI")]
[UnitCategory("Rytuały/UI")]
public class ShowCardInfoUINode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput card;
    [DoNotSerialize] public ValueInput message;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            Card c = flow.GetValue<Card>(card);
            string msg = flow.GetValue<string>(message);

            if (c != null)
            {
                // Show UI popup with card info
                Debug.Log($"{msg}: {c.data.cardName} (Power: {c.currentPower}, Domain: {c.data.domain})");

                // TODO: Create actual UI popup
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        card = ValueInput<Card>(nameof(card), null);
        message = ValueInput<string>(nameof(message), "Revealed Card");

        Succession(inputTrigger, outputTrigger);
    }
}

// Node 8: Transform Card Data
[UnitTitle("Transform Card")]
[UnitCategory("Rytuały/Advanced")]
public class TransformCardNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput card;
    [DoNotSerialize] public ValueInput newName;
    [DoNotSerialize] public ValueInput newPower;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            Card c = flow.GetValue<Card>(card);
            string name = flow.GetValue<string>(newName);
            int power = flow.GetValue<int>(newPower);

            if (c != null)
            {
                // Create new CardData (or modify existing)
                c.currentPower = power;
                c.UpdateVisuals();

                // For name change, would need to create new CardData
                Debug.Log($"Card transformed to: {name} with power {power}");
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        card = ValueInput<Card>(nameof(card), null);
        newName = ValueInput<string>(nameof(newName), "");
        newPower = ValueInput<int>(nameof(newPower), 0);

        Succession(inputTrigger, outputTrigger);
    }
}

// Node 9: Get Opposite MOD Slot
[UnitTitle("Get Opposite MOD Slot")]
[UnitCategory("Rytuały/Lane")]
public class GetOppositeMODSlotNode : Unit
{
    [DoNotSerialize] public ValueInput mySlot;
    [DoNotSerialize] public ValueOutput oppositeSlot;

    protected override void Definition()
    {
        mySlot = ValueInput<BoardSlot>(nameof(mySlot), null);

        oppositeSlot = ValueOutput<BoardSlot>(nameof(oppositeSlot), (flow) =>
        {
            BoardSlot slot = flow.GetValue<BoardSlot>(mySlot);

            if (slot == null || RitualGameManager.Instance == null) return null;

            // Determine which lane this slot belongs to
            PlayerLane myLane = slot.owner == Player.Human ?
                RitualGameManager.Instance.playerLane :
                RitualGameManager.Instance.aiLane;

            PlayerLane oppositeLane = slot.owner == Player.Human ?
                RitualGameManager.Instance.aiLane :
                RitualGameManager.Instance.playerLane;

            // Find which MOD slot (1 or 2)
            if (slot == myLane.modSlot1)
            {
                return oppositeLane.modSlot1;
            }
            else if (slot == myLane.modSlot2)
            {
                return oppositeLane.modSlot2;
            }

            return null;
        });
    }
}

// Node 10: Copy Card Ability
[UnitTitle("Copy Card Ability")]
[UnitCategory("Rytuały/Advanced")]
public class CopyCardAbilityNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput sourceCard;
    [DoNotSerialize] public ValueInput targetCard;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            Card source = flow.GetValue<Card>(sourceCard);
            Card target = flow.GetValue<Card>(targetCard);

            if (source != null && target != null && source.data.abilityGraph != null)
            {
                // Copy ability graph reference
                target.data.abilityGraph = source.data.abilityGraph;

                // Copy any variables (like usesRemaining)
                // This is simplified - you'd need proper variable copying
                Debug.Log($"Copied ability from {source.data.cardName} to {target.data.cardName}");
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        sourceCard = ValueInput<Card>(nameof(sourceCard), null);
        targetCard = ValueInput<Card>(nameof(targetCard), null);

        Succession(inputTrigger, outputTrigger);
    }
}

// Node 11: Set All Cards Face Down
[UnitTitle("Set All Cards Face Down")]
[UnitCategory("Rytuały/Board")]
public class SetAllCardsFaceDownNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput faceDown;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            bool hidden = flow.GetValue<bool>(faceDown);

            if (RitualGameManager.Instance != null)
            {
                // Set all cards on both lanes
                SetLaneCardsFaceDown(RitualGameManager.Instance.playerLane, hidden);
                SetLaneCardsFaceDown(RitualGameManager.Instance.aiLane, hidden);
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        faceDown = ValueInput<bool>(nameof(faceDown), true);

        Succession(inputTrigger, outputTrigger);
    }

    void SetLaneCardsFaceDown(PlayerLane lane, bool hidden)
    {
        if (lane.backSlot.currentCard != null)
            lane.backSlot.currentCard.SetHidden(hidden);
        if (lane.midSlot.currentCard != null)
            lane.midSlot.currentCard.SetHidden(hidden);
        if (lane.frontSlot.currentCard != null)
            lane.frontSlot.currentCard.SetHidden(hidden);
    }
}

// Node 12: Flip Power Sign
[UnitTitle("Flip Power Sign")]
[UnitCategory("Rytuały/Card Effects")]
public class FlipPowerSignNode : Unit
{
    [DoNotSerialize] public ControlInput inputTrigger;
    [DoNotSerialize] public ControlOutput outputTrigger;

    [DoNotSerialize] public ValueInput card;

    protected override void Definition()
    {
        inputTrigger = ControlInput(nameof(inputTrigger), (flow) =>
        {
            Card c = flow.GetValue<Card>(card);

            if (c != null)
            {
                int currentPower = c.currentPower;
                int newPower = -currentPower;
                int difference = newPower - currentPower;

                c.ApplyModifier(difference);

                Debug.Log($"Flipped {c.data.cardName} power from {currentPower} to {newPower}");
            }

            return outputTrigger;
        });

        outputTrigger = ControlOutput(nameof(outputTrigger));
        card = ValueInput<Card>(nameof(card), null);

        Succession(inputTrigger, outputTrigger);
    }
}

// Node 13: Check If Slot Type
[UnitTitle("Check If Slot Type")]
[UnitCategory("Rytuały/Lane")]
public class CheckIfSlotTypeNode : Unit
{
    [DoNotSerialize] public ValueInput slot;
    [DoNotSerialize] public ValueInput slotType;
    [DoNotSerialize] public ValueOutput isMatch;

    protected override void Definition()
    {
        slot = ValueInput<BoardSlot>(nameof(slot), null);
        slotType = ValueInput<SlotType>(nameof(slotType), SlotType.BACK);

        isMatch = ValueOutput<bool>(nameof(isMatch), (flow) =>
        {
            BoardSlot s = flow.GetValue<BoardSlot>(slot);
            SlotType type = flow.GetValue<SlotType>(slotType);

            return s != null && s.slotType == type;
        });
    }
}