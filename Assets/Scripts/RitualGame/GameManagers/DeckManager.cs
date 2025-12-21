using System.Collections.Generic;
using CardGame.CardObj;
using UnityEngine.UI;
using UnityEngine;

namespace CardGame.Manager.Deck
{
    public class DeckManager : MonoBehaviour
    {
        public Player owner;

        [Header("Kupki Domen")]
        public List<CardData> domainK_Pile;
        public List<CardData> domainP_Pile;
        public List<CardData> domainM_Pile;
        public List<CardData> domainD_Pile;

        [Header("UI")]
        public Transform handTransform;
        public GameObject cardPrefab;

        [Header("Domain Pile Transforms")]
        public Transform pileK_Transform;
        public Transform pileP_Transform;
        public Transform pileM_Transform;
        public Transform pileD_Transform;

        private List<Card> hand = new List<Card>();
        private Dictionary<CardDomain, List<CardData>> domainPiles = new Dictionary<CardDomain, List<CardData>>();

        void Start()
        {
            InitializeDeck();
            DrawInitialHand();
            UpdateDomainPileVisuals();
        }

        void InitializeDeck()
        {
            domainPiles[CardDomain.K] = new List<CardData>(domainK_Pile);
            domainPiles[CardDomain.P] = new List<CardData>(domainP_Pile);
            domainPiles[CardDomain.M] = new List<CardData>(domainM_Pile);
            domainPiles[CardDomain.D] = new List<CardData>(domainD_Pile);
        }

        public void DrawInitialHand()
        {
            foreach (CardDomain domain in System.Enum.GetValues(typeof(CardDomain)))
            {
                DrawCardFromDomain(domain);
            }
        }

        public Card DrawCardFromDomain(CardDomain domain)
        {
            if (!domainPiles.ContainsKey(domain) || domainPiles[domain].Count == 0)
            {
                Debug.LogWarning($"Brak kart w kupce domeny {domain}!");
                return null;
            }

            int index = 0;
            CardData cardData = domainPiles[domain][index];

            domainPiles[domain].RemoveAt(index);

            GameObject cardObj = Instantiate(cardPrefab, handTransform);
            Card card = cardObj.GetComponent<Card>();
            card.Initialize(cardData, owner);

            hand.Add(card);
            UpdateHandPositions();
            UpdateDomainPileVisuals();

            return card;
        }

        public void RemoveFromHand(Card card)
        {
            hand.Remove(card);
            UpdateHandPositions();
        }

        void UpdateHandPositions()
        {
            for (int i = 0; i < hand.Count; i++)
            {
                float spacing = 150f;
                float offset = (hand.Count - 1) * spacing / 2f;
                hand[i].transform.localPosition = new Vector3(i * spacing - offset, 0, 0);
            }
        }

        void UpdateDomainPileVisuals()
        {
            UpdatePileCounter(pileK_Transform, domainPiles[CardDomain.K].Count);
            UpdatePileCounter(pileP_Transform, domainPiles[CardDomain.P].Count);
            UpdatePileCounter(pileM_Transform, domainPiles[CardDomain.M].Count);
            UpdatePileCounter(pileD_Transform, domainPiles[CardDomain.D].Count);
        }

        void UpdatePileCounter(Transform pileTransform, int remainingCards)
        {
            /*            if (pileTransform == null) return;
                        int remaining = totalCards > 0 ? ((currentIndex % totalCards) == 0 ? totalCards : totalCards - (currentIndex % totalCards)) : 0;
                        Debug.Log(remaining.ToString());

                        Text counterText = pileTransform.GetComponentInChildren<Text>();
                        if (counterText)
                        {

                            counterText.text = $"{remaining}/{totalCards}";
                        }*/

            if (pileTransform == null) return;

            Text counterText = pileTransform.GetComponentInChildren<Text>();
            if (counterText)
            {
                // Now it simply shows how many are left in the list
                counterText.text = $"{remainingCards}";
            }
        }

        public List<Card> GetHand() => hand;

        public int GetPileSize(CardDomain domain)
        {
            return domainPiles.ContainsKey(domain) ? domainPiles[domain].Count : 0;
        }
    }
}
