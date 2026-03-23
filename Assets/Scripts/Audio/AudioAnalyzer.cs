using UnityEngine;

/// <summary>
/// スペクトラムの解析と解析したデータを外部に公開するクラス
/// </summary>
public class AudioAnalyzer
{
    private AudioSource _audioSource;
    private int _spectrumSize;
    private FFTWindow _fftWindow;
    private int _bandCount;

    private float[] _spectrumData;
    private float[] _bandData;
    private int[] _bandStarts;
    private int[] _bandEnds;
    private int _offset;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="audioSource">解析するオーディオソース</param>
    /// <param name="spectrumSize">解析するスペクトラムのサイズ</param>
    /// <param name="bandCount">スペクトラムを分割する数</param>
    /// <param name="fftWindow">窓関数の種類</param>
    /// <param name="offset">解析開始位置のシフト量</param>
    public AudioAnalyzer(AudioSource audioSource, int spectrumSize = 512, int bandCount = 8, FFTWindow fftWindow = FFTWindow.Triangle, int offset = 0)
    {
        _audioSource = audioSource;
        _spectrumSize = Mathf.Max(2, spectrumSize);
        _fftWindow = fftWindow;
        _bandCount = Mathf.Max(1, bandCount);
        _offset = offset;

        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        _spectrumData = new float[_spectrumSize];
        _bandData = new float[_bandCount];
        _bandStarts = new int[_bandCount];
        _bandEnds = new int[_bandCount];

        int specLen = _spectrumData.Length;
        int offset = Mathf.Max(0, _offset);

        for (int idx = 0; idx < _bandCount; idx++)
        {
            int exp = idx + offset;
            // 符号ビットに達するのを防止
            if (exp >= 30) exp = 30;
            // 2乗して周波数が上がるほどレンジを広くする
            int start = Mathf.Clamp((1 << exp) - 1, 0, specLen - 1);
            int end = Mathf.Clamp((1 << (exp + 1)) - 1, 0, specLen - 1);
            _bandStarts[idx] = start;
            _bandEnds[idx] = Mathf.Max(start, end);
        }
    }

    /// <summary>
    /// スペクトラムの更新
    /// </summary>
    public void UpdateSpectrum()
    {
        if (_audioSource.isPlaying)
        {
            _audioSource.GetSpectrumData(_spectrumData, 0, _fftWindow);
        }
        else
        {
            System.Array.Clear(_spectrumData, 0, _spectrumData.Length);
        }
    }

    /// <summary>
    /// スペクトラムの各レンジ毎の平均を取得する
    /// </summary>
    /// <param name="smoothFactor">光の点滅具合を見てこの値を調整する</param>
    /// <returns>各レンジの平均値</returns>
    public float[] GetLogBands(float smoothFactor = 0.2f)
    {
        for (int i = 0; i < _bandCount; i++)
        {
            int start = _bandStarts[i];
            int end = _bandEnds[i];
            float sum = 0f;
            var spec = _spectrumData;
            for (int j = start; j <= end; j++) sum += spec[j];
            int count = Mathf.Max(1, end - start + 1);
            float avg = sum / count;
            // そのまま値を返すと変化量が激しいので対数的に圧縮し、smoothFactorでフレーム間の変化を平滑化して更新
            float compressed = Mathf.Log10(1f + avg * 100f);
            _bandData[i] = Mathf.Lerp(_bandData[i], compressed, Mathf.Clamp01(smoothFactor));
        }
        return _bandData;
    }
}
