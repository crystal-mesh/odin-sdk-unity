﻿using OdinNative.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OdinNative.Odin.Media
{
    /// <summary>
    /// Representation of a write only audio stream
    /// </summary>
    public class MicrophoneStream : MediaStream
    {
        internal MicrophoneStream()
            : base(0, new OdinMediaConfig(OdinDefaults.RemoteSampleRate, OdinDefaults.RemoteChannels))  // Defaults match server config
        { }

        internal MicrophoneStream(OdinMediaConfig config)
            : base(0, config)
        { }

        public override uint AudioReadData(float[] buffer)
        {
            throw new OdinWrapperException("Microphone streams are always writeonly!",
                new NotSupportedException("AudioReadData, AudioReadDataTask and AudioReadDataAsync are not supported!"));
        }

        public override Task<uint> AudioReadDataTask(float[] buffer, CancellationToken cancellationToken)
        {
            throw new OdinWrapperException("Microphone streams are always writeonly!",
                new NotSupportedException("AudioReadData, AudioReadDataTask and AudioReadDataAsync are not supported!"));
        }

        public override Task<uint> AudioReadDataAsync(float[] buffer)
        {
            throw new OdinWrapperException("Microphone streams are always writeonly!",
                new NotSupportedException("AudioReadData, AudioReadDataTask and AudioReadDataAsync are not supported!"));
        }

        public override uint AudioDataLength()
        {
            return 0;
        }
    }
}
