﻿using OdinNative.Core.Imports;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core.Handles
{
    class RoomHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(RoomHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        internal NativeMethods.OdinRoomDestroyDelegate Free;
        private bool OwnsHandle;

        /// <summary>
        /// Create room handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeMethods.OdinRoomDestroyDelegate"/></remarks>
        /// <param name="handle">Room handle pointer from <see cref="NativeMethods.OdinRoomCreateDelegate"/></param>
        /// <param name="roomDestroyDelegate">Will be called on <see cref="ReleaseHandle"/></param>
        /// <param name="ownsHandle">true to reliably release the handle during the finalization phase; false to prevent reliable release (not recommended).</param>
        internal RoomHandle(IntPtr handle, NativeMethods.OdinRoomDestroyDelegate roomDestroyDelegate, bool ownsHandle = true)
            : base(ownsHandle)
        {
            Free = roomDestroyDelegate;
            SetHandle(handle);
            OwnsHandle = ownsHandle;
        }

        /// <summary>
        /// Create room handle that is not owned and will not be freed
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle is set to invalid (<see cref="System.Runtime.InteropServices.SafeHandle.SetHandleAsInvalid"/>)</remarks>
        /// <param name="handle">remote handle</param>
        internal RoomHandle(IntPtr handle)
            : base(false)
        {
            OwnsHandle = false;
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            bool result = true;
            try
            {
                if (OwnsHandle)
                    Free(handle);
                else
                    SetHandleAsInvalid();
            }
            catch
            {
                result = false;
            }

            return result;
        }
    }
}
