using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int sceneToLoad = SceneManager.GetActiveScene().buildIndex == 0 ? 1 : 0;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
