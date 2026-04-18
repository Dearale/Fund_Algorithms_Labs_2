using Arithmetic.BigInt.Interfaces;
using System.Drawing;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero() || b.IsZero()) return new("0", 10);

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        uint[] res = MultiplyRec(aDigits, bDigits);

        return new(res, a.IsNegative != b.IsNegative);
    }

    private uint[] MultiplyRec(ReadOnlySpan<uint> aDigits, ReadOnlySpan<uint> bDigits)
    {
        aDigits = TrimLeadingZeros(aDigits);
        bDigits = TrimLeadingZeros(bDigits);
        if (aDigits.Length == 0 || bDigits.Length == 0) return [];

        if (aDigits.Length == 1 && bDigits.Length == 1)
        {
            ulong mult = (ulong)aDigits[0] * bDigits[0];
            if (mult <= uint.MaxValue) return [(uint)mult];
            return [(uint)mult, (uint)(mult >> 32)];
        }

        int maxLen = Math.Max(aDigits.Length, bDigits.Length);

        int halfLen = maxLen / 2;
        Slice(aDigits, halfLen, out ReadOnlySpan<uint> a, out ReadOnlySpan<uint> b);
        Slice(bDigits, halfLen, out ReadOnlySpan<uint> c, out ReadOnlySpan<uint> d);

        uint[] ac = MultiplyRec(a, c);
        uint[] bd = MultiplyRec(b, d);
        uint[] aAddB = BetterBigInteger.AddMagnitudes(a, b);
        uint[] cAddD = BetterBigInteger.AddMagnitudes(c, d);
        uint[] midValue = MultiplyRec(aAddB, cAddD);

        BetterBigInteger.SubMagnitudesInPlace(midValue, ac);
        BetterBigInteger.SubMagnitudesInPlace(midValue, bd);


        int resLen = Math.Max(2 * halfLen + ac.Length, Math.Max(halfLen + midValue.Length, bd.Length)) + 1;

        uint[] res = new uint[resLen];
        BetterBigInteger.AddMagnitudesInPlace(res, bd, 0);
        BetterBigInteger.AddMagnitudesInPlace(res, midValue, halfLen);
        BetterBigInteger.AddMagnitudesInPlace(res, ac, 2 * halfLen);

        return TrimLeadingZeros(res);
    }

    private void Slice(ReadOnlySpan<uint> digits, int length, out ReadOnlySpan<uint> a, out ReadOnlySpan<uint> b)
    {
        length = Math.Min(length, digits.Length);
        a = digits.Slice(length);
        b = digits.Slice(0, length);
    }

    private ReadOnlySpan<uint> TrimLeadingZeros(ReadOnlySpan<uint> span)
    {
        int i = span.Length - 1;
        while (i >= 0 && span[i] == 0) i--;
        return span.Slice(0, i + 1);
    }

    private uint[] TrimLeadingZeros(uint[] arr)
    {
        int i = arr.Length - 1;
        while (i >= 0 && arr[i] == 0) i--;
        if (i == arr.Length - 1) return arr;
        if (i < 0) return [];
        uint[] trimmed = new uint[i + 1];
        Array.Copy(arr, trimmed, i + 1);
        return trimmed;
    }
}