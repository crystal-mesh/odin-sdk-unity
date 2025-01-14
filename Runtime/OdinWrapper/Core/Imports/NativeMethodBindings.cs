﻿using OdinNative.Core.Handles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Imports
{
    internal partial class NativeMethods
    {
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinStartupDelegate();
        readonly OdinStartupDelegate _OdinStartup;
        /// <summary>
        /// Starts native runtime threads.
        /// <list type="table">
        /// <listheader><term>OdinRoom</term><description>RoomHandle for medias and events</description></listheader>
        /// <item>Create <description><see cref="RoomCreate"/></description></item>
        /// <item>Destroy <description><see cref="RoomDestroy"/></description></item>
        /// <item></item>
        /// <listheader><term>OdinMediaStream</term><description>StreamHandle for audio and video</description></listheader>
        /// <item>Create <description><see cref="AudioStreamCreate"/></description></item>
        /// <item>Destroy <description><see cref="MediaStreamDestroy"/></description></item>
        /// </list>
        /// </summary>
        /// <remarks>Stop with <see cref="Shutdown"/></remarks>
        public void Startup()
        {
            using (Lock)
                _OdinStartup();
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinShutdownDelegate();
        readonly OdinShutdownDelegate _OdinShutdown;
        /// <summary>
        /// Stops native runtime threads that are started with <see cref="Startup"/>
        /// </summary>
        public void Shutdown()
        {
            using (Lock)
                _OdinShutdown();
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinGenerateAccessKeyDelegate([In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinGenerateAccessKeyDelegate _OdinAccessKeyGenerate;
        /// <summary>
        /// Provides a readable representation for a test key
        /// </summary>
        /// <param name="bufferSize">max string buffer size</param>
        /// <returns>Test Key</returns>
        internal string GenerateAccessKey(int bufferSize = 128)
        {
            using (Lock)
            {
                IntPtr _akeyPointer = Marshal.AllocHGlobal(bufferSize);
                int size = _OdinAccessKeyGenerate(_akeyPointer, bufferSize);
                return ConsumeKeyBuffer(_akeyPointer, size);
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinAccessKeyPublicKeyDelegate(string accessKey, [In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinAccessKeyPublicKeyDelegate _OdinAccessKeyPublicKey;
        internal string LoadPublicKey(string accessKey, int bufferSize = 128)
        {
            using (Lock)
            {
                IntPtr _pkeyPointer = Marshal.AllocHGlobal(bufferSize);
                int size = _OdinAccessKeyPublicKey(accessKey, _pkeyPointer, bufferSize);
                return ConsumeKeyBuffer(_pkeyPointer, size);
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinAccessKeySecretKeyDelegate(string accessKey, [In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinAccessKeySecretKeyDelegate _OdinAccessKeySecretKey;
        internal string LoadSecretKey(string accessKey, int bufferSize = 128)
        {
            using (Lock)
            {
                IntPtr _skeyPointer = Marshal.AllocHGlobal(bufferSize);
                int size = _OdinAccessKeySecretKey(accessKey, _skeyPointer, bufferSize);
                return ConsumeKeyBuffer(_skeyPointer, size);
            }
        }

        #region Token Generator
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinTokenGeneratorCreateDelegate(string accessKey);
        readonly OdinTokenGeneratorCreateDelegate _OdinTokenGeneratorCreate;
        /// <summary>
        /// Allocate TokenGenerator
        /// </summary>
        /// <param name="accessKey">*const c_char</param>
        /// <returns><see cref="TokenGeneratorHandle"/> always owns the <see cref="IntPtr"/> handle</returns>
        public TokenGeneratorHandle TokenGeneratorCreate(string accessKey)
        {
            using (Lock)
            {
                IntPtr handle = _OdinTokenGeneratorCreate(accessKey);
                return new TokenGeneratorHandle(handle, _OdinTokenGeneratorDestroy);
            }
        }
        
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinTokenGeneratorDestroyDelegate(IntPtr tokenGenerator);
        readonly OdinTokenGeneratorDestroyDelegate _OdinTokenGeneratorDestroy;
        /// <summary>
        /// Free the allocated TokenGenerator
        /// </summary>
        /// <param name="room">*mut OdinTokenGenerator</param>
        public void TokenGeneratorDestroy(TokenGeneratorHandle tokenGenerator)
        {
            using (Lock)
                _OdinTokenGeneratorDestroy(tokenGenerator);
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinTokenGeneratorCreateTokenDelegate(IntPtr tokenGenerator, string roomId, string userId, [In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinTokenGeneratorCreateTokenDelegate _OdinTokenGeneratorCreateToken;
        /// <summary>
        /// Creat room token
        /// </summary>
        /// <param name="tokenGenerator">allocated TokenGenerator</param>
        /// <param name="roomId">*const c_char</param>
        /// <param name="userId">*const c_char</param>
        /// <param name="buffer">*mut c_char</param>
        /// <param name="bufferLength">size *mut</param>
        /// <returns>Token or empty string</returns>
        public string TokenGeneratorCreateToken(TokenGeneratorHandle tokenGenerator, string roomId, string userId, int bufferLength = 512)
        {
            using (Lock)
            {
                IntPtr _tokenPointer = Marshal.AllocHGlobal(bufferLength);
                int size = _OdinTokenGeneratorCreateToken(tokenGenerator, roomId, userId, _tokenPointer, bufferLength);
                if (InternalIsError(size))
                {
                    Marshal.FreeHGlobal(_tokenPointer);
                    return string.Empty;
                }
                byte[] token = new byte[size];
                Marshal.Copy(_tokenPointer, token, 0, token.Length);
                Marshal.FreeHGlobal(_tokenPointer);
                return Encoding.UTF8.GetString(token);
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinTokenGeneratorCreateTokenExDelegate(IntPtr tokenGenerator, string roomId, string userId, OdinTokenOptions options, [In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinTokenGeneratorCreateTokenExDelegate _OdinTokenGeneratorCreateTokenEx;
        /// <summary>
        /// Creat room token with options
        /// </summary>
        /// <param name="tokenGenerator">allocated TokenGenerator</param>
        /// <param name="roomId">*const c_char</param>
        /// <param name="userId">*const c_char</param>
        /// <param name="options"></param>
        /// <param name="buffer">*mut c_char</param>
        /// <param name="bufferLength">size *mut</param>
        /// <returns>Token or empty string</returns>
        public string TokenGeneratorCreateTokenEx(TokenGeneratorHandle tokenGenerator, string roomId, string userId, OdinTokenOptions options, int bufferLength = 512)
        {
            using (Lock)
            {
                IntPtr _tokenExPointer = Marshal.AllocHGlobal(bufferLength);
                int size = _OdinTokenGeneratorCreateTokenEx(tokenGenerator, roomId, userId, options, _tokenExPointer, bufferLength);
                if (InternalIsError(size))
                {
                    Marshal.FreeHGlobal(_tokenExPointer);
                    return string.Empty;
                }
                byte[] token = new byte[size];
                Marshal.Copy(_tokenExPointer, token, 0, token.Length);
                Marshal.FreeHGlobal(_tokenExPointer);
                return Encoding.UTF8.GetString(token);
            }
        }
        #endregion

        #region Room
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinRoomConfigureApmDelegate(IntPtr room, NativeBindings.OdinApmConfig apmConfig);
        readonly OdinRoomConfigureApmDelegate _OdinRoomConfigureApm;
        /// <summary>
        /// Set OdinRoomConfig <see cref="NativeBindings.OdinApmConfig"/> in the <see cref="RoomCreate"/> provided room
        /// </summary>
        /// <remarks>currently only returns 0</remarks>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="config"><see cref="OdinNative.Core.OdinRoomConfig"/></param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public int RoomConfigure(RoomHandle room, OdinRoomConfig config)
        {
            using (Lock)
            {
                int error = _OdinRoomConfigureApm(room, config.GetOdinApmConfig());
                CheckAndThrow(error);
                return error;
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinRoomCreateDelegate();
        readonly OdinRoomCreateDelegate _OdinRoomCreate;
        /// <summary>
        /// Create room object representation
        /// </summary>
        /// <returns><see cref="RoomHandle"/> always owns the <see cref="IntPtr"/> handle</returns>
        public RoomHandle RoomCreate()
        {
            using (Lock)
            {
                IntPtr handle = _OdinRoomCreate();
                return new RoomHandle(handle, _OdinRoomDestroy);
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinRoomDestroyDelegate(IntPtr room);
        readonly OdinRoomDestroyDelegate _OdinRoomDestroy;
        /// <summary>
        /// Free the allocated room object
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        public void RoomDestroy(RoomHandle room)
        {
            using (Lock)
                _OdinRoomDestroy(room);
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinRoomJoinDelegate(IntPtr room, string gatewayUrl, string roomToken, byte[] userData, ulong userDataLength, [Out] out UInt64 ownPeerIdOut);
        readonly OdinRoomJoinDelegate _OdinRoomJoin;
        /// <summary>
        /// Connect and join room on the gateway returned provided server
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="gatewayUrl">*const c_char</param>
        /// <param name="roomToken">*const c_char</param>
        /// <param name="userData">*const u8</param>
        /// <param name="userDataLength">usize</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public int RoomJoin(RoomHandle room, string gatewayUrl, string roomToken, byte[] userData, int userDataLength, out ulong ownPeerId)
        {
            using (Lock)
            {
                int error = _OdinRoomJoin(room, gatewayUrl, roomToken, userData, (ulong)userDataLength, out ownPeerId);
                CheckAndThrow(error);
                return error;
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinRoomAddMediaDelegate(IntPtr room, IntPtr mediaStream);
        readonly OdinRoomAddMediaDelegate _OdinRoomAddMedia;
        /// <summary>
        /// Add a <see cref="OdinNative.Odin.Media.MediaStream"/> in the <see cref="RoomCreate"/> provided room.
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="mediaStream">*mut <see cref="OdinNative.Odin.Media.MediaStream"/></param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public int RoomAddMedia(RoomHandle room, StreamHandle stream)
        {
            using (Lock)
            {
                int error = _OdinRoomAddMedia(room, stream);
                CheckAndThrow(error);
                return error;
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinRoomUpdateUserDataDelegate(IntPtr room, byte[] userData, ulong userDataLength);
        readonly OdinRoomUpdateUserDataDelegate _OdinRoomUpdateUserData;
        /// <summary>
        /// Update own Userdata
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="userData">*const u8</param>
        /// <param name="userDataLength">usize</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public int RoomUpdateUserData(RoomHandle room, byte[] userData, ulong userDataLength)
        {
            using (Lock)
            {
                int error = _OdinRoomUpdateUserData(room, userData, userDataLength);
                CheckAndThrow(error);
                return error;
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinRoomSetEventCallbackDelegate(IntPtr room, OdinEventCallback callback);
        readonly OdinRoomSetEventCallbackDelegate _OdinRoomSetEventCallback;
        /// <summary>
        /// Register a <see cref="OdinEventCallback"/> for all room events in the <see cref="RoomCreate"/> provided room.
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="callback">extern "C" fn(event: *const <see cref="NativeBindings.AkiEvent"/>) -> ()</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public int RoomSetEventCallback(RoomHandle room, OdinEventCallback callback)
        {
            using (Lock)
            {
                int error = _OdinRoomSetEventCallback(room, callback);
                CheckAndThrow(error);
                return error;
            }
        }
        public delegate void OdinEventCallback(IntPtr room, IntPtr odinEvent, IntPtr userData);

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinRoomSendMessageDelegate(IntPtr room, [In] UInt64[] peerIdList, [In] ulong peerIdListSize, [In] byte[] data, [In] ulong dataLength);
        readonly OdinRoomSendMessageDelegate _OdinRoomSendMessage;
        /// <summary>
        /// Sends arbitrary data to a list of target peers over the ODIN server.
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="peerIdList">*const u64</param>
        /// <param name="peerIdListSize">usize</param>
        /// <param name="data">*const u8</param>
        /// <param name="dataLength">usize</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public int RoomSendMessage(RoomHandle room, ulong[] peerIdList, ulong peerIdListSize, byte[] data, ulong dataLength)
        {
            using (Lock)
            {
                int error = _OdinRoomSendMessage(room, peerIdList, peerIdListSize, data, dataLength);
                CheckAndThrow(error);
                return error;
            }
        }
        #endregion Room

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinAudioProcessReverseDelegate(IntPtr room, [In] float[] buffer, [In] int bufferLength, [In, Out][MarshalAs(UnmanagedType.I4)] OdinChannelLayout channelLayout);
        readonly OdinAudioProcessReverseDelegate _OdinAudioProcessReverse;
        /// <summary>
        /// Send audio data for the i.e Echo cancellor
        /// </summary>
        /// <remarks>OdinChannelLayout is currently unused!</remarks>
        /// <param name="room">struct OdinRoom*</param>
        /// <param name="buffer">float*</param>
        /// <param name="channelLayout">enum <see cref="OdinChannelLayout"/></param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        internal int AudioProcessReverse(RoomHandle room, float[] buffer, OdinChannelLayout channelLayout = OdinChannelLayout.OdinChannelLayout_Mono)
        {
            using (Lock)
            {
                int error = _OdinAudioProcessReverse(room, buffer, buffer.Length, channelLayout);
                if (InternalIsError(error))
                    CheckAndThrow(error);
                return error;
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinAudioMixStreamsDelegate(IntPtr room, [In] IntPtr[] mediaStreams, [In] int streamsLength, [In, Out] float[] buffer, [In, Out] int bufferLength, [In, Out][MarshalAs(UnmanagedType.I4)] OdinChannelLayout channelLayout);
        readonly OdinAudioMixStreamsDelegate _OdinAudioMixStreams;
        /// <summary>
        /// Send audio data with multiple MediaStreams to mix
        /// </summary>
        /// <remarks>OdinChannelLayout is currently unused!</remarks>
        /// <param name="room">struct OdinRoom*</param>
        /// <param name="handles">struct OdinMediaStream *const *</param>
        /// <param name="buffer">float *</param>
        /// <param name="channelLayout">enum <see cref="OdinChannelLayout"/></param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        internal int AudioMixStreams(RoomHandle room, StreamHandle[] handles, float[] buffer, OdinChannelLayout channelLayout = OdinChannelLayout.OdinChannelLayout_Mono)
        {
            using (Lock)
            {
                IntPtr[] streams = handles
                    .Select(h => h.DangerousGetHandle())
                    .Where(p => p != IntPtr.Zero)
                    .ToArray();

                int error = _OdinAudioMixStreams(room, streams, streams.Length, buffer, buffer.Length, channelLayout);

                if (InternalIsError(error))
                    CheckAndThrow(error);
                return error;
            }
        }

        #region Media
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinVideoStreamCreateDelegate();
        readonly OdinVideoStreamCreateDelegate _OdinVideoStreamCreate;
        /// <summary>
        /// NotSupported 
        /// </summary>
        /// <returns><see cref="OdinNative.Odin.Media.MediaStream"/> *</returns>
        internal IntPtr VideoStreamCreate()
        {
            using (Lock)
                return _OdinVideoStreamCreate();
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinAudioStreamCreateDelegate(NativeBindings.OdinAudioStreamConfig config);
        readonly OdinAudioStreamCreateDelegate _OdinAudioStreamCreate;
        /// <summary>
        /// Creates a native <see cref="StreamHandle"/>. Can only be destroyed with  
        /// <see cref="MediaStreamDestroy"/>
        /// </summary>
        /// <param name="config"><see cref="OdinMediaConfig"/></param>
        /// <returns><see cref="StreamHandle"/> * as <see cref="IntPtr"/> so <see cref="StreamHandle"/> can own the handle</returns>
        public StreamHandle AudioStreamCreate(OdinMediaConfig config)
        {
            using (Lock)
            {
                IntPtr handle = _OdinAudioStreamCreate(config.GetOdinAudioStreamConfig());
                return new StreamHandle(handle, _OdinMediaStreamDestroy);
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinMediaStreamDestroyDelegate(IntPtr mediaStream);
        readonly OdinMediaStreamDestroyDelegate _OdinMediaStreamDestroy;
        /// <summary>
        /// Destroy a native <see cref="StreamHandle"/> that is created before with <see cref="AudioStreamCreate"/>.
        /// </summary>
        /// <remarks> Should not be called on remote streams from <see cref="NativeBindings.AkiEvent"/>.</remarks>
        /// <param name="handle"><see cref="StreamHandle"/> *</param>
        public void MediaStreamDestroy(StreamHandle handle)
        {
            using (Lock)
                _OdinMediaStreamDestroy(handle);
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinMediaStreamTypeDelegate(ref IntPtr mediaStream);
        readonly OdinMediaStreamTypeDelegate _OdinMediaStreamType;
        // TODO: MediaStreamType

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinAudioPushDataDelegate(IntPtr mediaStream, [In] float[] buffer, [In] int bufferLength);
        readonly OdinAudioPushDataDelegate _OdinAudioPushData;
        /// <summary>
        /// Sends the buffer data to Odin.
        /// </summary>
        /// <param name="mediaStream">OdinMediaStream *</param>
        /// <param name="buffer">allocated buffer to read from</param>
        /// <param name="bufferLength">size of the buffer</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public int AudioPushData(StreamHandle mediaStream, float[] buffer, int bufferLength)
        {
            using (Lock)
            {
                int error = _OdinAudioPushData(mediaStream, buffer, bufferLength);
                if (InternalIsError(error))
                    CheckAndThrow(error);
                return error;
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinAudioDataLenDelegate(IntPtr mediaStream);
        readonly OdinAudioDataLenDelegate _OdinAudioDataLen;
        /// <summary>
        /// Get available audio data size.
        /// </summary>
        /// <param name="mediaStream">OdinMediaStream *</param>
        /// <returns>floats available to read with <see cref="AudioReadData"/></returns>
        public int AudioDataLength(StreamHandle mediaStream)
        {
            using (Lock)
                return _OdinAudioDataLen(mediaStream);
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinAudioReadDataDelegate(IntPtr mediaStream, [In, Out][MarshalAs(UnmanagedType.LPArray)] float[] buffer, [In] int bufferLength, [In, Out][MarshalAs(UnmanagedType.I4)] OdinChannelLayout channelLayout);
        readonly OdinAudioReadDataDelegate _OdinAudioReadData;
        /// <summary>
        /// Reads data into the buffer.
        /// </summary>
        /// <remarks>writes only audio data into the buffer even if the buffer size exceeded the available data</remarks>
        /// <param name="mediaStream">OdinMediaStream *</param>
        /// <param name="buffer">allocated buffer to write to</param>
        /// <param name="bufferLength">size of the buffer</param>
        /// <returns>count of written data</returns>
        public int AudioReadData(StreamHandle mediaStream, [In, Out] float[] buffer, int bufferLength)
        {
            using (Lock)
                return _OdinAudioReadData(mediaStream, buffer, bufferLength, OdinChannelLayout.OdinChannelLayout_Mono);
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinMediaStreamMediaIdDelegate(IntPtr mediaStream, [Out] out ushort mediaId);
        readonly OdinMediaStreamMediaIdDelegate _OdinMediaStreamMediaId;
        /// <summary>
        /// Returns the media ID of the specified <see cref="StreamHandle"/>
        /// </summary>
        /// <param name="handle"><see cref="StreamHandle"/> *mut</param>
        /// <param name="mediaId">media id of the handle</param>
        /// <returns>error code that is readable with <see cref="ErrorFormat"/></returns>
        public int MediaStreamMediaId(StreamHandle handle, out UInt16 mediaId)
        {
            using (Lock)
            {
                int error = _OdinMediaStreamMediaId(handle, out mediaId);
                CheckAndThrow(error);
                return error;
            }
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate int OdinMediaStreamPeerIdDelegate(IntPtr mediaStream, [Out] out ulong peerId);
        readonly OdinMediaStreamPeerIdDelegate _OdinMediaStreamPeerId;
        /// <summary>
        /// Returns the peer ID of the specified <see cref="StreamHandle"/>
        /// </summary>
        /// <param name="handle"><see cref="StreamHandle"/> *mut</param>
        /// <param name="peerId">peer id of the handle</param>
        /// <returns>error code that is readable with <see cref="ErrorFormat"/></returns>
        public int MediaStreamPeerId(StreamHandle handle, out UInt64 peerId)
        {
            using (Lock)
            {
                int error = _OdinMediaStreamPeerId(handle, out peerId);
                CheckAndThrow(error);
                return error;
            }
        }
        #endregion Media

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinErrorFormatDelegate(int error, [In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinErrorFormatDelegate _OdinErrorFormat;
        /// <summary>
        /// Writes a readable string representation of the error in a buffer.
        /// </summary>
        /// <param name="error">error code</param>
        /// <param name="buffer">String buffer pointer (e.g read with <see cref="Marshal.PtrToStringAnsi"/>)</param>
        /// <param name="bufferLength">String buffer length</param>
        /// <returns>0 or error code that is readable with <see cref="GetErrorMessage"/></returns>
        internal uint ErrorFormat(int error, IntPtr buffer, int bufferLength)
        {
            using (Lock)
                return _OdinErrorFormat(error, buffer, bufferLength);
        }

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate bool OdinIsErrorDelegate(int error);
        readonly OdinIsErrorDelegate _OdinIsError;
        /// <summary>
        /// Check if the error code is in range of errors.
        /// </summary>
        /// <remarks>Code <see cref="Utility.OK"/> is never a error and will not be checked</remarks>
        /// <param name="error">error code</param>
        /// <returns>true if error</returns>
        internal bool IsError(int error)
        {
            if (error == Utility.OK) return false;

            using (Lock)
                return _OdinIsError(error);
        }
    }
}
