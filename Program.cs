using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using MPI;

class Program
{
    static void Main(string[] args)
    {
        var steps = 100;
        var length = 1000;
        var width = 1000;
        var map = GetInitialConfiguration(length, width, new Random(), 100_000);
        SaveToFile(map, length, width, "large_map.txt");
        
        var timer = new Stopwatch();
        timer.Start();
        for (var i = 0; i < steps; i++)
        {
            map = PerformStep(map, length, width);
        }
        timer.Stop();
        Console.WriteLine(timer.ElapsedMilliseconds);

        SaveToFile(map, length, width, "output.txt");
    }

    public static bool[] PerformStep(bool[] map, int length, int width)
    {
        var newState = new bool[length * width];
        for(var i = 0; i < width; i++)
        {
            for(var j = 0; j < length; j++)
            {
                var alives = GetNeighborsCount(map, length, width, i, j);
                if (alives == 3 || alives == 2 && map[i * length + j])
                    newState[i * length + j] = true;
            }
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