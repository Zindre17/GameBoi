using NAudio.Wave;

abstract class SoundChannel : Hardware
{

    protected readonly WaveOut waveEmitter = new WaveOut();
    protected IWaveProvider waveProvider;

    public abstract void Tick(Byte cpuCycles);

    public abstract override void Connect(Bus bus);

    protected void Play()
    {
        if (waveEmitter.PlaybackState != PlaybackState.Playing)
            waveEmitter.Play();
    }

}