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
    [SerializeField, Range(0.0f, 1.0f)]
    private float _bodyCalibratingSpeed = 1.0f;

    private int _index = 0;
    private bool[] _isMoving;

    private void Awake()
    {
        if(Legs.Length < 2)
        {
            Debug.LogWarning("Leg data is empty. The Coroutine will not be triggered.");
        }

        _isMoving = new bool[Legs.Length];
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

    private void _UpdateDefaultLegsPosition()
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

    private void _UpdateBodyPosition()
    {
        var position = transform.position;
        var targetYPos = _CalculateBodyHeightCalibration();
        var calibratedYPos = Mathf.Lerp(position.y, targetYPos, Time.deltaTime * _bodyCalibratingSpeed);
        position.y = calibratedYPos;
        transform.position = position;
    }

    private float _CalculateBodyHeightCalibration()
    {
        // Candidate 1: Detect obstacles below the body
        float candidateY1 = transform.position.y;
        if (Physics.Raycast(
                transform.position,
                -transform.up,
                out var hit,
                _detectingDistance))
            candidateY1 = hit.point.y + _bodyOffset;

        // Candidate 2: Average of lowest leg and highest leg's Y position
        float maxx = Legs[0].DefaultTransform.position.y;
        float minn = Legs[0].DefaultTransform.position.y;
        foreach (var leg in Legs)
        {
            minn = Mathf.Min(minn, leg.DefaultTransform.position.y);
            maxx = Mathf.Max(maxx, leg.DefaultTransform.position.y);
        }
        float candidateY2 = (maxx + minn) / 2.0f + _bodyOffset;

        return Mathf.Max(candidateY1, candidateY2);
    }

    private void FixedUpdate()
    {
        _UpdateDefaultLegsPosition();

        _UpdateBodyPosition();
    }

    private void _TryMove(int legIndex)
    {
        if (_isMoving[legIndex])
            return;

        var legIKTarget = Legs[legIndex].LegIK.Target;
        var defaultTransform = Legs[legIndex].DefaultTransform;

        float distance = Vector3.Distance(legIKTarget.position, defaultTransform.position);
        if (distance > _maxDistance)
        {
            Vector3 startPoint = legIKTarget.position;
            Vector3 endPoint = defaultTransform.position;

            _isMoving[legIndex] = true;
            Vector3 centerPos = (startPoint + endPoint) / 2;
            centerPos += defaultTransform.up * Vector3.Distance(startPoint, endPoint) / 2f;

            Sequence movementSequence = DOTween.Sequence();
            movementSequence.Append(legIKTarget.DOMove(centerPos, distance / _moveSpeed / 2.0f));
            movementSequence.Append(
                legIKTarget.DOMove(endPoint, distance / _moveSpeed / 2.0f)
                .OnComplete(() => _isMoving[legIndex] = false));
            movementSequence.Play();
        }
    }

    private bool _IsAnyOddLegMoving()
    {
        for(int i = 1; i < Legs.Length; i += 2)
        {
            if (_isMoving[i])
                return true;
        }
        return false;
    }

    private bool _IsAnyEvenLegMoving()
    {
        for (int i = 0; i < Legs.Length; i += 2)
        {
            if (_isMoving[i])
                return true;
        }
        return false;
    }

    private IEnumerator _UpdateLegs()
    {
        while (true)
        {
            do
            {
                // Move even legs
                for(int i = 0; i < Legs.Length; i += 2)
                {
                    _TryMove(i);
                }

                yield return null;
            } while (_IsAnyEvenLegMoving());

            do
            {
                // Move odd legs
                for (int i = 1; i < Legs.Length; i += 2)
                {
                    _TryMove(i);
                }

                yield return null;
            } while (_IsAnyOddLegMoving());
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
