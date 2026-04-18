using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        uint[] res = new uint[aDigits.Length + bDigits.Length];

        if (a.IsZero() || b.IsZero()) return new("0", 10);

        ulong carry = 0;
        ulong cur;
        for (int i = 0; i < aDigits.Length; i++)
        {
            for (int j = 0; j < bDigits.Length; j++)
            {
                cur = (ulong)aDigits[i] * bDigits[j] + res[i + j] + carry;
                res[i + j] = (uint)cur;
                carry = cur >> 32;
            }

            res[i + bDigits.Length] += (uint)carry;
            carry = 0;
        }

        return new(res, a.IsNegative != b.IsNegative);
    }
}