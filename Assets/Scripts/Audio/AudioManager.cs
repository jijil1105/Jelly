using UnityEngine;

/// <summary>
/// オーディオ関係を取りまとめるクラス
/// </summary>
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    private AudioAnalyzer _audioAnalyzer = null;

    /// <inheritdoc/>
    public float[] GetLogBands(float smoothFactor = 0.2f) => _audioAnalyzer.GetLogBands(smoothFactor);

    public void Initialize()
    {
        _audioAnalyzer = new AudioAnalyzer(_audioSource, offset: 1);
        _audioSource.Play();
    }
    
    public void OnUpdate()
    {
        _audioAnalyzer.UpdateSpectrum();
    }
}
