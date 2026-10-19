using System;
using UnityEngine;

namespace RupeeRanger.Town
{
    public enum BuildingType
    {
        Home,
        College,
        Shop,
        BusStop,
        Hospital,
        FriendsHouse,
        GameCentre,
        Bank,
        WordSearchHouse,
        WordFinderHouse,
        MathsSpeedHouse,
        MathsPuzzleHouse,
        Library,
        Park,
        CommunityHall,
        Bakery
    }

    [RequireComponent(typeof(Collider2D))]
    public class TownBuilding : MonoBehaviour
    {
        [Header("Building Info")]
        public BuildingType buildingType;
        public string buildingName = "Building";
        [TextArea(2, 4)]
        public string description = "Building description.";
        public Sprite buildingIcon;
        public Color mapMarkerColor = Color.yellow;

        public bool IsGameHouse()
        {
            return buildingType == BuildingType.WordSearchHouse ||
                   buildingType == BuildingType.WordFinderHouse ||
                   buildingType == BuildingType.MathsSpeedHouse ||
                   buildingType == BuildingType.MathsPuzzleHouse ||
                   buildingType == BuildingType.GameCentre ||
                   buildingType == BuildingType.Library;
        }

        [Header("State")]
        public bool isPlayerInside = false;

        public event Action<TownBuilding> OnBuildingEntered;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController2D>() != null)
            {
                isPlayerInside = true;
                PlayerController2D player = other.GetComponent<PlayerController2D>();
                if (player != null)
                {
                    player.SetNearbyBuilding(this);
                }

                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.ShowBuildingPrompt(this);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController2D>() != null)
            {
                isPlayerInside = false;
                PlayerController2D player = other.GetComponent<PlayerController2D>();
                if (player != null)
                {
                    player.ClearNearbyBuilding(this);
                }

                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.HideBuildingPrompt(this);
                }
            }
        }

        /// <summary>
        /// Called when the player presses E / Interact while inside this building's trigger zone.
        /// </summary>
        public void Interact()
        {
            if (!isPlayerInside) return;

            Debug.Log($"[TownBuilding] Player entered {buildingName} ({buildingType})");
            OnBuildingEntered?.Invoke(this);

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnPlayerInteractBuilding(this);
            }
            else if (TownHUD.Instance != null)
            {
                TownHUD.Instance.OpenBuildingDialog(this);
            }
        }
    }
}
