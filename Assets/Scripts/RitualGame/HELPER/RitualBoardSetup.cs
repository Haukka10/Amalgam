using CardGame.Board.Lane;
using CardGame.Board.Slot;
using CardGame.UI.SlotClick;
using UnityEngine;
using UnityEngine.UI;
using static CardGame.Structures.Structures;

///
/// currently not in use
///
namespace CardGame.Helper.Board
{
    public class RitualBoardSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        public bool autoCreateBoard = false;

        [Header("Prefabs")]
        public GameObject slotPrefab;
        public GameObject cardPrefab;

        void Update()
        {
#if UNITY_EDITOR
            if (autoCreateBoard)
            {
                autoCreateBoard = false;
                CreateBoard();
            }
#endif
        }

        void CreateBoard()
        {
            Debug.Log("Tworzenie planszy Rytuały...");

            // Stwórz tory gracza i AI
            CreateLane("PlayerLane", Player.Human, new Vector3(0, -3, 0));
            CreateLane("AILane", Player.AI, new Vector3(0, 3, 0));

            Debug.Log("Plansza utworzona!");
        }

        void CreateLane(string name, Player owner, Vector3 position)
        {
            GameObject laneObj = new GameObject(name);
            laneObj.transform.SetParent(transform);
            laneObj.transform.localPosition = position;

            PlayerLane lane = laneObj.AddComponent<PlayerLane>();
            lane.owner = owner;

            // Stwórz sloty: BACK → MOD → MID → MOD → FRONT
            lane.backSlot = CreateSlot("BACK", SlotType.BACK, owner, new Vector3(-4, 0, 0), laneObj.transform);
            lane.modSlot1 = CreateSlot("MOD1", SlotType.MOD, owner, new Vector3(-2, 0, 0), laneObj.transform);
            lane.midSlot = CreateSlot("MID", SlotType.MID, owner, new Vector3(0, 0, 0), laneObj.transform);
            lane.modSlot2 = CreateSlot("MOD2", SlotType.MOD, owner, new Vector3(2, 0, 0), laneObj.transform);
            lane.frontSlot = CreateSlot("FRONT", SlotType.FRONT, owner, new Vector3(4, 0, 0), laneObj.transform);
        }

        BoardSlot CreateSlot(string name, SlotType type, Player owner, Vector3 localPos, Transform parent)
        {
            GameObject slotObj;

            if (slotPrefab != null)
            {
                slotObj = Instantiate(slotPrefab, parent);
            }
            else
            {
                slotObj = new GameObject(name);
                slotObj.transform.SetParent(parent);

                // Dodaj Image dla wizualizacji
                Image img = slotObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                RectTransform rt = slotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100, 150);
            }

            slotObj.name = name;
            slotObj.transform.localPosition = localPos;

            BoardSlot slot = slotObj.GetComponent<BoardSlot>();
            if (slot == null) slot = slotObj.AddComponent<BoardSlot>();

            slot.slotType = type;
            slot.owner = owner;

            // Dodaj click handler
            slotObj.AddComponent<SlotClickHandler>();

            return slot;
        }
    }
}
