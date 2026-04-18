using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class EnterTrain : MonoBehaviour
{
    [SerializeField] private Collider _enterTrigger;

    private readonly Subject<Unit> _onEnterTrain = new();

    public IObservable<Unit> OnEnterTrain => _onEnterTrain;

    public void Initialize()
    {
        _enterTrigger.OnTriggerEnterAsObservable().Where(other => other.CompareTag("Player")).Subscribe(_ => _onEnterTrain.OnNext(Unit.Default)).AddTo(this);
    }

    void OnDestroy()
    {
        _onEnterTrain.Dispose();
    }
}
