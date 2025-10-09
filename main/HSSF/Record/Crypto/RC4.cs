/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace NPOI.HSSF.Record.Crypto
{
    using System;
    using System.Text;
    using Cysharp.Text;
    using NPOI.Util;

    /**
     * Simple implementation of the alleged RC4 algorithm.
     *
     * Inspired by <A HREF="http://en.wikipedia.org/wiki/RC4">wikipedia's RC4 article</A>
     *
     * @author Josh Micich
     */
    public sealed class RC4
    {
        private int _i, _j;
        private readonly byte[] _state = new byte[256];

        public RC4()
        {
        }

        public RC4(byte[] key)
        {
            InitializeState(key);
        }

        /// <summary>
        /// Initialize the RC4 state with a given key
        /// </summary>
        /// <param name="key">The key bytes</param>
        public void InitializeState(byte[] key)
        {
            for(int i = 0; i < 256; i++)
            {
                _state[i] = (byte) i;
            }

            _i = 0;
            _j = 0;
            int num = 0;
            for(int j = 0; j < 256; j++)
            {
                num = (num + key[j % key.Length] + _state[j]) % 256;
                SwapBytes(_state, j, num);
            }

        }

        /// <summary>
        /// Advance the RC4 state by a specified number of steps without generating output
        /// </summary>
        /// <param name="steps">Number of steps to advance</param>
        public void AdvanceState(int steps = 1)
        {
            for(int step = 0; step < steps; step++)
            {
                _i = (_i + 1) % 256;
                _j = (_j + _state[_i]) % 256;
                SwapBytes(_state, _i, _j);
            }
        }

        /// <summary>
        /// Convert (encrypt/decrypt) data in place
        /// </summary>
        /// <param name="buffer">Buffer containing data to convert</param>
        /// <param name="offset">Starting offset in the buffer</param>
        /// <param name="count">Number of bytes to convert</param>
        public void ConvertData(byte[] buffer, int offset, int count)
        {
            int end = offset + count;
            for(int idx = offset; idx < end; idx++)
            {
                // Use AdvanceState and then generate keystream byte
                AdvanceState();
                buffer[idx] ^= _state[(_state[_i] + _state[_j]) % 256];
            }
        }

        /// <summary>
        /// Convert (encrypt/decrypt) entire data buffer in place
        /// </summary>
        /// <param name="buffer">Buffer to convert</param>
        public void ConvertData(byte[] buffer)
        {
            ConvertData(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Swap two bytes in the S-box
        /// </summary>
        /// <param name="array">The array</param>
        /// <param name="i">First index</param>
        /// <param name="j">Second index</param>
        private static void SwapBytes(byte[] array, int i, int j)
        {
            byte temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }

        public byte Output()
        {
            // Use AdvanceState to update the internal state
            AdvanceState();

            // Generate and return the keystream byte
            return _state[(_state[_i] + _state[_j]) % 256];
        }

        public void Encrypt(byte[] in1)
        {
            ConvertData(in1, 0, in1.Length);
        }
        public void Encrypt(byte[] in1, int OffSet, int len)
        {
            ConvertData(in1, OffSet, len);
        }
        public override String ToString()
        {
            using var sb = ZString.CreateStringBuilder();

            sb.Append(GetType().Name);
            sb.Append(" [");
            sb.Append("i=");
            sb.Append(_i);
            sb.Append(" j=");
            sb.Append(_j);
            sb.Append("]");
            sb.Append("\n");
            sb.Append(HexDump.Dump(_state, 0, 0));

            return sb.ToString();
        }
    }


}