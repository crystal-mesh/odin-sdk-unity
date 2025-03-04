﻿using OdinNative.Core;
using OdinNative.Odin.Room;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Odin
{
    /// <summary>
    /// Client Wrapper for ODIN ffi <see cref="OdinNative.Core.OdinLibrary.NativeMethods"/>
    /// </summary>
    public class OdinClient : IDisposable
    {
        /// <summary>
        /// Client Connection to manage Startup/Shutdown
        /// </summary>
        public static Core.Imports.NativeBindings.OdinConnectionState ConnectionState { get; private set; } = Core.Imports.NativeBindings.OdinConnectionState.Disconnected;
        /// <summary>
        /// Rooms
        /// </summary>
        internal volatile static RoomCollection _Rooms = new RoomCollection();
        /// <summary>
        /// A collection of all <see cref="Room.Room"/>
        /// </summary>
        public RoomCollection Rooms { get { return _Rooms; } }
        /// <summary>
        /// Connection EndPoint. Default from OdinEditorConfig.
        /// </summary>
        public Uri EndPoint { get; private set; }
        /// <summary>
        /// Client AccessKey for all new rooms. Default from OdinHandler config.
        /// </summary>
        public string AccessKey { get; private set; }
        /// <summary>
        /// Client custom UserData
        /// </summary>
        public UserData UserData { get; set; }
        /// <summary>
        /// True on succesful Startup or false
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Creates a new instance for ODIN ffi C# Wrapper
        /// </summary>
        /// <remarks><see cref="OdinNative.Odin.UserData"/> will be empty</remarks>
        /// <param name="url"><see cref="EndPoint"/> Odin Server</param>
        /// <param name="accessKey">Odin access key</param>
        public OdinClient(string url, string accessKey)
            : this(new Uri(url), accessKey, new UserData())
        { }


        /// <summary>
        /// Creates a new instance for ODIN ffi C# Wrapper
        /// </summary>
        /// <remarks><see cref="OdinNative.Odin.UserData"/> will be empty</remarks>
        /// <param name="server"><see cref="EndPoint"/> Odin Server</param>
        /// <param name="accessKey">Odin access key</param>
        public OdinClient(Uri server, string accessKey)
            : this(server, accessKey, new UserData())
        { }

        /// <summary>
        /// Creates a new instance for ODIN ffi C# Wrapper
        /// </summary>
        /// <remarks><see cref="OdinNative.Odin.UserData"/> is optional</remarks>
        /// <param name="server"><see cref="EndPoint"/> Odin Server</param>
        /// <param name="accessKey">Odin access key</param>
        /// <param name="userData"><see cref="UserData"/> to set</param>
        public OdinClient(Uri server, string accessKey, UserData userData = null)
        {
            EndPoint = server;
            UserData = userData;
            AccessKey = accessKey;
        }

        protected internal void ReloadLibrary(bool init = true)
        {
            if (OdinLibrary.IsInitialized)
            {
                if (this.IsInitialized)
                {
                    Close();
                    Shutdown();
                }
                OdinLibrary.Release();
            }
            if(init) OdinLibrary.Initialize();
        }

        /// <summary>
        /// Start internal ffi worker threads
        /// </summary>
        internal void Startup()
        {
            OdinLibrary.Api.Startup();
            IsInitialized = true;
        }

        internal static string CreateAccessKey()
        {
            return OdinLibrary.Api.GenerateAccessKey();
        }

        #region Gateway
        /// <summary>
        /// Join or create a <see cref="Room.Room"/> by name via a gateway
        /// </summary>
        /// <remarks>Initialize ODIN if <see cref="IsInitialized"/> is false</remarks>
        /// <param name="name">Room name</param>
        /// <param name="customerId">Customer ID</param>
        /// <returns><see cref="Room.Room"/> or null</returns>
        public async Task<Room.Room> JoinRoom(string name, string userId)
        {
            return await JoinRoom(name, userId, UserData, null);
        }

        /// <summary>
        /// Join or create a <see cref="Room.Room"/> by name via a gateway
        /// </summary>
        /// <remarks>Initialize ODIN if <see cref="IsInitialized"/> is false</remarks>
        /// <param name="name">Room name</param>
        /// <param name="userId">Odin client ID</param>
        /// <param name="setup">will invoke to setup a room before adding or joining</param>
        /// <returns><see cref="Room.Room"/> or null</returns>
        public async Task<Room.Room> JoinRoom(string name, string userId, Action<Room.Room> setup = null)
        {
            return await JoinRoom(name, userId, UserData, setup);
        }

        /// <summary>
        /// Join or create a <see cref="Room.Room"/> by name via a gateway
        /// </summary>
        /// <remarks>Initialize ODIN if <see cref="IsInitialized"/> is false</remarks>
        /// <param name="name">Room name</param>
        /// <param name="userId">Odin client ID</param>
        /// <param name="userData">Set new <see cref="UserData"/> on room join</param>
        /// <param name="setup">will invoke to setup a room before adding or joining</param>
        /// <returns><see cref="Room.Room"/> or null</returns>
        public async Task<Room.Room> JoinRoom(string name, string userId, UserData userData, Action<Room.Room> setup)
        {
            if (string.IsNullOrEmpty(name)) throw new OdinWrapperException("Room name can not be null or empty!", new ArgumentNullException());
            if (IsInitialized == false) Startup();

            UserData = userData.IsEmpty() ? UserData : userData;
            return await Task.Factory.StartNew<Room.Room>(() =>
            {
                var room = new Room.Room(EndPoint.ToString(), AccessKey, name);
                setup?.Invoke(room);
                Rooms.Add(room);
                if (room.Join(name, userId, UserData) == false)
                {
                    Rooms.Remove(room);
                    room.Dispose();
                    room = null;
                }
                return room;
            });
        }

        /// <summary>
        /// Join or create a <see cref="Room.Room"/> by token via a gateway
        /// </summary>
        /// <remarks>Initialize ODIN if <see cref="IsInitialized"/> is false and uses the token as name</remarks>
        /// <param name="token">Room token</param>
        /// <param name="userData">Set new <see cref="UserData"/> on room join</param>
        /// <param name="setup">will invoke to setup a room before adding or joining</param>
        /// <returns><see cref="Room.Room"/> or null</returns>
        public async Task<Room.Room> JoinRoom(string token, UserData userData, Action<Room.Room> setup)
        {
            if (string.IsNullOrEmpty(token)) throw new OdinWrapperException("Room token can not be null or empty!", new ArgumentNullException());
            if (IsInitialized == false) Startup();

            UserData = userData.IsEmpty() ? UserData : userData;
            return await Task.Factory.StartNew<Room.Room>(() =>
            {
                var room = new Room.Room(EndPoint.ToString(), AccessKey, token);
                setup?.Invoke(room);
                Rooms.Add(room);
                if (room.Join(token, UserData) == false)
                {
                    Rooms.Remove(room);
                    room.Dispose();
                    room = null;
                }
                return room;
            });
        }
        #endregion Gateway

        /// <summary>
        /// Updates the <see cref="UserData"/> for all <see cref="Rooms"/>
        /// </summary>
        /// <param name="userData"><see cref="OdinNative.Odin.UserData"/></param>
        public async void UpdateUserData(UserData userData)
        {
            if (userData == null) throw new OdinWrapperException("UserData can not be null!", new ArgumentNullException());

            UserData = userData;
            await Task.Factory.StartNew(() =>
            {
                foreach (var room in Rooms)
                    room.UpdateUserData(userData);
            });
        }

        /// <summary>
        /// Leave a joined Room
        /// </summary>
        /// <remarks>Will dispose the <see cref="Room.Room"/> object</remarks>
        /// <param name="name">Room name</param>
        /// <returns>true if removed from <see cref="Rooms"/> or false</returns>
        public async Task<bool> LeaveRoom(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return await Task.Factory.StartNew<bool>(() =>
            {
                // remove and dispose
                return Rooms.Free(name);
            });
        }


        /// <summary>
        /// Main entry for native OdinEvents sanitize and passthrough to instance room.
        /// </summary>
        /// <remarks>Events: PeerJoined, PeerLeft, PeerUpdated, MediaAdded, MediaRemoved</remarks>
        /// <param name="roomPtr">sender room pointer</param>
        /// <param name="event">OdinEvent struct</param>
        /// <param name="userDataPtr">userdata pointer</param>
        [AOT.MonoPInvokeCallback(typeof(Core.Imports.NativeMethods.OdinEventCallback))]
        internal static void OnEventReceivedProxy(IntPtr roomPtr, IntPtr odinEvent, IntPtr extraData)
        {
            try
            {
                var @event = Marshal.PtrToStructure<Core.Imports.NativeBindings.OdinEvent>(odinEvent);

                if (@event.tag == Core.Imports.NativeBindings.OdinEventTag.OdinEvent_ConnectionStateChanged)
                {
                    ConnectionState = @event.StateChanged.state;
                    return;
                }

                var sender = OdinClient._Rooms[roomPtr];
                if (sender != null)
                {
                    //TODO get event userDataPtr and sanitize e.g 3D-Audio
                    sender.OnEventReceived(sender, @event, extraData);
                }
            }
            catch(Exception e)
#pragma warning disable CS0618 // Type or member is obsolete
            { Utility.Throw(e); }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Completly closes this Client and all <see cref="Room.Room"/> associated.
        /// </summary>
        /// <remarks>Should only be called in Loading-Screens or Scene transissions</remarks>
        public void Close()
        {
            if (Rooms == null) return;

            try { Rooms.FreeAll(); }
            catch { /* nop */ }
        }

        /// <summary>
        /// Stop ffi worker threads
        /// </summary>
        internal void Shutdown()
        {
            try { OdinLibrary.Api.Shutdown(); }
            catch { /* nop */ }
            IsInitialized = false;
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                if (IsInitialized)
                    Shutdown();

                disposedValue = true;
            }
        }

        ~OdinClient()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// On dispose will free all <see cref="Room"/> and <see cref="Shutdown"/>
        /// </summary>
        /// <remarks>Override dispose if muliple <see cref="OdinClient"/> are needed</remarks>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
