﻿#if UNITY_EDITOR
using OdinNative.Unity;
using UnityEngine;
using UnityEditor;

namespace OdinNative.Unity.UIEditor
{
    /// <summary>
    /// Adds a custom layout to the OdinEditorConfig component
    /// </summary>
    [CustomEditor(typeof(OdinEditorConfig))]
    public class OdinEditorConfigEditor : Editor
    {
        SerializedProperty Verbose;
        SerializedProperty AccessKey;
        SerializedProperty ClientId;
        SerializedProperty Server;
        SerializedProperty UserDataText;

        SerializedProperty DeviceSampleRate;
        SerializedProperty DeviceChannels;
        SerializedProperty RemoteSampleRate;
        SerializedProperty RemoteChannels;

        SerializedProperty VadEnable;
        SerializedProperty EchoCanceller;
        SerializedProperty HighPassFilter;
        SerializedProperty PreAmplifier;
        SerializedProperty NoiseSuppressionLevel;
        SerializedProperty TransientSuppressor;

        private bool toggleAuthSettings;
        private bool toggleAudioSettings;
        private bool toggleRoomSettings;

        private GUIStyle FoldoutLabelStyle;

        void OnEnable()
        {
            Verbose = serializedObject.FindProperty("Verbose");

            AccessKey = serializedObject.FindProperty("AccessKey");
            ClientId = serializedObject.FindProperty("ClientId");
            Server = serializedObject.FindProperty("Server");
            UserDataText = serializedObject.FindProperty("UserDataText");

            DeviceSampleRate = serializedObject.FindProperty("DeviceSampleRate");
            DeviceChannels = serializedObject.FindProperty("DeviceChannels");
            RemoteSampleRate = serializedObject.FindProperty("RemoteSampleRate");
            RemoteChannels = serializedObject.FindProperty("RemoteChannels");

            VadEnable = serializedObject.FindProperty("VadEnable");
            EchoCanceller = serializedObject.FindProperty("EchoCanceller");
            HighPassFilter = serializedObject.FindProperty("HighPassFilter");
            PreAmplifier = serializedObject.FindProperty("PreAmplifier");
            NoiseSuppressionLevel = serializedObject.FindProperty("NoiseSuppressionLevel");
            TransientSuppressor = serializedObject.FindProperty("TransientSuppressor");
        }

        /// <summary>
        /// Implementation for the Unity custom inspector of OdinEditorConfig.
        /// </summary>
        public override void OnInspectorGUI()
        {
            changeStyles();
            OdinEditorConfig odinEditorConfig = (target as OdinEditorConfig);
            if (odinEditorConfig == null)
            {
                DrawDefaultInspector(); // fallback
                return;
            }

            EditorGUILayout.PropertyField(Verbose, new GUIContent("Verbose Mode", "Enable additional logs"));
            GUILayout.Space(10);
            CreateClientSettingsLayout(odinEditorConfig);
            GUILayout.Space(10);
            CreateAudioSettingsLayout();
            GUILayout.Space(10);
            CreateRoomSettingsLayout();

            serializedObject.ApplyModifiedProperties();
        }

        private void changeStyles()
        {
            FoldoutLabelStyle = new GUIStyle(EditorStyles.foldout);
            FoldoutLabelStyle.fontStyle = FontStyle.Bold;
            FoldoutLabelStyle.fontSize = 14;
        }

        private static void drawRect(int height)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            GUILayout.Space(3);
        }

        #region ClientSettings
        private void CreateClientSettingsLayout(OdinEditorConfig odinEditorConfig)
        {
            toggleAuthSettings = EditorGUILayout.Foldout(toggleAuthSettings, "Client Authentication", FoldoutLabelStyle);
            drawRect(2);
            if (toggleAuthSettings)
            {
                EditorGUILayout.PropertyField(AccessKey, new GUIContent("Access Key", "Used to create room tokens for accessing the ODIN network.\n\nNote that all of your clients must use tokens generated from either the same access key or another key from the same project. While you can create an infinite number of access keys for your projects, we strongly recommend that you never put an access key in your client code."));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Manage Access"))
                {
                    OdinKeysWindow.ShowWindow();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                EditorGUILayout.PropertyField(ClientId, new GUIContent("Client ID", "A unique client identifier to be sent with the authentication request. This value cannot be changed while the ODIN client is connected."));
                EditorGUILayout.PropertyField(UserDataText, new GUIContent("Peer User Data", "Arbitrary data to be attached to the own peer when the ODIN client is joining the room. The data is visible to all other clients and can also be changed afterwards using OdinClient.UpdateUserData()."));
            }

        }
        #endregion

        #region AudioSettings
        private void CreateAudioSettingsLayout()
        {
            toggleAudioSettings = EditorGUILayout.Foldout(toggleAudioSettings, "Capture & Playback", FoldoutLabelStyle);
            drawRect(2);
            if (toggleAudioSettings)
            {
                EditorGUILayout.PropertyField(DeviceSampleRate, new GUIContent("Capture Sample Rate", "Sets the sample rate of the capture device."));
                EditorGUILayout.PropertyField(DeviceChannels, new GUIContent("Capture Channels", "Sets the channels of the capture device."));

                GUILayout.Space(10);

                EditorGUILayout.PropertyField(RemoteSampleRate, new GUIContent("Playback Sample Rate", "Sets the sample rate of the playback device."));
                EditorGUILayout.PropertyField(RemoteChannels, new GUIContent("Playback Channels", "Sets the channels of the playback device."));
            }
        }
        #endregion

        #region RoomSettings
        private void CreateRoomSettingsLayout()
        {
            toggleRoomSettings = EditorGUILayout.Foldout(toggleRoomSettings, "Room Audio Processing", FoldoutLabelStyle);
            drawRect(2);
            if (toggleRoomSettings)
            {
                EditorGUILayout.PropertyField(VadEnable, new GUIContent("Voice activity detection", "Use intelligent algorithms to determine speech presence probability in the microphone input signal."));
                //EditorGUILayout.PropertyField(EchoCanceller, new GUIContent("Echo cancellation", "Improve voice quality by preventing echo from being created.")); // TODO
                EditorGUILayout.PropertyField(HighPassFilter, new GUIContent("High-pass filter", "Reduce lower frequencies of the microphone input signal."));
                EditorGUILayout.PropertyField(PreAmplifier, new GUIContent("Input Amplifier", "Amplify the microphone input signal when needed."));
                EditorGUILayout.PropertyField(NoiseSuppressionLevel, new GUIContent("Noise Suppression", "Remove background noise from the microphone input signal."));
                EditorGUILayout.PropertyField(TransientSuppressor, new GUIContent("Transient Suppression", "Detect and reduce high amplitude noises such as typing sounds."));
            }
        }
        #endregion
    }
}
#endif