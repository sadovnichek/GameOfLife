using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using MPI;

class Program
{
    public static void Main(string[] args)
    {
        MPI.Environment.Run(ref args, communicator =>
        {
            var rank = communicator.Rank;
            var size = communicator.Size;
            bool[] initialState = null;
            int length = 0;
            int width = 0;
            int steps = 0;

            if (rank == 0)
            {
                (initialState, length, width) = LoadFromFile(args[0]);
                steps = int.Parse(args[1]);
                Console.WriteLine(DisplayState(initialState, length, width));
            }

            communicator.Broadcast(ref initialState, 0);
            communicator.Broadcast(ref length, 0);
            communicator.Broadcast(ref width, 0);
            communicator.Broadcast(ref steps, 0);

            var blockWidth = width / size;

            var currentBlock = new bool[(blockWidth + 2) * length];

            for (var i = 0; i < blockWidth; i++)
            {
                for (var j = 0; j < length; j++)
                {
                    currentBlock[length + i * length + j] = initialState[rank * blockWidth * length + i * length + j];
                }
            }

            for (var s = 0; s < steps; s++)
            {
                var localTop = currentBlock.Skip(length).Take(length).ToArray();
                var localBot = currentBlock.Skip(length * blockWidth).Take(length).ToArray();

                var topRank = (rank - 1 + size) % size;
                var bottomRank = (rank + 1) % size;

                //Console.WriteLine($"{rank}: sent top row to {topRank}");
                communicator.SendReceive(localTop, topRank, 0, bottomRank, 0, out var topRow);
                //Console.WriteLine($"{rank}: received top row: {string.Join("", topRow.Select(x => x ? "*" : "."))}");

                //Console.WriteLine($"{rank}: sent bottom row to {bottomRank}");
                communicator.SendReceive(localBot, bottomRank, 1, topRank, 1, out var bottomRow);
                //Console.WriteLine($"{rank}: received bottom row: {string.Join("", bottomRow.Select(x => x ? "*" : "."))}");

                for (var i = 0; i < length; i++)
                {
                    currentBlock[i] = bottomRow[i];
                }
                for (var i = 0; i < length; i++)
                {
                    currentBlock[(blockWidth + 1) * length + i] = topRow[i];
                }

                //Console.WriteLine($"{rank}: Local block before step performed\n{DisplayState(currentBlock, length, blockWidth + 2)}");
                currentBlock = PerformStep(currentBlock, length, blockWidth + 2);
                //Console.WriteLine($"{rank}: step {s + 1} performed\n{DisplayState(currentBlock, length, blockWidth + 2)}");

                communicator.Barrier();
            }

            var gathered = communicator.Gather(currentBlock, 0);
            communicator.Barrier();

            if (rank == 0)
            {
                for(var i = 0; i < gathered.GetLength(0); i++)
                {
                    var subState = gathered[i].Skip(length).Take(length * blockWidth).ToArray();
                    Console.Write(DisplayState(subState, length, blockWidth));
                }
            }
        });
    }

    public static bool[] PerformStep(bool[] map, int length, int width)
    {
        var newState = new bool[length * width];

        for(var i = 0; i < length; i++)
        {
            newState[i] = map[i];
        }

        for(var i = 1; i < width - 1; i++)
        {
            for(var j = 0; j < length; j++)
            {
                var alives = GetNeighborsCount(map, length, width, i, j);
                if (alives == 3 || alives == 2 && map[i * length + j])
                    newState[i * length + j] = true;
            }
        }

        for (var i = 0; i < length; i++)
        {
            newState[(width - 1) * length + i] = map[(width - 1) * length + i];
        }

        return newState;
    }

    public static string DisplayState(bool[] map, int length, int width)
    {
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < width; i++)
        {
            for(var j = 0; j < length; j++)
            {
                if (map[i * length + j])
                    stringBuilder.Append('*');
                else
                    stringBuilder.Append('.');
            }
            stringBuilder.Append('\n');
        }
        return stringBuilder.ToString();
    }

    public static bool[] GetInitialConfiguration(int length, int width, Random random, int filledCells = 25)
    {
        var map = new bool[length * width];
        for(var i = 0; i < filledCells; i++)
        {
            var x = random.Next(0, width);
            var y = random.Next(0, length);
            map[x * length + y] = true;
        }
        return map;
    }

    public static int GetNeighborsCount(bool[] map, int length, int width, int i, int j)
    {
        var alives = 0;
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }
                var u = i + dx >= 0 ? (i + dx) % width : (i + dx + width) % width;
                var v = j + dy >= 0 ? (j + dy) % length : (j + dy + length) % length;
                if (map[u * length + v])
                {
                    alives++;
                }
            }
        }
        return alives;
    }

    public static (bool[], int, int) LoadFromFile(string filename)
    {
        using (var reader = new StreamReader(filename))
        {
            var lines = new List<string>();
            while(!reader.EndOfStream)
            {
                lines.Add(reader.ReadLine());
            }
            var length = lines.First().Length;
            var map = new bool[lines.Count * length];
            for(var i = 0; i < lines.Count; i++)
            {
                for(var j = 0; j < length; j++)
                {
                    if (lines[i][j] == '*')
                        map[i * length + j] = true;
                }
            }
            return (map, length, lines.Count);
        }
    }

    public static void SaveToFile(bool[] map, int length, int width, string filename)
    {
        using(var writer = new StreamWriter(filename))
        {
            writer.Write(DisplayState(map, length, width));
        }
    }
}