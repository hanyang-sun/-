/*
 * Copyright (c) 2011 Juan Mellado
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
using System;

namespace BrickDefense.ThirdParty
{
    // C# adaptation of src/aruco.js getMarker/hammingDistance/mat2id/rotate.
    // Source: jcmellado/js-aruco @ 2203d4b5efb601c39054a341bf04702409699383.
    // Changes: binary samples supplied by caller; reject ambiguous rotations; expose template encoder.
    public static class ArucoCode
    {
        private static readonly int[,] Rows =
        { {1,0,0,0,0}, {1,0,1,1,1}, {0,1,0,0,1}, {0,1,1,1,0} };

        public static bool[,] Encode(int id)
        {
            if (id < 0 || id > 1023) throw new ArgumentOutOfRangeException(nameof(id));
            var cells = new bool[7,7];
            for (int r = 0; r < 5; r++)
            {
                int row = (id >> (2 * (4 - r))) & 3;
                for (int c = 0; c < 5; c++) cells[r + 1, c + 1] = Rows[row,c] == 1;
            }
            return cells;
        }

        public static bool TryDecode(bool[,] cells, out int id, out int rotations)
        {
            id = -1; rotations = 0;
            if (cells == null || cells.GetLength(0) != 7 || cells.GetLength(1) != 7) return false;
            for (int i = 0; i < 7; i++)
                if (cells[0,i] || cells[6,i] || cells[i,0] || cells[i,6]) return false;
            var bits = new bool[5,5];
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 5; c++) bits[r,c] = cells[r + 1,c + 1];
            int matches = 0;
            for (int turn = 0; turn < 4; turn++)
            {
                if (HammingDistance(bits) == 0)
                {
                    int candidate = 0;
                    for (int r = 0; r < 5; r++)
                    {
                        candidate = (candidate << 1) | (bits[r,1] ? 1 : 0);
                        candidate = (candidate << 1) | (bits[r,3] ? 1 : 0);
                    }
                    id = candidate; rotations = turn; matches++;
                }
                bits = Rotate(bits);
            }
            return matches == 1;
        }

        private static int HammingDistance(bool[,] bits)
        {
            int distance = 0;
            for (int r = 0; r < 5; r++)
            {
                int minimum = 5;
                for (int row = 0; row < 4; row++)
                {
                    int sum = 0;
                    for (int c = 0; c < 5; c++)
                        if (bits[r,c] != (Rows[row,c] == 1)) sum++;
                    minimum = Math.Min(minimum, sum);
                }
                distance += minimum;
            }
            return distance;
        }

        private static bool[,] Rotate(bool[,] src)
        {
            var dst = new bool[5,5];
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 5; c++) dst[r,c] = src[4 - c,r];
            return dst;
        }
    }
}
