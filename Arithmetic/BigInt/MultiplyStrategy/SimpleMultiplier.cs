using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    private const int NumOfBits = sizeof(uint) * 8;
    private const int HalfDigit = (sizeof(uint) / 2) * 8;
    private const uint RightHalfMask = (1 << HalfDigit) - 1;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero() || b.IsZero()) return new("0", 10);

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        BetterBigInteger res = new BetterBigInteger([0], a.IsNegative != b.IsNegative);

        for (int i = 0; i < aDigits.Length; i++)
        {
            for (int j = 0; j < bDigits.Length; j++)
            {
                BetterBigInteger mult = Multiply(aDigits[i], bDigits[j]);
                mult <<= i * NumOfBits + j * NumOfBits;
                res += mult;
            }
        }

        if (a.IsNegative == b.IsNegative)
            return res;
        return -res;
    }

    private BetterBigInteger Multiply(uint a, uint b)
    {
        uint aRightHalf = a & RightHalfMask;
        uint bRightHalf = b & RightHalfMask;
        uint aLeftHalf = a >> HalfDigit;
        uint bLeftHalf = b >> HalfDigit;

        BetterBigInteger rightHalves = new([aRightHalf * bRightHalf]);
        BetterBigInteger rightLeftHalves = new BetterBigInteger([aRightHalf * bLeftHalf]) << HalfDigit;
        BetterBigInteger leftRightHalves = new BetterBigInteger([aLeftHalf * bRightHalf]) << HalfDigit;
        BetterBigInteger leftHalves = new BetterBigInteger([aLeftHalf * bLeftHalf]) << (2 * HalfDigit);

        return rightHalves + rightLeftHalves + leftRightHalves + leftHalves;
    }
}