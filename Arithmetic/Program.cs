// тут можно что-то тестить

using Arithmetic.BigInt;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;
using System.Diagnostics;

MultiplierPerformanceDemo.Run();

internal static class MultiplierPerformanceDemo
{
    public static void Run()
    {
        int[] sizesInUIntWords =
        {
            128,
            256,
            512,
            1024,
            2048,
            4096
        };

        IMultiplier simple = new SimpleMultiplier();
        IMultiplier karatsuba = new KaratsubaMultiplier();
        IMultiplier fft = new FftMultiplier();

        Console.WriteLine("Multiplier performance demo");
        Console.WriteLine("Size = number of uint words, 1 word = 32 bits");
        Console.WriteLine();

        foreach (int size in sizesInUIntWords)
        {
            Console.WriteLine($"=== Size: {size} uint words ({size * 32} bits) ===");

            BetterBigInteger a = RandomBigInteger(size, seed: 1000 + size);
            BetterBigInteger b = RandomBigInteger(size, seed: 2000 + size);

            BetterBigInteger expected = Measure("Simple", simple, a, b, out long simpleMs);
            // BetterBigInteger expected = Measure("Karatsuba", karatsuba, a, b, out long simpleMs);
            BetterBigInteger karatsubaResult = Measure("Karatsuba", karatsuba, a, b, out long karatsubaMs);
            BetterBigInteger fftResult = Measure("FFT / Schönhage-Strassen", fft, a, b, out long fftMs);

            if (!expected.Equals(karatsubaResult))
                throw new Exception("Karatsuba result is incorrect.");

            if (!expected.Equals(fftResult))
                throw new Exception("FFT result is incorrect.");

            Console.WriteLine($"Simple:    {simpleMs} ms");
            Console.WriteLine($"Karatsuba: {karatsubaMs} ms");
            Console.WriteLine($"FFT:       {fftMs} ms");
            Console.WriteLine();
        }
    }

    private static BetterBigInteger Measure(
        string name,
        IMultiplier multiplier,
        BetterBigInteger a,
        BetterBigInteger b,
        out long elapsedMs)
    {
        multiplier.Multiply(a, b);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Stopwatch sw = Stopwatch.StartNew();

        BetterBigInteger result = multiplier.Multiply(a, b);

        sw.Stop();

        elapsedMs = sw.ElapsedMilliseconds;
        return result;
    }

    private static BetterBigInteger RandomBigInteger(int uintWordCount, int seed)
    {
        Random random = new Random(seed);
        uint[] digits = new uint[uintWordCount];

        for (int i = 0; i < digits.Length; i++)
        {
            uint low = (uint)random.Next();
            uint high = (uint)random.Next();

            digits[i] = low ^ (high << 16);
        }

        digits[^1] |= 0x80000000u;

        return new BetterBigInteger(digits, isNegative: false);
    }
}