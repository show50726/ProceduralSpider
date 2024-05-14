using DG.Tweening;
using DitzelGames.FastIK;
using System.Collections;
using UnityEngine;

public class SpiderController : MonoBehaviour
{
    [System.Serializable]
    private struct LegData
    {
        public FastIKFabric LegIK;
        public Transform DefaultTransform;
    }

    [SerializeField]
    private LegData[] Legs;
    [SerializeField]
    private float _maxDistance = 3f;
    [SerializeField]
    private float _moveSpeed = 4f;
    [SerializeField]
    private float _bodyOffset = 1f;
    [SerializeField]
    private float _detectingDistance = 5.0f;

    private int _index = 0;
    private bool[] _moving;

    private void Awake()
    {
        if(Legs.Length == 0)
        {
            Debug.LogWarning("Leg data is empty. The Coroutine will not be triggered.");
        }

        _moving = new bool[Legs.Length];
        _bodyOffset = transform.position.y - Legs[0].DefaultTransform.position.y;

        _AlignTargetToFoot();

        StartCoroutine(_UpdateLegs());
    }

    private void _AlignTargetToFoot()
    {
        foreach(var leg in Legs)
        {
            leg.LegIK.Target.position = leg.LegIK.transform.position;
        }
    }

    private void Update()
    {
        foreach (var leg in Legs)
        {
            if (!Physics.Raycast(
                leg.DefaultTransform.position + transform.up * _bodyOffset, 
                -transform.up, 
                out var hit,
                _detectingDistance))
                continue;

            leg.DefaultTransform.position = hit.point;
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

    private void _TryMove(int legIndex)
    {
        if (_moving[legIndex])
            return;

        var legIKTarget = Legs[legIndex].LegIK.Target;
        var defaultTransform = Legs[legIndex].DefaultTransform;

        float distance = Vector3.Distance(legIKTarget.position, defaultTransform.position);
        if (distance > _maxDistance)
        {
            Vector3 startPoint = legIKTarget.position;
            Vector3 endPoint = defaultTransform.position;

            _moving[legIndex] = true;
            Vector3 centerPos = (startPoint + endPoint) / 2;
            centerPos += defaultTransform.up * Vector3.Distance(startPoint, endPoint) / 2f;

            Sequence movementSequence = DOTween.Sequence();
            movementSequence.Append(legIKTarget.DOMove(centerPos, distance / _moveSpeed / 2.0f));
            movementSequence.Append(
                legIKTarget.DOMove(endPoint, distance / _moveSpeed / 2.0f)
                .OnComplete(() => _moving[legIndex] = false));
            movementSequence.Play();
        }
    }

    private IEnumerator _UpdateLegs()
    {
        while (true)
        {
            do
            {
                _TryMove(0);
                _TryMove(1);

                yield return null;
            } while (_moving[0] || _moving[1]);

            do
            {
                _TryMove(2);
                _TryMove(3);

                yield return null;
            } while (_moving[2] || _moving[3]);
        }
    }

    private void OnDrawGizmos()
    {
        foreach(var leg in Legs)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(leg.DefaultTransform.position, 0.1f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                leg.DefaultTransform.position + transform.up * _bodyOffset,
                leg.DefaultTransform.position + transform.up * _bodyOffset - transform.up * _detectingDistance);
        }
    }
}
