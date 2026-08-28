using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAudio : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    public bool playFrogCroak;
    public bool toggleBgmMute;
    public bool toggleSfxMute;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (MusicManager.instance == null)
            return;

        if (playFrogCroak)
            MusicManager.instance.PlayFrogCroak();
        else
            MusicManager.instance.PlayButtonClick();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (MusicManager.instance == null)
            return;

        if (toggleBgmMute)
            MusicManager.instance.ToggleBgmMute();

        if (toggleSfxMute)
            MusicManager.instance.ToggleSfxMute();
    }
}
