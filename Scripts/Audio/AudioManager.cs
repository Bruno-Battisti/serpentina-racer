using System;
using Godot;

namespace SerpentinaRacer.Audio;

/// <summary>
/// Generates every sound effect procedurally (pure PCM synthesis) so the project
/// ships with zero binary audio assets, and plays them back through a small pool
/// of AudioStreamPlayers.
/// </summary>
public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	private const int MixRate = 22050;

	private AudioStreamPlayer _enginePlayer;
	private AudioStreamPlayer _sfxBeep;
	private AudioStreamPlayer _sfxGo;
	private AudioStreamPlayer _sfxCrash;
	private AudioStreamPlayer _sfxLap;
	private AudioStreamPlayer _sfxClick;
	private AudioStreamPlayer _sfxCheckpoint;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		_enginePlayer = CreatePlayer(BuildEngineLoop(), -8f);
		_sfxBeep = CreatePlayer(BuildTone(880, 0.12f, 0.35f), -6f);
		_sfxGo = CreatePlayer(BuildTone(1320, 0.3f, 0.4f), -4f);
		_sfxCrash = CreatePlayer(BuildNoiseBurst(0.25f), -4f);
		_sfxLap = CreatePlayer(BuildChime(), -5f);
		_sfxClick = CreatePlayer(BuildTone(660, 0.05f, 0.25f), -8f);
		_sfxCheckpoint = CreatePlayer(BuildTone(990, 0.08f, 0.2f), -10f);
	}

	private AudioStreamPlayer CreatePlayer(AudioStreamWav stream, float volumeDb)
	{
		var player = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = volumeDb,
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(player);
		return player;
	}

	public void StartEngine()
	{
		if (!_enginePlayer.Playing)
			_enginePlayer.Play();
	}

	public void StopEngine()
	{
		_enginePlayer.Stop();
	}

	/// <param name="throttle01">0..1 throttle amount</param>
	/// <param name="speedRatio01">0..1 of current speed vs top speed</param>
	public void UpdateEngine(float throttle01, float speedRatio01)
	{
		float pitch = 0.65f + speedRatio01 * 1.65f + throttle01 * 0.25f;
		_enginePlayer.PitchScale = Mathf.Clamp(pitch, 0.5f, 2.6f);
		_enginePlayer.VolumeDb = Mathf.Lerp(-14f, -3f, Mathf.Clamp(0.3f + throttle01 * 0.7f, 0f, 1f));
	}

	public void PlayBeep() => _sfxBeep.Play();
	public void PlayGo() => _sfxGo.Play();
	public void PlayCrash() => _sfxCrash.Play();
	public void PlayLap() => _sfxLap.Play();
	public void PlayClick() => _sfxClick.Play();
	public void PlayCheckpoint() => _sfxCheckpoint.Play();

	private static AudioStreamWav CreateStream(byte[] data, bool loop)
	{
		return new AudioStreamWav
		{
			Data = data,
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = MixRate,
			Stereo = false,
			LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled,
			LoopBegin = 0,
			LoopEnd = loop ? data.Length / 2 : 0
		};
	}

	private static AudioStreamWav BuildTone(float freq, float duration, float amplitude)
	{
		int samples = (int)(MixRate * duration);
		var data = new byte[samples * 2];
		for (int i = 0; i < samples; i++)
		{
			float t = (float)i / MixRate;
			float env = 1f - (float)i / samples;
			float v = Mathf.Sin(Mathf.Tau * freq * t) * amplitude * env;
			WriteSample(data, i, v);
		}
		return CreateStream(data, false);
	}

	private static AudioStreamWav BuildChime()
	{
		float duration = 0.5f;
		int samples = (int)(MixRate * duration);
		var data = new byte[samples * 2];
		float[] freqs = { 660f, 990f };
		for (int i = 0; i < samples; i++)
		{
			float t = (float)i / MixRate;
			float env = 1f - (float)i / samples;
			int which = t < duration * 0.4f ? 0 : 1;
			float v = Mathf.Sin(Mathf.Tau * freqs[which] * t) * 0.35f * env;
			WriteSample(data, i, v);
		}
		return CreateStream(data, false);
	}

	private static AudioStreamWav BuildNoiseBurst(float duration)
	{
		int samples = (int)(MixRate * duration);
		var data = new byte[samples * 2];
		var rng = new Random(1234);
		for (int i = 0; i < samples; i++)
		{
			float env = 1f - (float)i / samples;
			float v = ((float)rng.NextDouble() * 2f - 1f) * 0.6f * env;
			WriteSample(data, i, v);
		}
		return CreateStream(data, false);
	}

	/// <summary>
	/// Builds one perfectly loopable period of a buzzy multi-harmonic waveform
	/// that reads as an idling combustion engine; pitch is modulated at runtime.
	/// </summary>
	private static AudioStreamWav BuildEngineLoop()
	{
		float baseFreq = 55f;
		float duration = 1f / baseFreq * 8f;
		int samples = (int)(MixRate * duration);
		var data = new byte[samples * 2];
		var rng = new Random(42);
		for (int i = 0; i < samples; i++)
		{
			float t = (float)i / MixRate;
			float v = 0f;
			v += Mathf.Sin(Mathf.Tau * baseFreq * t) * 0.5f;
			v += Mathf.Sin(Mathf.Tau * baseFreq * 2f * t) * 0.25f;
			v += Mathf.Sin(Mathf.Tau * baseFreq * 3f * t) * 0.15f;
			v += ((float)rng.NextDouble() * 2f - 1f) * 0.06f;
			WriteSample(data, i, v * 0.8f);
		}
		return CreateStream(data, true);
	}

	private static void WriteSample(byte[] data, int index, float value)
	{
		short s = (short)Mathf.Clamp(value * 32767f, short.MinValue, short.MaxValue);
		data[index * 2] = (byte)(s & 0xFF);
		data[index * 2 + 1] = (byte)((s >> 8) & 0xFF);
	}
}
