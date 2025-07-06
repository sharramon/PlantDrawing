using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineManager : Singleton<CoroutineManager>
{
    private Dictionary<string, Coroutine> _activeCoroutines = new();

    public Coroutine RunWithoutID(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public Coroutine Run(string id, IEnumerator routine)
    {
        if (_activeCoroutines.ContainsKey(id))
        {
            StopCoroutine(_activeCoroutines[id]);
            _activeCoroutines.Remove(id);
        }

        Coroutine c = StartCoroutine(Wrap(id, routine));
        _activeCoroutines[id] = c;
        return c;
    }

    private IEnumerator Wrap(string id, IEnumerator routine)
    {
        yield return routine;
        _activeCoroutines.Remove(id);
    }

    public void Stop(string id)
    {
        if (_activeCoroutines.TryGetValue(id, out Coroutine c))
        {
            StopCoroutine(c);
            _activeCoroutines.Remove(id);
        }
    }

    public bool IsRunning(string id)
    {
        return _activeCoroutines.ContainsKey(id);
    }

    public void StopAll()
    {
        foreach (var pair in _activeCoroutines)
        {
            StopCoroutine(pair.Value);
        }
        _activeCoroutines.Clear();
    }
}
