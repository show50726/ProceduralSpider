using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SpiderController : MonoBehaviour
{
    [System.Serializable]
    private struct LegData
    {
        public Transform LegIKTarget;
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
    [SerializeField]
    private float _bodyCalibratingSpeed = 1.0f;
    [SerializeField]
    private AnimationCurve _animationCurve;

    // TODO: Use sphere cast to the obstacles
    // TODO: Add time offset
    private int _index = 0;
    private Vector3[] _footWorldPosition;
    private bool[] _isMoving;

    private int _selfLayer;

    private void Awake()
    {
        if(Legs.Length < 2)
        {
            Debug.LogWarning("Leg data is empty. The Coroutine will not be triggered.");
        }

        _isMoving = new bool[Legs.Length];
        _footWorldPosition = new Vector3[Legs.Length];
        _bodyOffset = transform.position.y - Legs[0].DefaultTransform.position.y;
        _selfLayer = LayerMask.GetMask("Characters");

        for (int i = 0; i < Legs.Length; i++)
        {
            _footWorldPosition[i] = Legs[i].LegIKTarget.position;
        }

        StartCoroutine(_UpdateLegs());
    }

    private void _UpdateDefaultLegsPosition()
    {
        foreach (var leg in Legs)
        {
            // TODO: exclude self layer
            if (!Physics.Raycast(
                leg.DefaultTransform.position + transform.up * _bodyOffset,
                -transform.up,
                out var hit,
                _detectingDistance, 
                ~_selfLayer))
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

    private void _UpdateIKTargetPosition()
    {
        for (int i = 0; i < Legs.Length; i++)
        {
            Legs[i].LegIKTarget.position = _footWorldPosition[i];
        }
    }

    private float _CalculateBodyHeightCalibration()
    {
        // Candidate 1: Detect obstacles below the body
        float candidateY1 = transform.position.y;
        if (Physics.Raycast(
                transform.position,
                -transform.up,
                out var hit,
                _detectingDistance,
                _selfLayer))
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
        // TODO: Calibrate rotation

        _UpdateDefaultLegsPosition();

        _UpdateIKTargetPosition();

        _UpdateBodyPosition();
    }

    private void _TryMove(int legIndex)
    {
        if (_isMoving[legIndex])
            return;

        var defaultTransform = Legs[legIndex].DefaultTransform;

        float distance = Vector3.Distance(_footWorldPosition[legIndex], defaultTransform.position);
        if (distance < _maxDistance)
            return;

        Vector3 startPoint = _footWorldPosition[legIndex];
        Vector3 endPoint = defaultTransform.position;
        Vector3 centerPos = (startPoint + endPoint) / 2.0f;
        centerPos += defaultTransform.up * distance / 2.0f;

        Sequence movementSequence = DOTween.Sequence();

        float minDuration = 0.05f;
        float duration = Mathf.Max(minDuration, distance / _moveSpeed);

        float progress = 0.0f;
        movementSequence.Append(DOTween.To(
            () => progress,
            x => progress = x,
            1.0f,
            duration)
            .OnUpdate(() => {
                float evaluatedValue = _animationCurve.Evaluate(progress);
                float xPos = Mathf.Lerp(startPoint.x, endPoint.x, progress);
                float yPos = Mathf.Lerp(startPoint.y, endPoint.y, evaluatedValue);
                float zPos = Mathf.Lerp(startPoint.z, endPoint.z, progress);
                _footWorldPosition[legIndex] = new Vector3(xPos, yPos, zPos);
            })
            .OnStart(() => _isMoving[legIndex] = true)
            .OnComplete(() =>
            {
                _footWorldPosition[legIndex] = endPoint;
                _isMoving[legIndex] = false;
            }));
        movementSequence.Play();
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
