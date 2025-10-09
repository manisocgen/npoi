/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You Under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed Under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations Under the License.
==================================================================== */

namespace NPOI.HSSF.Record.Crypto
{
    using System;
    using NPOI.HSSF.Record;
    using NPOI.POIFS.Crypt;
    using NPOI.Util;

    /// <summary>
    /// BIFF8 decrypting stream that uses EncryptionInfo / Decryptor abstraction.
    /// Reads record headers (sid, size) in plain form while advancing cipher state,
    /// then decrypts record payload unless record is in the never-encrypted list.
    /// </summary>
    public sealed class Biff8DecryptingStream : BiffHeaderInput, ILittleEndianInput
    {
        public const int RC4_REKEYING_INTERVAL = 1024; // BIFF8 block size

        private readonly ChunkedCipherInputStream ccis;
        private readonly byte[] buffer = new byte[LittleEndianConsts.LONG_SIZE];
        private bool shouldSkipEncryptionOnCurrentRecord; // true => do not decrypt payload

        public Biff8DecryptingStream(InputStream input, int initialOffset, EncryptionInfo info)
        {
            try
            {
                byte[] initialBuf = IOUtils.SafelyAllocate(initialOffset, CryptoFunctions.MAX_RECORD_LENGTH);
                InputStream stream = input;
                if (initialOffset == 0)
                {
                    stream = input;
                }
                else
                {
                    stream = new PushbackInputStream(input, initialOffset);
                    ((PushbackInputStream)stream).Unread(initialBuf);
                }

                var dec = info.Decryptor;
                dec.SetChunkSize(RC4_REKEYING_INTERVAL);
                ccis = (ChunkedCipherInputStream)dec.GetDataStream(stream, int.MaxValue, 0);

                if (initialOffset > 0)
                {
                    // Advance cipher state across initial bytes (as plain header bytes)
                    ccis.ReadFully(initialBuf);
                }
            }
            catch (Exception ex)
            {
                throw new RecordFormatException("Failed to initialise decrypting stream: " + ex.Message, ex);
            }
        }

        public int Available() => ccis.Available();

        /// <summary>Read SID (2 bytes) without decrypting, decide if payload must be decrypted.</summary>
        public int ReadRecordSID()
        {
            ReadPlain(buffer, 0, LittleEndianConsts.SHORT_SIZE);
            int sid = LittleEndian.GetUShort(buffer, 0);
            shouldSkipEncryptionOnCurrentRecord = IsNeverEncryptedRecord(sid);
            return sid;
        }

        /// <summary>Read record data size (2 bytes) without decrypting; set expected record size.</summary>
        public int ReadDataSize()
        {
            ReadPlain(buffer, 0, LittleEndianConsts.SHORT_SIZE);
            int size = LittleEndian.GetUShort(buffer, 0);
            ccis.SetNextRecordSize(size);
            return size;
        }

        public double ReadDouble()
        {
            long bits = ReadLong();
            double d = BitConverter.Int64BitsToDouble(bits);
            if (double.IsNaN(d)) throw new InvalidOperationException("Unexpected NaN");
            return d;
        }

        public void ReadFully(byte[] buf)
        {
            ReadFully(buf, 0, buf.Length);
        }

        public void ReadFully(byte[] buf, int off, int len)
        {
            if (shouldSkipEncryptionOnCurrentRecord)
            {
                ccis.ReadPlain(buf, off, buf.Length);
            }
            else
            {
                ccis.Read(buf, off, len);
            }
        }

        public byte ReadByte()
        {
            if (shouldSkipEncryptionOnCurrentRecord)
            {
                ReadPlain(buffer, 0, LittleEndianConsts.BYTE_SIZE);
                return buffer[0];
            }
            else
            {
                return (byte)ccis.ReadByte();
            }
        }

        int ILittleEndianInput.ReadByte() => ReadByte();

        public int ReadUByte() => ReadByte() & 0xFF;

        public short ReadShort()
        {
            if(shouldSkipEncryptionOnCurrentRecord)
            {
                ReadPlain(buffer, 0, LittleEndianConsts.SHORT_SIZE);
                return LittleEndian.GetShort(buffer);
            }
            else
            {
                return ccis.ReadShort();
            }
        }

        public int ReadUShort() => ReadShort() & 0xFFFF;

        public int ReadInt()
        {
            if (shouldSkipEncryptionOnCurrentRecord)
            {
                ReadPlain(buffer, 0, LittleEndianConsts.INT_SIZE);
                return LittleEndian.GetInt(buffer);
            }
            else
            {
                return ccis.ReadInt();
            }
        }

        public long ReadLong()
        {
            if (shouldSkipEncryptionOnCurrentRecord)
            {
                ReadPlain(buffer, 0, LittleEndianConsts.LONG_SIZE);
                return LittleEndian.GetLong(buffer);
            }
            else
            {
                return ccis.ReadLong();
            }
        }

        public void ReadPlain(byte[] b, int off, int len)
        {
            ccis.ReadPlain(b, off, len);
        }

        public bool IsCurrentRecordEncrypted() => !shouldSkipEncryptionOnCurrentRecord;

        public static bool IsNeverEncryptedRecord(int sid)
        {
            switch(sid)
            {
                case BOFRecord.sid:
                // sheet BOFs for sure
                // TODO - find out about chart BOFs

                case InterfaceHdrRecord.sid:
                // don't know why this record doesn't seem to get encrypted

                case FilePassRecord.sid:
                    // this only really counts when writing because FILEPASS is read early

                    // UsrExcl(0x0194)
                    // FileLock
                    // RRDInfo(0x0196)
                    // RRDHead(0x0138)

                    return true;

                default:
                    return false;
            }
        }
    }
}

