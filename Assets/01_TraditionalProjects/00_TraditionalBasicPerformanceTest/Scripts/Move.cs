using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Move : MonoBehaviour
{
    [SerializeField] public GameObject cube;
    public int CountToGen;



    public void ButtonClickToTest()
    {
        //SceneManager.LoadScene("Lerp");

        for (int i = 0; i < CountToGen; i++)
        {
            GameObject obj = Instantiate(cube, new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10)),
                Quaternion.identity);
            obj.AddComponent(typeof(MoveToEnd));
        }
    }
}
