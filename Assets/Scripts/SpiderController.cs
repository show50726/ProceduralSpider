using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpiderController : MonoBehaviour
{
    public Transform[] targets;
    public Transform[] homes;
    public float maxDistance = 3f;
    public float moveSpeed = 4f;
    public float bodyOffset = 1f;

    int index = 0;
    bool[] moving = new bool[4];

    private void Awake()
    {
        bodyOffset = transform.position.y - targets[1].position.y;
        Debug.Log(bodyOffset);
        StartCoroutine(LegUpdateCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var h in homes)
        {
            RaycastHit hit;
            if (Physics.Raycast(h.transform.position + transform.up * 0.1f, -transform.up, out hit, 5))
            {
                Debug.DrawLine(hit.point, hit.point + Vector3.up);
                //Debug.Log(hit.transform.gameObject.name);
                h.transform.position = hit.point;
            }
        }
    }


    // TODO
    private void FixedUpdate()
    {
        /*
        Vector3 sum = Vector3.zero;
        foreach(var t in homes)
        {
            sum += t.position;
        }
        sum /= 4;
        transform.position = transform.up * (bodyOffset + sum.y);
        */
    }

    void TryMove(int i)
    {
        if (moving[i]) return;
        float dis = Vector3.Distance(targets[i].transform.position, homes[i].transform.position);
        if (dis > maxDistance)
        {
            Vector3 startPoint = targets[i].position;
            Vector3 endPoint = homes[i].position;

            moving[i] = true;
            Vector3 centerPos = (startPoint + endPoint) / 2;
            centerPos += homes[i].transform.up * Vector3.Distance(startPoint, endPoint) / 2f;

            Sequence mysq = DOTween.Sequence();
            mysq.Append(targets[i].DOMove(centerPos, dis / moveSpeed / 2));
            mysq.Append(targets[i].DOMove(homes[i].transform.position, dis / moveSpeed / 2).OnComplete(() => moving[i] = false));
            mysq.Play();
        }
    }

    IEnumerator LegUpdateCoroutine()
    {
        while (true)
        {
            do
            {
                TryMove(0);
                TryMove(1);

                yield return null;
            } while (moving[0] || moving[1]);

            do
            {
                TryMove(2);
                TryMove(3);

                yield return null;
            } while (moving[2] || moving[3]);
        }
    }


}
