using CardGame.Board.Slot;
using CardGame.CardObj;
using CardGame.Manager.Main;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static CardGame.Structures.Structures;

namespace CardGame.UI.CardDrag
{

    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Card card;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector3 originalPosition;

        void Awake()
        {
            card = GetComponent<Card>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();

            if (!canvasGroup)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (RitualGameManager.Instance.currentState != RitualGameManager.GameState.PlayerTurn) return;
            if (card.owner != Player.Human) return;

            originalParent = transform.parent;
            originalPosition = transform.position;

            // Przenieś na wierzch UI
            transform.SetParent(canvas.transform);

            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false;

            // Podświetl możliwe sloty
            RitualGameManager.Instance.HighlightValidSlots(card);
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Sprawdź czy upuszczono na slot
            BoardSlot targetSlot = GetSlotUnderPointer(eventData);

            if (targetSlot != null && targetSlot.CanPlaceCard(card))
            {
                // Zagraj kartę
                RitualGameManager.Instance.PlayCardOnSlot(card, targetSlot);
            }
            else
            {
                // Wróć do ręki
                transform.SetParent(originalParent);
                transform.position = originalPosition;
            }

            RitualGameManager.Instance.ClearHighlights();
        }

        BoardSlot GetSlotUnderPointer(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                BoardSlot slot = result.gameObject.GetComponent<BoardSlot>();
                if (slot != null) return slot;
            }

            return null;
        }
    }
}
