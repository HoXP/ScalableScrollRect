using UnityEngine;

public class Main : MonoBehaviour
{
    private int n = 100;
    private bool ensureExit = false;

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if(ensureExit)
            {
                Application.Quit();
            }
            else
            {
                ensureExit = true;
                n = 100;
            }
        }

        if(ensureExit)
        {
            n--;
            if(n <= 0)
            {
                ensureExit = false;
            }
        }
    }

    private void OnGUI()
    {
        if (ensureExit)
        {
            GUILayout.Space(100);
            GUILayout.Label("再次按Esc退出");
        }
    }
}