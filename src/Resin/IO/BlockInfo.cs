using System;

namespace Resin.IO
{
    [Serializable]
    public struct BlockInfo
    {
        public long Position;
        public int Length;

        public BlockInfo(long position, int length)
        {
            Position = position;
            Length = length;
        }
        public static BlockInfo MinValue { get { return new BlockInfo(0, 0);} }
    }
}