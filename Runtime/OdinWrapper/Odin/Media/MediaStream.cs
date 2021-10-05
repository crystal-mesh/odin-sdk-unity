﻿using OdinNative.Core;
using OdinNative.Core.Handles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OdinNative.Odin.Media
{
    /// <summary>
    /// Base audio only stream
    /// </summary>
    /// <remarks>Video is currently not supported</remarks>
    public abstract class MediaStream : IVideoStream, IAudioStream, IDisposable
    {
        /// <summary>
        /// Media id
        /// </summary>
        public int Id { get; internal set; }
        /// <summary>
        /// Audio config
        /// </summary>
        public OdinMediaConfig MediaConfig { get; private set; }
        /// <summary>
        /// Control of async read and write tasks
        /// </summary>
        public CancellationTokenSource CancellationSource { get; internal set; }
        /// <summary>
        /// Determine to send and/or read data
        /// </summary>
        /// <remarks> if true no function call to ODIN ffi at all</remarks>
        public bool IsMuted { get; set; }

        private StreamHandle Handle;

        public MediaStream(int id, OdinMediaConfig config)
            : this(id, config, OdinLibrary.Api.AudioStreamCreate(config))
        { }

        internal MediaStream(int id, OdinMediaConfig config, StreamHandle handle)
        {
            Id = id;
            MediaConfig = config;
            Handle = handle;
            CancellationSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Media id
        /// </summary>
        /// <returns>id value</returns>
        public int GetId() => Id;

        /// <summary>
        /// Set <see cref="IsMuted"/>
        /// </summary>
        /// <param name="mute">true to call ffi on read/write or false for NOP</param>
        public void SetMute(bool mute)
        {
            IsMuted = mute;
        }

        /// <summary>
        /// Toggle <see cref="IsMuted"/>
        /// </summary>
        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        /// <summary>
        /// Add this media to a room
        /// </summary>
        /// <param name="roomHandle"><see cref="Room.Room"/> handle</param>
        /// <returns>true on success or false</returns>
        internal bool AddMediaToRoom(RoomHandle roomHandle)
        {
            return OdinLibrary.Api.RoomAddMedia(roomHandle, Handle) == Utility.OK;
        }

        /// <summary>
        /// Send audio data
        /// </summary>
        /// <remarks>if <see cref="IsMuted"/> NOP</remarks>
        /// <param name="buffer">audio data</param>
        public virtual void AudioPushData(float[] buffer)
        {
            if (IsMuted) return;

            OdinLibrary.Api.AudioPushData(Handle, buffer, buffer.Length);
        }

        /// <summary>
        /// Send audio data
        /// </summary>
        /// <remarks>if <see cref="IsMuted"/> NOP</remarks>
        /// <param name="buffer">audio data</param>
        /// <param name="cancellationToken"></param>
        public virtual Task AudioPushDataTask(float[] buffer, CancellationToken cancellationToken)
        {
            if (IsMuted) return Task.CompletedTask;

            return Task.Factory.StartNew(() => {
                OdinLibrary.Api.AudioPushData(Handle, buffer, buffer.Length);
            }, cancellationToken);
        }

        /// <summary>
        /// Send audio data and use custom <see cref="CancellationSource"/>
        /// </summary>
        /// <remarks>if <see cref="IsMuted"/> NOP</remarks>
        /// <param name="buffer">audio data</param>
        public virtual async void AudioPushDataAsync(float[] buffer)
        {
            await AudioPushDataTask(buffer, CancellationSource.Token);
        }

        /// <summary>
        /// Read audio data
        /// </summary>
        /// <remarks>if <see cref="IsMuted"/> NOP</remarks>
        /// <param name="buffer">buffer to write into</param>
        /// <returns>count of written bytes</returns>
        public virtual uint AudioReadData(float[] buffer)
        {
            if (IsMuted) return 0;

            return OdinLibrary.Api.AudioReadData(Handle, buffer, buffer.Length);
        }

        /// <summary>
        /// Read audio data
        /// </summary>
        /// <remarks>if <see cref="IsMuted"/> NOP</remarks>
        /// <param name="buffer">buffer to write into</param>
        /// <param name="cancellationToken"></param>
        /// <returns>count of written bytes</returns>
        public virtual Task<uint> AudioReadDataTask(float[] buffer, CancellationToken cancellationToken)
        {
            if (IsMuted) return Task.FromResult<uint>(0);

            return Task.Factory.StartNew(() => {
                return OdinLibrary.Api.AudioReadData(Handle, buffer, buffer.Length);
            }, cancellationToken);
        }

        /// <summary>
        /// Read audio data and use custom <see cref="CancellationSource"/>
        /// </summary>
        /// <param name="buffer">buffer to write into</param>
        /// <returns>count of written bytes</returns>
        public virtual async Task<uint> AudioReadDataAsync(float[] buffer)
        {
            return await AudioReadDataTask(buffer, CancellationSource.Token);
        }

        public virtual uint AudioDataLength()
        {
            return OdinLibrary.Api.AudioDataLength(Handle);
        }

        /// <summary>
        /// Cancel the custom <see cref="CancellationSource"/>
        /// </summary>
        /// <returns>true if the token can and was canceled or false</returns>
        public bool Cancel()
        {
            if (CancellationSource.Token.CanBeCanceled)
            {
                CancellationSource.Cancel();
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CancellationSource.Dispose();
                    MediaConfig = null;
                }

                Handle.Close();
                Handle = null;
                disposedValue = true;
            }
        }

        ~MediaStream()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}