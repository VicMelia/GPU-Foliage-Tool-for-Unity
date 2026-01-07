using System.Collections;
using UnityEngine;

public class FlashColor : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();

        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();
    }


    public IEnumerator Flash(float duration, Color flashColor, Color flashEmission)
    {
        foreach (var rend in _renderers)
        {
            _propBlock.Clear();
            rend.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_MainColor", flashColor);
            _propBlock.SetColor("_EmissionColor", flashEmission);
            rend.SetPropertyBlock(_propBlock);
        }
        yield return new WaitForSeconds(duration);
        foreach (var rend in _renderers)
        {
            _propBlock.Clear();
            rend.SetPropertyBlock(_propBlock);
        }
    }
}
