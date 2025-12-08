using CardGame.CardObj;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CardGame.Structures.Structures;

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
        private Dictionary<CardDomain, int> domainIndices = new Dictionary<CardDomain, int>();

        void Start()
        {
            InitializeDeck();
            DrawInitialHand();
            UpdateDomainPileVisuals();
        }

        void InitializeDeck()
        {
            // Przypisz kupki do słownika
            domainPiles[CardDomain.K] = new List<CardData>(domainK_Pile);
            domainPiles[CardDomain.P] = new List<CardData>(domainP_Pile);
            domainPiles[CardDomain.M] = new List<CardData>(domainM_Pile);
            domainPiles[CardDomain.D] = new List<CardData>(domainD_Pile);

            // Inicjalizuj indeksy dla każdej domeny
            foreach (CardDomain domain in System.Enum.GetValues(typeof(CardDomain)))
            {
                domainIndices[domain] = 0;
            }
        }

        public void DrawInitialHand()
        {
            // Dobierz po 1 karcie z każdej domeny (K, P, M, D)
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

            // Pobierz następną kartę z kupki (cyklicznie)
            int index = domainIndices[domain] % domainPiles[domain].Count;
            CardData cardData = domainPiles[domain][index];
            domainIndices[domain]++;

            // Stwórz kartę
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
            // Aktualizuj liczniki kart w kupkach (opcjonalnie)
            UpdatePileCounter(pileK_Transform, domainPiles[CardDomain.K].Count, domainIndices[CardDomain.K]);
            UpdatePileCounter(pileP_Transform, domainPiles[CardDomain.P].Count, domainIndices[CardDomain.P]);
            UpdatePileCounter(pileM_Transform, domainPiles[CardDomain.M].Count, domainIndices[CardDomain.M]);
            UpdatePileCounter(pileD_Transform, domainPiles[CardDomain.D].Count, domainIndices[CardDomain.D]);
        }

        void UpdatePileCounter(Transform pileTransform, int totalCards, int currentIndex)
        {
            if (pileTransform == null) return;

            Text counterText = pileTransform.GetComponentInChildren<Text>();
            if (counterText)
            {
                int remaining = totalCards > 0 ? ((currentIndex % totalCards) == 0 ? totalCards : totalCards - (currentIndex % totalCards)) : 0;
                counterText.text = $"{remaining}/{totalCards}";
            }
        }

        public List<Card> GetHand() => hand;

        public int GetPileSize(CardDomain domain)
        {
            return domainPiles.ContainsKey(domain) ? domainPiles[domain].Count : 0;
        }
    }
}
