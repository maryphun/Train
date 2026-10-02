using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlot : MonoBehaviour
{
    [SerializeField] private Button btn;
    [SerializeField] private GameObject isEmptyLabel;
    [SerializeField] private TMP_Text irl_date;
    [SerializeField] private TMP_Text irl_time;
    [SerializeField] private TMP_Text ingame_date;

    bool isSaveBtn; // this same script is used for load btn too so it is false when it's supposed to be clicked for loading save files.
    int slotID;

    public void Init(int slotID, bool isSaveBtn)
    {
        this.isSaveBtn = isSaveBtn;
        this.slotID = slotID;

        if (SaveLoad.Exists(slotID))
        {
            isEmptyLabel.SetActive(false);

            irl_date.gameObject.SetActive(true);
            irl_time.gameObject.SetActive(true);
            ingame_date.gameObject.SetActive(true);

            if (SaveLoad.TryReadMetadata(slotID, out SaveSlotMetadata info, out string error))
            {
                var local = info.SavedAtLocal;

                irl_date.text = local.ToString("yyyy/MM/dd");
                irl_time.text = local.ToString("HH:mm:ss");
                ingame_date.text = $"Day {info.InGameDay}";
            }
            else
            {
                Debug.LogWarning(error);
            }

            if (!isSaveBtn) btn.interactable = true;
        }        
        else
        {
            isEmptyLabel.SetActive(true);

            irl_date.gameObject.SetActive(false);
            irl_time.gameObject.SetActive(false);
            ingame_date.gameObject.SetActive(false);

            if (!isSaveBtn) btn.interactable = false;
        }
    }

    public void OnClickButton()
    {
        if (isSaveBtn)
        {
            SaveLoad.Save(slotID);
            Init(slotID, isSaveBtn);
        }
        else
        {
            SceneTransitionManager.Restart();
            SaveLoad.Load(slotID);
        }
    }
}
