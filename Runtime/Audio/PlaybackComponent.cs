﻿using OdinNative.Core;
using OdinNative.Odin;
using OdinNative.Odin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Handles the Playback for received ODIN audio data.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlaybackComponent : MonoBehaviour
    {
        /// <summary>
        /// The Unity AudioSource component for playback
        /// </summary>
        /// <remarks>Unity controls the playback device: no ConfigurationChanged event</remarks>
        public AudioSource PlaybackSource;
        /// <summary>
        /// The Unity AudioSource mute property
        /// </summary>
        /// <remarks>Sets volume to 0 or restore original volume</remarks>
        public bool Mute { get { return PlaybackSource?.mute ?? true; } set { if (PlaybackSource == null) return; PlaybackSource.mute = value; } }
        /// <summary>
        /// The Odin PlaybackStream underlying media stream calls
        /// </summary>
        /// <remarks>on true ignores stream calls</remarks>
        public bool MuteStream { get { return OdinMedia?.IsMuted ?? true; } set { OdinMedia?.SetMute(value); } }
        internal PlaybackStream OdinMedia => OdinHandler.Instance.Client
            .Rooms[RoomName]?
            .RemotePeers[PeerId]?
            .Medias[MediaId] as PlaybackStream;

        private string _RoomName;
        /// <summary>
        /// Room name for this playback. Change this value to change the PlaybackStream by Rooms from the Client.
        /// </summary>
        /// <remarks>Invalid values will cause errors.</remarks>
        public string RoomName
        {
            get { return _RoomName; }
            set
            {
                _RoomName = value;
                PlaybackMedia = OdinMedia;
            }
        }
        private ulong _PeerId;
        /// <summary>
        /// Peer id for this playback. Change this value to change the PlaybackStream by RemotePeers in the Room.
        /// </summary>
        /// <remarks>Invalid values will cause errors.</remarks>
        public ulong PeerId
        {
            get { return _PeerId; }
            set
            {
                _PeerId = value;
                PlaybackMedia = OdinMedia;
            }
        }
        private ushort _MediaId;
        /// <summary>
        /// Media id for this playback. Change this value to pick a PlaybackStream by media id from peers Medias.
        /// </summary>
        /// <remarks>Invalid values will cause errors.</remarks>
        public ushort MediaId
        {
            get { return _MediaId; }
            set
            {
                _MediaId = value;
                PlaybackMedia = OdinMedia;
            }
        }
        private PlaybackStream PlaybackMedia;

        /// <summary>
        /// On true destroy the <see cref="PlaybackSource"/> in dispose to not leak 
        /// <see cref="UnityEngine.AudioSource"/> <see href="https://docs.unity3d.com/ScriptReference/AudioSource.html">(AudioSource)</see>
        /// or false for manually manage sources
        /// </summary>
        public bool AutoDestroyAudioSource = true;

        internal bool RedirectPlaybackAudio = true;
        //State of InvokeRepeating
        private bool _RedirectingPlaybackAudio = false;
        private const float RedirectPlaybackDelay = 0.5f;
        private const float RedirectPlaybackInterval = 0.02f;

        [Header("AudioClip Settings")]
        private int AudioClipIndex;

        private const int MinAudioPackageSize = 960;
        private const int CacheSize = 3840; // Playback request of > 2048 requested
        private const int BufferSize = CacheSize * 4;

        /// <summary>
        /// Use set <see cref="Channels"/> on true, <see cref="OdinEditorConfig.RemoteChannels"/> on false
        /// </summary>
        public bool OverrideChannels;
        /// <summary>
        /// The playback <see cref="OdinNative.Core.MediaChannels"/>
        /// </summary>
        /// <remarks>Set value is ignored on 
        /// <see cref="UnityEngine.AudioClip"/> <see href="https://docs.unity3d.com/ScriptReference/AudioClip.html">(AudioClip)</see>
        /// creation if <see cref="OverrideChannels"/> is false</remarks>
        public MediaChannels Channels;

        /// <summary>
        /// Use set <see cref="SampleRate"/> on true, <see cref="OdinEditorConfig.RemoteSampleRate"/> on false
        /// </summary>
        public bool OverrideSampleRate;
        /// <summary>
        /// The playback <see cref="OdinNative.Core.MediaSampleRate"/>
        /// </summary>
        /// <remarks>Set value is ignored on 
        /// <see cref="UnityEngine.AudioClip"/> <see href="https://docs.unity3d.com/ScriptReference/AudioClip.html">(AudioClip)</see>
        /// creation if <see cref="OverrideSampleRate"/> is false</remarks>
        public MediaSampleRate SampleRate;

        private bool _IsPlaying;
        /// <summary>
        /// True, if <see cref="OdinNative.Odin.Media.MediaStream.AudioDataLength"/> reports available data otherwise false
        /// </summary>
        /// <remarks>Sets _IsPlaying and invokes <see cref="OnPlaybackPlayingStatusChanged"/> only if the value is new</remarks>
        public bool PlayingStatus
        { 
            get
            {
                return _IsPlaying;
            }
            private set
            {
                if (_IsPlaying != value)
                {
                    _IsPlaying = value;
                    OnPlaybackPlayingStatusChanged?.Invoke(this, _IsPlaying);
                }
            }
        }
        public delegate void IsPlayingCallback(PlaybackComponent component, bool isPlaying);
        /// <summary>
        /// Triggered if <see cref="OdinNative.Odin.Media.MediaStream.AudioDataLength"/> of Odin
        /// <see cref="OdinNative.Odin.Media.PlaybackStream"/> has data to play or not.
        /// </summary>
        /// <remarks>Note: oneshot on status change and will not invoked multiple times for the same status!</remarks>
        public IsPlayingCallback OnPlaybackPlayingStatusChanged;
        /// <summary>
        /// Updates <see cref="PlayingStatus"/> and invokes <see cref="OnPlaybackPlayingStatusChanged"/>.
        /// Using <see cref="Update"/> on true.
        /// </summary>
        /// <remarks>If true uses <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.CancelInvoke.html">MonoBehaviour.CancelInvoke</see>
        /// to stop <see cref="UpdatePlayingStatus"/> even if <see cref="CheckPlayingStatusAsInvoke"/> is true</remarks>
        public bool CheckPlayingStatusInUpdate;
        /// <summary>
        /// On true updates <see cref="PlayingStatus"/> and invokes <see cref="OnPlaybackPlayingStatusChanged"/>
        /// the <see cref="UpdatePlayingStatus"/> in time <see cref="PlayingStatusDelay"/> seconds, then repeatedly every <see cref="PlayingStatusRepeatingTime"/> seconds.
        /// Using <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.InvokeRepeating.html">MonoBehaviour.InvokeRepeating</see>.
        /// </summary>
        /// <remarks>This does not work if the 
        /// <see cref="UnityEngine.Time.timeScale"/> <see href="https://docs.unity3d.com/ScriptReference/Time-timeScale.html">(AudioClip)</see>
        /// is set to 0 or <see cref="CheckPlayingStatusInUpdate"/> is true!</remarks>
        public bool CheckPlayingStatusAsInvoke;
        //State of InvokeRepeating
        private bool CheckPlayingStatusInvoke;

        /// <summary>
        /// Set the start of <see cref="UpdatePlayingStatus"/> in seconds.
        /// </summary>
        [Range(0f, 2.0f)]
        public float PlayingStatusDelay = 0f;
        /// <summary>
        /// Set the execution interval of <see cref="UpdatePlayingStatus"/> in seconds.
        /// </summary>
        [Range(0f, 2.0f)]
        public float PlayingStatusRepeatingTime = 0.2f;

        void Awake()
        {
            if (PlaybackSource == null)
                PlaybackSource = gameObject.GetComponents<AudioSource>()
                    .Where(s => s.clip == null)
                    .FirstOrDefault() ?? gameObject.AddComponent<AudioSource>();

            PlaybackSource.loop = true;

            CreateClip();
        }

        private void CreateClip()
        {
            if (OverrideChannels == false) Channels = OdinHandler.Config.RemoteChannels;
            if (OverrideSampleRate == false) SampleRate = OdinHandler.Config.RemoteSampleRate;

            if (Channels != MediaChannels.Mono) Debug.LogWarning("Odin-Server assert Mono-Channel missmatch, continue anyway...");
            if (SampleRate != MediaSampleRate.Hz48000) Debug.LogWarning("Odin-Server assert 48k-SampleRate missmatch, continue anyway...");
            PlaybackSource.clip = AudioClip.Create($"({PlaybackSource.gameObject.name}) {nameof(PlaybackComponent)}",
                BufferSize,
                (int)Channels,
                (int)SampleRate, false);
        }

        void OnEnable()
        {
            if (PlaybackSource.isPlaying == false)
                PlaybackSource.Play();

            RedirectPlaybackAudio = true;
        }

        void Reset()
        {
            OverrideChannels = false;
            Channels = OdinHandler.Config.RemoteChannels;

            OverrideSampleRate = false;
            SampleRate = OdinHandler.Config.RemoteSampleRate;

            CheckPlayingStatusInUpdate = false;
            CheckPlayingStatusAsInvoke = false;
            PlayingStatusDelay = 0f;
            PlayingStatusRepeatingTime = 0.2f;
        }

        void Start()
        {
            _IsPlaying = false;
            AudioClipIndex = 0;
        }

        void Update()
        {
            CheckRedirectAudio();

            if (CheckPlayingStatusInUpdate)
            {
                if (CheckPlayingStatusInvoke)
                    CancelInvoke("UpdatePlayingStatus");

                UpdatePlayingStatus();

                return;
            }
            else
            {
                CheckPlayingStatus();
            }
        }

        private void CheckRedirectAudio()
        {
            if (RedirectPlaybackAudio && _RedirectingPlaybackAudio == false)
            {
                InvokeRepeating("Flush", RedirectPlaybackDelay, RedirectPlaybackInterval);
                _RedirectingPlaybackAudio = true;
                if (PlaybackSource.isPlaying == false)
                    PlaybackSource.Play();
            }
            else if (RedirectPlaybackAudio == false && _RedirectingPlaybackAudio)
            {
                CancelInvoke("Flush");
                _RedirectingPlaybackAudio = false;
                PlaybackSource.Stop();
                PlaybackSource.clip.SetData(new float[BufferSize], 0);
                AudioClipIndex = 0;
            }
        }

        private void CheckPlayingStatus()
        {
            if (CheckPlayingStatusAsInvoke && CheckPlayingStatusInvoke == false)
            {
                InvokeRepeating("UpdatePlayingStatus", PlayingStatusDelay, PlayingStatusRepeatingTime);
                CheckPlayingStatusInvoke = true;
            }
            else if (CheckPlayingStatusAsInvoke == false && CheckPlayingStatusInvoke)
            {
                CancelInvoke("UpdatePlayingStatus");
                CheckPlayingStatusInvoke = false;
            }
        }

        // TODO: Frame independent
        private void Flush()
        {
            if (PlaybackMedia == null || PlaybackMedia.IsMuted || _RedirectingPlaybackAudio == false) return;

            int newIndex = PlaybackSource.timeSamples + CacheSize;
            int requested = newIndex - AudioClipIndex;
            if (requested < 0) requested += BufferSize;
            if (requested > 0)
            {
                //Debug.Log($"{PlaybackSource.timeSamples} {AudioClipIndex} {requested}");
                var n = Math.Min(MinAudioPackageSize, requested);
                float[] buffer = new float[n];
                PlaybackMedia.AudioReadData(buffer);
                PlaybackSource.clip.SetData(buffer, AudioClipIndex % BufferSize);
                if (n < requested)
                {
                    buffer = new float[requested - n];
                    PlaybackMedia.AudioReadData(buffer);
                    PlaybackSource.clip.SetData(buffer, (AudioClipIndex + n) % BufferSize);
                }
            }
            AudioClipIndex = newIndex;
        }

        /// <summary>
        /// Set <see cref="PlayingStatus"/> to true if audio data is available.
        /// </summary>
        /// <returns>true on data or false</returns>
        public bool UpdatePlayingStatus()
        {
            if (PlaybackMedia == null || PlaybackMedia.IsMuted || _RedirectingPlaybackAudio == false)
                return PlayingStatus = false;

            return PlayingStatus = PlaybackMedia.AudioDataLength() > 0;
        }

        void OnDisable()
        {
            CancelInvoke();
            PlaybackSource.Stop();
            AudioClipIndex = 0;
            RedirectPlaybackAudio = false;
        }

        private void OnDestroy()
        {
            if (AutoDestroyAudioSource)
                Destroy(PlaybackSource);

            OdinHandler.Instance.Client
            .Rooms[RoomName]?
            .RemotePeers[PeerId]?
            .Medias.Free(MediaId);
        }
    }
}
