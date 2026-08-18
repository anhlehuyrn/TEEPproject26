using UnityEngine;
using UnityEngine.UI;
using System;

public class StampManager : MonoBehaviour
{
    [Serializable]
    public class Stamp
    {
        public string targetName; 
        public GameObject stampObject;
        [HideInInspector] public Image cachedImage;
    }

    [Header("Stamp Database")]
    public Stamp[] stamps;

    private void Awake()
    {
        // Cache UI components at boot to avoid runtime GetComponent calls
        if (stamps == null) return;
        for (int i = 0; i < stamps.Length; i++)
        {
            if (stamps[i] != null && stamps[i].stampObject != null)
            {
                stamps[i].cachedImage = stamps[i].stampObject.GetComponent<Image>();
            }
        }
    }

    private void OnEnable()
    {
        RefreshStamps();
    }

    public void RefreshStamps()
    {
        if (stamps == null) return;

        for (int i = 0; i < stamps.Length; i++)
        {
            Stamp stamp = stamps[i];
            if (stamp == null || stamp.stampObject == null) continue;

            bool isCollected = PlayerPrefs.GetInt("Stamp_" + stamp.targetName, 0) == 1;

            if (stamp.cachedImage != null)
            {
                stamp.cachedImage.enabled = isCollected;
            }
            else
            {
                stamp.stampObject.SetActive(isCollected);
            }
        }
    }

    public void ResetAllStampsForTesting()
    {
        if (stamps == null) return;
        for (int i = 0; i < stamps.Length; i++)
        {
            if (stamps[i] != null) PlayerPrefs.SetInt("Stamp_" + stamps[i].targetName, 0);
        }
        PlayerPrefs.Save();
        RefreshStamps();
    }
}