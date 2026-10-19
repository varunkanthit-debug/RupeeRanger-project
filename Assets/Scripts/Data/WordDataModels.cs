using System;
using System.Collections.Generic;

namespace RupeeRanger.Data
{
    [Serializable]
    public class WordSearchPuzzleData
    {
        public string difficulty;
        public string category;
        public int gridSize;
        public List<string> words = new List<string>();
    }

    [Serializable]
    public class WordFinderPuzzleData
    {
        public string difficulty;
        public List<string> letters = new List<string>();
        public List<string> targetWords = new List<string>();
    }

    [Serializable]
    public class WordDatabase
    {
        public List<WordSearchPuzzleData> wordSearchPuzzles = new List<WordSearchPuzzleData>();
        public List<WordFinderPuzzleData> wordFinderPuzzles = new List<WordFinderPuzzleData>();
    }
}
