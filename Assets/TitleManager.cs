using UnityEngine;

public class TitleManager : MonoBehaviour
{
    private void Start()
    {
        
    }

    public void OnClickStart()
    {
        // init player profile
        PlayerProfile.Initialization();

        // change scene
        SceneTransitionManager.Instance.LoadScene("MainMenu", 0.75f);
    }

    public void OnClickLoad()
    {
        // todo
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}
