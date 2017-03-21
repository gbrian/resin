﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Resin.Analysis;
using Resin.IO.Read;

namespace Resin.IO
{
    [Serializable, DebuggerDisplay("{Value} {EndOfWord}")]
    public class LcrsTrie : ITrieReader
    {
        public LcrsTrie RightSibling { get; set; }
        public LcrsTrie LeftChild { get; set; }
        public BlockInfo PostingsInfo { get; set; }
        
        public char Value { get; private set; }
        public bool EndOfWord { get; private set; }

        public LcrsTrie(char value, bool endOfWord)
        {
            Value = value;
            EndOfWord = endOfWord;
        }

        public void Add(string path)
        {
            Add(path, BlockInfo.MinValue);
        }

        public void Add(string path, BlockInfo postingsInfo)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("word");

            var key = path[0];
            var eow = path.Length == 1;

            LcrsTrie node;
            if (!TryGetChild(key, out node))
            {
                node = new LcrsTrie(key, eow);
                node.RightSibling = LeftChild;
                LeftChild = node;
            }
            else
            {
                if (!node.EndOfWord)
                {
                    node.EndOfWord = eow;
                }
            }

            if (eow)
            {
                PostingsInfo = postingsInfo;
            }
            else
            {
                node.Add(path.Substring(1), postingsInfo);
            }
        }

        private bool TryGetChild(char c, out LcrsTrie node)
        {
            node = LeftChild;
            
            while (node != null)
            {
                if (node.Value == c)
                {
                    return true;
                }
                node = node.RightSibling;
            }

            node = null;
            return false;
        }

        public IEnumerable<LcrsTrie> GetLeftChildAndAllOfItsSiblings()
        {
            if (LeftChild != null)
            {
                yield return LeftChild;

                var sibling = LeftChild.RightSibling;

                while (sibling != null)
                {
                    yield return sibling;

                    sibling = sibling.RightSibling;
                }
            }
        }

        public IEnumerable<LcrsTrie> GetAllSiblings()
        {
            if (RightSibling != null)
            {
                yield return RightSibling;

                var sibling = RightSibling.RightSibling;

                while (sibling != null)
                {
                    yield return sibling;

                    sibling = sibling.RightSibling;
                }
            }
        }

        public int GetWeight()
        {
            var count = 1;

            if (LeftChild != null)
            {
                count = count + LeftChild.GetWeight();
            }

            if (RightSibling != null)
            {
                count = count + RightSibling.GetWeight();
            }

            return count;
        }

        public bool HasWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) throw new ArgumentException("path");

            LcrsTrie child;
            if (TryFindPath(word, out child))
            {
                return child.EndOfWord;
            }
            return false;
        }

        public IEnumerable<Word> StartsWith(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("traveled");

            var compressed = new List<Word>();

            LcrsTrie child;
            if (TryFindPath(prefix, out child))
            {
                child.LeftChild.DepthFirst(prefix, new List<char>(), compressed);
            }

            return compressed;
        }

        public IEnumerable<Word> Near(string word, int maxEdits)
        {
            var compressed = new List<Word>();
            if (LeftChild != null)
            {
                LeftChild.WithinEditDistanceDepthFirst(word, new string(new char[word.Length]), compressed, 0, maxEdits);
            }
            return compressed;
        }

        private void WithinEditDistanceDepthFirst(string word, string state, IList<Word> compressed, int depth, int maxEdits)
        {
            var childIndex = depth + 1;
            string test;

            if (depth == state.Length)
            {
                test = state + Value;
            }
            else
            {
                test = new string(state.ReplaceOrAppend(depth, Value).Where(c => c != Char.MinValue).ToArray());
            }

            var edits = Levenshtein.Distance(word, test);

            if (edits <= maxEdits)
            {
                if (EndOfWord)
                {
                    compressed.Add(new Word(test));
                }
            }

            if (edits <= maxEdits || test.Length < word.Length)
            {
                if (LeftChild != null)
                {
                    LeftChild.WithinEditDistanceDepthFirst(word, test, compressed, childIndex, maxEdits);
                }

                if (RightSibling != null)
                {
                    RightSibling.WithinEditDistanceDepthFirst(word, test, compressed, depth, maxEdits);
                }
            }
        }

        private void DepthFirst(string traveled, IList<char> state, IList<Word> compressed)
        {
            var copy = new List<char>(state);
            state.Add(Value);

            if (EndOfWord)
            {
                compressed.Add(new Word(traveled + new string(state.ToArray())));
            }

            if (LeftChild != null)
            {
                LeftChild.DepthFirst(traveled, state, compressed);
            }

            if (RightSibling != null)
            {
                RightSibling.DepthFirst(traveled, copy, compressed);
            }
        }

        public bool TryFindPath(string path, out LcrsTrie leaf)
        {
            var child = LeftChild;
            while (child != null)
            {
                if (child.Value.Equals(path[0]))
                {
                    break;
                }
                child = child.RightSibling;
            }
            if (child != null)
            {
                if (path.Length == 1)
                {
                    leaf = child;
                    return true;
                }
                return child.TryFindPath(path.Substring(1), out leaf);
            }
            leaf = null;
            return false;
        }
    }
}