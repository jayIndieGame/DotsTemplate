using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    [SerializeField] public GameObject cube;
    public int CountToGen;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < CountToGen; i++)
        {
            GameObject obj = Instantiate(cube, new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10)),
                Quaternion.identity);
            obj.AddComponent(typeof(MoveToEnd));
        }
    }
}
