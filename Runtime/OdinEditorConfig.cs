using System;
using System.Linq;
using OdinNative.Core;
using OdinNative.Odin;
using UnityEngine;
using Random = System.Random;

namespace OdinNative.Unity
{
    /// <summary>
    /// UnityEditor UI component for instance config of <see cref="OdinNative.Odin.OdinDefaults"/>
    /// </summary>
    [DisallowMultipleComponent, ExecuteInEditMode]
    public class OdinEditorConfig : MonoBehaviour
    {
        /// <summary>
        /// Enable additional Logs
        /// </summary>
        public bool Verbose = OdinDefaults.Verbose;

        /// <summary>
        /// Odin Client ApiKey
        /// </summary>
        public string AccessKey;
        /// <summary>
        /// Odin Client ID
        /// </summary>
        /// <remarks>Room token userId</remarks>
        public string ClientId;
        /// <summary>
        /// Gateway
        /// </summary>
        public string Server = OdinDefaults.Server;
        /// <summary>
        /// Default UserData content
        /// </summary>
        public string UserDataText = OdinDefaults.UserDataText;

        /// <summary>
        /// Microphone Sample-Rate
        /// </summary>
        public MediaSampleRate DeviceSampleRate = OdinDefaults.DeviceSampleRate;
        /// <summary>
        /// Microphone Channels
        /// </summary>
        public MediaChannels DeviceChannels = OdinDefaults.DeviceChannels;

        /// <summary>
        /// Playback Sample-Rate
        /// </summary>
        public MediaSampleRate RemoteSampleRate = OdinDefaults.RemoteSampleRate;
        /// <summary>
        /// Playback Channels
        /// </summary>
        public MediaChannels RemoteChannels = OdinDefaults.RemoteChannels;

        #region Events
        public bool PeerJoinedEvent = OdinDefaults.PeerJoinedEvent;
        public bool PeerLeftEvent = OdinDefaults.PeerLeftEvent;
        public bool PeerUpdatedEvent = OdinDefaults.PeerUpdatedEvent;
        public bool MediaAddedEvent = OdinDefaults.MediaAddedEvent;
        public bool MediaRemovedEvent = OdinDefaults.MediaRemovedEvent;
        #endregion Events

        /// <summary>
        /// Time untill the token expires
        /// </summary>
        public ulong TokenLifetime = OdinDefaults.TokenLifetime;

        #region Apm
        /// <summary>
        /// Turns VAC on and off
        /// </summary>
        public bool VadEnable = OdinDefaults.VadEnable;
        /// <summary>
        /// Turns Echo cancellation on and off
        /// </summary>
        public bool EchoCanceller = OdinDefaults.EchoCanceller;
        /// <summary>
        /// Reduces lower frequencies of the input (Automatic game control)
        /// </summary>
        public bool HighPassFilter = OdinDefaults.HighPassFilter;
        /// <summary>
        /// Amplifies the audio input
        /// </summary>
        public bool PreAmplifier = OdinDefaults.PreAmplifier;
        /// <summary>
        /// Turns noise suppression on and off
        /// </summary>
        public Core.Imports.NativeBindings.OdinNoiseSuppressionLevel NoiseSuppressionLevel = OdinDefaults.NoiseSuppressionLevel;
        /// <summary>
        /// Filters high amplitude noices
        /// </summary>
        public bool TransientSuppressor = OdinDefaults.TransientSuppressor;
        #endregion Apm

        internal string Identifier => SystemInfo.deviceUniqueIdentifier;

        void Awake()
        {
            if (string.IsNullOrEmpty(ClientId))
                ClientId = string.Join(".", Application.companyName, Application.productName);
        }

        public void GenerateUIAccessKey()
        {
            AccessKey = GenerateAccessKey();
        }

        /// <summary>
        /// Generates a new ODIN access key.
        /// </summary>
        /// <returns>ODIN access key</returns>
        private static string GenerateAccessKey()
        {
            var rand = new Random();
            byte[] key = new byte[33];
            rand.NextBytes(key);
            key[0] = 0x01;
            byte[] subArray = new ArraySegment<byte>(key, 1, 31).ToArray();
            key[32] = Crc8(subArray);
            return Convert.ToBase64String(key);
        }

        private static byte Crc8(byte[] data)
        {
            byte crc = 0xff;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x80) != 0) crc = (byte)((crc << 1) ^ 0x31);
                    else crc <<= 1;
                }
                crc = (byte)(0xff & crc);
            }
            return crc;
        }

    }


}
