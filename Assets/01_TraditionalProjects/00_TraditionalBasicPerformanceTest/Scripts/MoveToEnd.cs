using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToEnd : MonoBehaviour
{
    private Vector3 endVector3;
    void Start()
    {
        endVector3 = new Vector3(Random.Range(-8, 8), Random.Range(-5, 5), 5);
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, endVector3) > 0.2f)
        {
            transform.position = Vector3.Lerp(transform.position, endVector3, 0.01f);
        }
    }
}
