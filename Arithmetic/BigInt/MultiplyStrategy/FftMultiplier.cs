using Arithmetic.BigInt.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Windows.Markup;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const int NumOfBits = sizeof(uint) * 8;
    private const int CoefNumOfBits = NumOfBits / 2;
    private const uint CoefMask = (1 << CoefNumOfBits) - 1;
    private const int HalfDigit = NumOfBits / 2;
    private const uint RightHalfMask = (1 << HalfDigit) - 1;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero() || b.IsZero())
            return new BetterBigInteger([0]);

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        uint[] res = Multiply(aDigits, bDigits);

        return new BetterBigInteger(res, a.IsNegative != b.IsNegative);
    }

    private static uint[] Multiply(ReadOnlySpan<uint> aDigits, ReadOnlySpan<uint> bDigits)
    {
        uint[] a = ToBase2Power16(aDigits);
        uint[] b = ToBase2Power16(bDigits);

        int resCoefCount = a.Length + b.Length - 1;
        int n = NextPowerOfTwo(resCoefCount);

        int coefBoundBits = NumOfBits + CeilLog2(Math.Min(a.Length, b.Length));
        int rootReq = Math.Max(1, n / 2);
        int m = RoundUpToMultiple(coefBoundBits, rootReq);

        Ring ring = new Ring(m);

        uint[][] fa = new uint[n][];
        uint[][] fb = new uint[n][];
        for (int i = 0; i < n; i++)
        {
            fa[i] = i < a.Length ? ring.FromUInt(a[i]) : ring.Zero();
            fb[i] = i < b.Length ? ring.FromUInt(b[i]) : ring.Zero();
        }

        NumberTheoreticTransform(fa, invert: false, ring);
        NumberTheoreticTransform(fb, invert: false, ring);

        for (int i = 0; i < n; i++)
        {
            fa[i] = ring.Multiply(fa[i], fb[i]);
        }

        NumberTheoreticTransform(fa, invert: true, ring);

        ulong[] coefs = new ulong[resCoefCount];
        for (int i = 0; i < coefs.Length; i++)
        {
            coefs[i] = ring.ToULong(fa[i]);
        }

        return FromBase2Power16WithCarry(coefs);
    }

    private static void NumberTheoreticTransform(uint[][] vals, bool invert, Ring ring)
    {
        int n = vals.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;

            if (i < j)
                (vals[i], vals[j]) = (vals[j], vals[i]);
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            int baseStep = (2 * ring.modBits) / len;
            int step = invert ? 2 * ring.modBits - baseStep : baseStep;
            int half = len >> 1;
            int period = 2 * ring.modBits;

            for (int block = 0; block < n; block += len)
            {
                int exp = 0;
                for (int j = 0; j < half; j++)
                {
                    uint[] u = vals[block + j];
                    uint[] v = ring.MultiplyByPowerOfTwo(vals[block + j + half], exp);

                    vals[block + j] = ring.Add(u, v);
                    vals[block + j + half] = ring.Subtract(u, v);

                    exp += step;
                    if (exp >= period)
                        exp %= period;
                }
            }
        }

        if (invert)
        {
            int logN = Log2PowerOfTwo(n);

            int inverseShift = ring.modBits - logN;
            for (int i = 0; i < n; i++)
            {
                vals[i] = ring.Negate(ring.MultiplyByPowerOfTwo(vals[i], inverseShift));
            }
        }
    }

    private static uint[] ToBase2Power16(ReadOnlySpan<uint> digits)
    {
        uint[] res = new uint[digits.Length * 2];
        for (int i = 0; i < digits.Length; i++)
        {
            res[2 * i] = digits[i] & CoefMask;
            res[2 * i + 1] = digits[i] >> CoefNumOfBits;
        }

        return TrimLeadingZeros(res);
    }

    private static uint[] FromBase2Power16WithCarry(ReadOnlySpan<ulong> coefs)
    {
        List<uint> base16 = new();
        ulong carry = 0;

        for (int i = 0; i < coefs.Length; i++)
        {
            ulong cur = coefs[i] + carry;
            base16.Add((uint)(cur & CoefMask));
            carry = cur >> CoefNumOfBits;
        }

        while (carry != 0)
        {
            base16.Add((uint)(carry & CoefMask));
            carry >>= CoefNumOfBits;
        }

        uint[] res = new uint[(base16.Count + 1) / 2];
        for (int i = 0; i < base16.Count; i++)
        {
            if (i % 2 == 0)
                res[i / 2] |= base16[i];
            else
                res[i / 2] |= base16[i] << CoefNumOfBits;
        }

        return TrimLeadingZeros(res);
    }

    private static uint[] TrimLeadingZeros(ReadOnlySpan<uint> span)
    {
        int i = span.Length - 1;
        while (i >= 0 && span[i] == 0) i--;
        return span[..(i + 1)].ToArray();
    }

    private static int NextPowerOfTwo(int val)
    {
        int res = 1;
        while (res < val)
            res <<= 1;
        return res;
    }

    private static int Log2PowerOfTwo(int val)
    {
        int res = 0;
        while ((1 << res) != val)
            res++;
        return res;
    }

    private static int CeilLog2(int val)
    {
        if (val <= 1)
            return 0;

        int res = 0;
        int x = val - 1;
        while (x != 0)
        {
            res++;
            x >>= 1;
        }
        return res;
    }


    private static int RoundUpToMultiple(int val, int multiple)
    {
        return ((val - 1) / multiple + 1) * multiple;
    }

    private class Ring
    {

        public readonly int modBits;
        private readonly int wordCount;
        private readonly int modWord;
        private readonly int modBit;
        private readonly uint[] modulus;

        public Ring(int modBits)
        {
            this.modBits = modBits;
            modWord = modBits / NumOfBits;
            modBit = modBits % NumOfBits;

            wordCount = modWord + 2;

            modulus = new uint[wordCount];
            modulus[0] = 1;
            modulus[modWord] |= 1u << modBit;
        }

        public uint[] Zero() => new uint[wordCount];

        public uint[] FromUInt(uint value)
        {
            uint[] res = new uint[wordCount];
            res[0] = value;
            return res;
        }

        public uint[] Add(uint[] a, uint[] b)
        {
            uint[] res = new uint[wordCount];

            uint carry = 0;
            for (int i = 0; i < wordCount; i++)
            {
                res[i] = AddDigits(a[i], b[i], ref carry);
            }

            if (Compare(res, modulus) >= 0)
                SubInPlace(res, modulus);

            return res;
        }

        public uint[] Subtract(uint[] a, uint[] b)
        {
            uint[] res = new uint[wordCount];
            uint borrow = 0;

            for (int i = 0; i < wordCount; i++)
            {
                uint new_borrow = (a[i] < borrow || a[i] - borrow < b[i]) ? 1u : 0u;
                res[i] = a[i] - borrow - b[i];
                borrow = new_borrow;
            }

            if (borrow != 0)
                AddInPlace(res, modulus);


            return res;
        }

        public uint[] Negate(uint[] value)
        {
            if (IsZero(value))
                return Zero();

            uint[] res = new uint[wordCount];
            Array.Copy(modulus, res, wordCount);
            SubInPlace(res, value);
            return res;
        }

        public uint[] Multiply(uint[] a, uint[] b)
        {
            if (IsZero(a) || IsZero(b))
                return Zero();

            uint[] aTrimmed = TrimLeadingZeros(a);
            uint[] bTrimmed = TrimLeadingZeros(b);

            uint[] res = new KaratsubaMultiplier().Multiply(new BetterBigInteger(aTrimmed), new BetterBigInteger(bTrimmed)).GetDigits().ToArray();

            return ReduceRawProduct(res);
        }

        private uint[] ReduceRawProduct(uint[] prod)
        {
            uint[] low = LowModBits(prod);
            uint[] high = ShiftRight(prod, modBits, wordCount);

            return Subtract(low, high);
        }

        private static uint[] TrimLeadingZeros(uint[] a)
        {
            int length = a.Length;
            while (length > 1 && a[length - 1] == 0)
                length--;

            uint[] res = new uint[length];
            Array.Copy(a, res, length);
            return res;
        }

        public uint[] MultiplyByPowerOfTwo(uint[] val, int exp)
        {
            int period = 2 * modBits;
            exp %= period;
            if (exp < 0)
                exp += period;

            if (exp == 0)
                return val.ToArray();

            if (exp >= modBits)
                return Negate(MultiplyByPowerOfTwo(val, exp - modBits));

            uint[] shifted = ShiftLeft(val, exp, wordCount * 2);
            uint[] low = LowModBits(shifted);
            uint[] high = ShiftRight(shifted, modBits, wordCount);

            return Subtract(low, high);
        }

        public ulong ToULong(uint[] val)
        {
            ulong res = 0;
            int maxWords = Math.Min(2, wordCount);
            for (int i = 0; i < maxWords; i++)
                res |= (ulong)val[i] << (NumOfBits * i);

            return res;
        }

        private uint[] LowModBits(uint[] val)
        {
            uint[] res = new uint[wordCount];

            Array.Copy(val, res, Math.Min(modWord, val.Length));

            if (modWord < val.Length && modBit != 0)
            {
                uint mask = (1u << modBit) - 1u;
                res[modWord] = val[modWord] & mask;
            }

            return res;
        }

        private static uint[] ShiftLeft(uint[] a, int shift, int resLen)
        {
            uint[] res = new uint[resLen];
            int indexShift = shift / NumOfBits;
            int localShift = shift % NumOfBits;
            uint carry = 0;
            if (localShift == 0)
            {
                for (int i = 0; i + indexShift < res.Length && i < a.Length; i++)
                    res[i + indexShift] = a[i];
            }
            else
            {
                int i = 0;
                for (; i + indexShift < res.Length && i < a.Length; i++)
                {
                    res[i + indexShift] = (a[i] << localShift) | carry;
                    carry = a[i] >> (NumOfBits - localShift);
                }

                if (carry != 0 && i + indexShift < res.Length)
                    res[i + indexShift] = carry;
            }
            return res;
        }

        private static uint[] ShiftRight(uint[] a, int shift, int resLen)
        {
            uint[] res = new uint[resLen];
            int indexShift = shift / NumOfBits;
            int localShift = shift % NumOfBits;
            uint carry = 0;
            if (localShift == 0)
            {
                for (int i = Math.Min(a.Length - 1, resLen - 1 + indexShift); i >= indexShift; i--)
                    res[i - indexShift] = a[i];
            }
            else
            {
                for (int i = Math.Min(a.Length - 1, resLen - 1 + indexShift); i >= indexShift; i--)
                {
                    res[i - indexShift] = carry | (a[i] >> localShift);
                    carry = a[i] << (NumOfBits - localShift);
                }
            }
            return res;
        }

        private static bool IsZero(uint[] value)
        {
            for (int i = 0; i < value.Length; i++)
                if (value[i] != 0)
                    return false;
            return true;
        }

        private static int Compare(uint[] a, uint[] b)
        {
            for (int i = a.Length - 1; i >= 0; i--)
            {
                if (a[i] < b[i])
                    return -1;
                if (a[i] > b[i])
                    return 1;
            }
            return 0;
        }

        private static void AddInPlace(uint[] a, uint[] b)
        {
            uint carry = 0;
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = AddDigits(a[i], b[i], ref carry);
            }
        }

        private static void SubInPlace(uint[] a, uint[] b)
        {
            uint borrow = 0;
            for (int i = 0; i < a.Length; i++)
            {
                uint new_borrow = (a[i] < borrow || a[i] - borrow < b[i]) ? 1u : 0u;
                a[i] = a[i] - borrow - b[i];
                borrow = new_borrow;
            }
        }

        private static uint AddDigits(uint a, uint b, ref uint carry)
        {
            uint aRightHalf = a & RightHalfMask;
            uint bRightHalf = b & RightHalfMask;
            uint rightHalf = AddHalfDigits(aRightHalf, bRightHalf, ref carry);

            uint aLeftHalf = a >> HalfDigit;
            uint bLeftHalf = b >> HalfDigit;

            uint leftHalf = AddHalfDigits(aLeftHalf, bLeftHalf, ref carry) << HalfDigit;
            return leftHalf + rightHalf;
        }

        private static uint AddHalfDigits(uint a, uint b, ref uint carry)
        {
            uint res = a + b + carry;
            carry = res >> HalfDigit;
            return res & RightHalfMask;
        }

        private void ClearBitsAboveRange(uint[] value)
        {
            if (modBit == 31)
            {
                for (int i = modWord + 1; i < value.Length; i++)
                {
                    value[i] = 0;
                }
                return;
            }

            uint mask = (1u << (modBit + 1)) - 1u;
            value[modWord] &= mask;
            for (int i = modWord + 1; i < value.Length; i++)
                value[i] = 0;
        }
    }
}