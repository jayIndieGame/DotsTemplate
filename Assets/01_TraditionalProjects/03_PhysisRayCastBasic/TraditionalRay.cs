using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TraditionalRay : MonoBehaviour
{
    public GameObject Sphere;
    private Ray ray;
    private Ray sphereRay;
    private Transform sphereRayHit;
    private RaycastHit hitInfo;
    private RaycastHit hitGroundInfo;
    private Vector3 startPosition;
    private float timeInterval;

    private float height;
    private float radius;
    private Vector3 sphereProjectPoint;

    void Start()
    {
        Sphere = GameObject.Find("Sphere");
        sphereRay = new Ray
        {
            origin = Sphere.transform.position,
            direction = Vector3.down
        };
        sphereRayHit = null;
        timeInterval = 0f;
        height = Sphere.GetComponent<CapsuleCollider>().height;
        radius = Sphere.GetComponent<CapsuleCollider>().radius;

    }

    void Update()
    {
        #region 没有任何实际意义，仅仅是展示Api。
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //Ray customRay = new Ray
        //{
        //    origin = transform.position,
        //    direction = transform.forward
        //};

        //RaycastHit raycastHit;
        //RaycastHit raycastHitCustom;

        //Physics.Raycast(ray, out raycastHit);
        //Physics.Raycast(ray.origin, ray.direction, out raycastHit);
        //Physics.Raycast(customRay, out raycastHitCustom, float.MaxValue);

        //Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);
        //Debug.DrawRay(customRay.origin, customRay.direction * 100, Color.green);

        #endregion

        sphereProjectPoint = new Vector3(Sphere.transform.position.x,
            Sphere.transform.position.y - (height / 2 - radius ), Sphere.transform.position.z);
        sphereRay.origin = sphereProjectPoint;
        sphereRay.direction = sphereRayHit ==null? Vector3.down: -hitGroundInfo.normal;

        Debug.DrawRay(sphereProjectPoint, -hitGroundInfo.normal * 100,Color.red);

        if (Input.GetMouseButtonDown(0))
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            startPosition = sphereProjectPoint;
            timeInterval = 0f;
        }

        bool isHit = Physics.Raycast(ray, out hitInfo,100f,1<<LayerMask.NameToLayer("Walkable"));
        bool isHitGround = Physics.Raycast(sphereRay, out hitGroundInfo,100f, 1 << LayerMask.NameToLayer("Walkable"));


        #region 需要按照点击点看就加上这段代码

        //if (Input.GetMouseButtonUp(0) && isHit)
        //{
        //    Vector3 lookAtPosition = hitInfo.point;
        //    Sphere.transform.LookAt(lookAtPosition);
        //}

        #endregion


        if (isHit && isHitGround)
            if ((hitInfo.transform.name == "Plane" || hitInfo.transform.name == "Plane1"))
            {
                timeInterval += Time.deltaTime;
                Vector3 lerpPosition = Vector3.Lerp(startPosition, hitInfo.point, timeInterval);
                //(radius + radius/10)这里给了点容错率
                Vector3 targetPosition = new Vector3(lerpPosition.x, hitGroundInfo.point.y + (height/2-radius) + (radius + radius/10) * Vector3.Dot(-hitGroundInfo.normal.normalized,Vector3.down.normalized), lerpPosition.z);
                Sphere.transform.position = targetPosition;

            }


    }
}
