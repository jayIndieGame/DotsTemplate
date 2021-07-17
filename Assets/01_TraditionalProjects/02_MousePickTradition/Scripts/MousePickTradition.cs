using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MousePickTradition : MonoBehaviour
{
    private RaycastHit raycastHitInfo;

    private Vector3 worldHitPoint;
    private Vector3 localHitPoint;
    private GameObject springJointObj;
    private GameObject hitGameObject;
    private SpringJoint springJoint;
    private Rigidbody rigidbody;
    private float cameraZ;
    #region Spring Propertities

    [HideInInspector]public bool AutoConfigureConnnected = false;
    public float Spring;
    public float Damper;
    public float MinDistance;
    public float MaxDistance;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        springJointObj = new GameObject();
        rigidbody = springJointObj.AddComponent<Rigidbody>();
        springJoint = springJointObj.AddComponent<SpringJoint>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        springJoint.damper = Damper;
        springJoint.spring = Spring;
        springJoint.minDistance = MinDistance;
        springJoint.maxDistance = MaxDistance;

        //springJointObj.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0)&& (Camera.main != null))
        {

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out raycastHitInfo, 100f) && !raycastHitInfo.rigidbody.isKinematic)
            {
                hitGameObject = raycastHitInfo.transform.gameObject;
                worldHitPoint = raycastHitInfo.point;
                localHitPoint = raycastHitInfo.transform.InverseTransformPoint(worldHitPoint);
            }
            springJointObj.SetActive(true);


            Vector3 anchor = new Vector3(0,0,0);
            springJoint.anchor = anchor;
            springJoint.connectedAnchor = localHitPoint;
            springJoint.maxDistance = 0f;
            springJoint.connectedBody = raycastHitInfo.rigidbody;

            cameraZ = hitGameObject.transform.position.z - Camera.main.transform.position.z;
            //DragObject(worldHitPoint); TODO 自身带的Spring调整参数着实不能达到满意的效果。而且还有各种bug。运动也得自己写了
        }

        if (Input.GetMouseButton(0))
        {
            if(hitGameObject == null) return;;

            springJointObj.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y, cameraZ));
        }

        if (Input.GetMouseButtonUp(0))
        {
            springJointObj.SetActive(false);
        }
    }

}
