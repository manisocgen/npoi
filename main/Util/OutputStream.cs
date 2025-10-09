using System;
using System.IO;

namespace NPOI.Util
{
    /// <summary>
    /// Java-like OutputStream base. Provides convenience Write(byte[]) and delegates Write(byte[],off,len)
    /// calls to abstract single-byte Write(int). Read/Seek operations are not supported.
    /// </summary>
    public abstract class OutputStream : Stream
    {
        // ---- Java-style API ----
        public abstract void Write(int b);

        public virtual void Write(byte[] b)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            Write(b, 0, b.Length);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if ((uint)offset > buffer.Length || (uint)count > buffer.Length - offset) throw new ArgumentOutOfRangeException();
            for (int i = 0; i < count; i++)
            {
                Write(buffer[offset + i]);
            }
        }

        // ---- Stream abstract members / capabilities ----
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { /* no-op by default */ }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
