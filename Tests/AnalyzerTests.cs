using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Resin;
using Resin.IO;

namespace Tests
{
    [TestFixture]
    public class AnalyzerTests
    {
        [Test, Ignore]
        public void Test2()
        {
            var dic = new Dictionary<string, int>();
            for (int i = 0; i < 10000000; i++)
            {
                dic.Add(Path.GetRandomFileName(), i);
            }
            var timer = new Stopwatch();
            timer.Start();
            using (var fs = File.Open(Path.Combine(Setup.Dir, "xxxxxxxxxxx.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                FileBase.Serializer.Serialize(fs, dic);
            }
            Trace.WriteLine("serialized in " + timer.Elapsed);
            timer.Restart();
            using (var fs = File.Open(Path.Combine(Setup.Dir, "xxxxxxxxxxx.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var obj = (Dictionary<string, int>)FileBase.Serializer.Deserialize(fs);
                //Assert.AreEqual(10000000, obj.Count);
            }
            Trace.WriteLine("deserialized in " + timer.Elapsed);
        }


        [Test, Ignore]
        public void Test()
        {
            var timer = new Stopwatch();
            timer.Start();
            using (var fs = File.Open(Path.Combine(Setup.Dir, "yyyyyyyyyyy.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var w = new StreamWriter(fs, Encoding.Unicode))
            {
                for (int i = 0; i < 10000000; i++)
                {
                    w.WriteLine("{0}:{1}", i, Guid.NewGuid());
                }
            }
            Trace.WriteLine("wrote in " + timer.Elapsed);
            timer.Restart();

            using (var fs = File.Open(Path.Combine(Setup.Dir, "yyyyyyyyyyy.txt"), FileMode.Open, FileAccess.Read, FileShare.None))
            using (var w = new StreamReader(fs, Encoding.Unicode))
            {
                string line;
                while ((line = w.ReadLine()) != null)
                {
                    var interestingPart = line.Substring(0, line.IndexOf(':'));
                    var id = int.Parse(interestingPart);
                    if(id==9999999) Trace.WriteLine(id);
                }
            }
            Trace.WriteLine("read in " + timer.Elapsed);
        }

        [Test]
        public void Stopwords()
        {
            var terms = new Analyzer(stopwords:new[]{"the", "a"}).Analyze("The americans sent us a new movie.").ToList();
            Assert.AreEqual(5, terms.Count);
            Assert.AreEqual("americans", terms[0]);
            Assert.AreEqual("sent", terms[1]);
            Assert.AreEqual("us", terms[2]);
            Assert.AreEqual("new", terms[3]);
            Assert.AreEqual("movie", terms[4]);
        }

        [Test]
        public void Separators()
        {
            var terms = new Analyzer(tokenSeparators:new []{'o'}).Analyze("hello world").ToList();
            Assert.AreEqual(3, terms.Count);
            Assert.AreEqual("hell", terms[0]);
            Assert.AreEqual("w", terms[1]);
            Assert.AreEqual("rld", terms[2]);
        }

        [Test]
        public void Can_analyze()
        {
            var terms = new Analyzer().Analyze("Hello!World?").ToList();
            Assert.AreEqual(2, terms.Count);
            Assert.AreEqual("hello", terms[0]);
            Assert.AreEqual("world", terms[1]);
        }

        [Test, Ignore]
        public void Can_analyze_wierdness()
        {
            var terms = new Analyzer().Analyze("Spanish noblewoman, († 1292) .net c#").ToList();
            Assert.AreEqual(5, terms.Count);
            Assert.AreEqual("spanish", terms[0]);
            Assert.AreEqual("noblewoman", terms[1]);
            Assert.AreEqual("1292", terms[2]);
            Assert.AreEqual(".net", terms[3]);
            Assert.AreEqual("c#", terms[4]);
        }

        [Test]
        public void Can_analyze_common()
        {
            var terms = new Analyzer().Analyze("German politician (CDU)").ToList();
            Assert.AreEqual(3, terms.Count);
            Assert.AreEqual("german", terms[0]);
            Assert.AreEqual("politician", terms[1]);
            Assert.AreEqual("cdu", terms[2]);
        }

        [Test]
        public void Can_analyze_space()
        {
            var terms = new Analyzer().Analyze("   (abc)   ").ToList();
            Assert.AreEqual(1, terms.Count);
            Assert.AreEqual("abc", terms[0]);

            terms = new Analyzer().Analyze(" ").ToList();
            Assert.AreEqual(0, terms.Count);

            terms = new Analyzer().Analyze("  ").ToList();
            Assert.AreEqual(0, terms.Count);
        }
    }
}