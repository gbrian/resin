using System;
using System.IO;
using System.Text;

namespace Resin.IO.Read
{
    public class StreamingTrieReader : TrieReader, IDisposable
    {
        private readonly TextReader _reader;

        public StreamingTrieReader(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.None);
            _reader = new StreamReader(fs, Encoding.Unicode);
        }

        public StreamingTrieReader(TextReader reader)
        {
            _reader = reader;
            LastRead = LcrsNode.MinValue;
            Replay = LcrsNode.MinValue;
        }

        protected override void Skip(int count)
        {
            var buffer = new char[LcrsNode.BlockSize * count];
            _reader.Read(buffer, 0, buffer.Length);
        }

        protected override LcrsNode Step()
        {
            if (Replay != LcrsNode.MinValue)
            {
                var replayed = Replay;
                Replay = LcrsNode.MinValue;
                return replayed;
            }

            var data = new char[LcrsNode.BlockSize];
            if (_reader.Read(data, 0, data.Length) == 0)
            {
                return LcrsNode.MinValue;
            }

            LastRead = new LcrsNode(new string(data));
            return LastRead;
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }
        }
    }
}