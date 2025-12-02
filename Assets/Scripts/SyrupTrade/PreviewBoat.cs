using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using WorldUtil;
using Unity.Netcode;

public class PreviewBoat : NetworkBehaviour
{
    public float movementSpeed = 1.0f;

    private List<Hex> targetRoute;
    private Vector3 currentTargetPos;

    private bool travelToTarget = false;
    private const float targetBuffer = 1f;

    private int routeProg = 0;
    private int routeMax = 0;
    private float routeLength;

    public void SetRoute(List<Hex> route)
    {
        // Clone the route since we will be altering it
        targetRoute = route;
        routeProg = 0;
        routeMax = route.Count - 1;
        UpdateTargetPos();
        travelToTarget = true;
    }

    public void FixedUpdate()
    {
        if (!travelToTarget) return;
        if (NetworkManager.Singleton != null && !IsServer) return;
        
        Vector3 pos = gameObject.transform.position;
        float dist = Vector3.Distance(pos, currentTargetPos);
        if (dist < targetBuffer)
        {
            if (routeProg == routeMax)
            {
                if (NetworkManager.Singleton != null)
                {
                    GetComponent<NetworkObject>().Despawn();
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
            else
            {
                routeProg++;
                UpdateTargetPos();
            }
        }
        else
        {
            float delta = movementSpeed / dist;
            if (delta > 1f) delta = 1f;
            Vector3 toward = Vector3.Lerp(pos, currentTargetPos, delta);
            gameObject.transform.position = toward;
        }
    }

    private void UpdateTargetPos()
    {
        // Set target
        currentTargetPos = targetRoute[routeProg].waterMesh
            .GetComponent<MeshRenderer>().bounds.center;
        // Face target
        Vector3 pos = gameObject.transform.position;
        gameObject.transform.localRotation
            = Quaternion.LookRotation(pos - currentTargetPos);
    }
}
