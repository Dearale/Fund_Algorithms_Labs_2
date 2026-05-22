using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit = 0;
    
    private uint _smallValue = 0; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data = null;
    
    public bool IsNegative => _signBit == 1;
    private const ulong BaseValue = (ulong)uint.MaxValue + 1;
    private const int NumOfBits = sizeof(uint) * 8;

    private const int HalfDigit = NumOfBits / 2;
    private const uint RightHalfMask = (1 << HalfDigit) - 1;
    public const int SimpleMultThreshold = 8;

    private static readonly IMultiplier SimpleMultiplier = new SimpleMultiplier();
    private static readonly IMultiplier KaratsubaMultiplier = new KaratsubaMultiplier();
    private static readonly IMultiplier FftMultiplier = new FftMultiplier();

    private const int KaratsubaThreshold = 32;
    private const int FftThreshold = 256;


    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        int len = digits.Length;
        while (len > 0 && digits[len - 1] == 0) len--;
        _data = null;

        if (len == 0) return;

        _signBit = isNegative ? 1 : 0;

        if (len == 1)
        {
            _smallValue = digits[0];
            return;
        }

        _data = digits[..len];

    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false) : this(digits.ToArray(), isNegative) { }

    public BetterBigInteger(string value, int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), "radix must be in [2..36]");
        }

        if (value == "")
            throw new ArgumentException(nameof(value), "value cannot be empty");

        int start = 0;
        if (value[start] == '+' || value[start] == '-')
        {
            _signBit = value[start] == '+' ? 0 : 1;
            start++;
        }
        if (start == value.Length) throw new FormatException("Sign without digits");

        while (start < value.Length && value[start] == '0') start++;

        List<uint> words = new();
        for (int i = start; i < value.Length; i++)
        {
            int digitValue = GetValueFromDigit(value[i]);
            if (digitValue < 0 || digitValue >= radix)
            {
                throw new FormatException($"Invalid digit {value[i]} for radix {radix}.");
            }

            uint carry = (uint)digitValue;

            for (int j = 0; j < words.Count; j++)
            {
                ulong current = (ulong) words[j] * (uint) radix + carry;
                words[j] = (uint)current;
                carry = (uint)(current >> 32);
            }

            if (carry > 0)
            {
                words.Add(carry);
            }
        }

        if (words.Count == 0)
        {
            _signBit = 0;
            return;
        }
        if (words.Count == 1)
        {
            _smallValue = words[0];
            return;
        }

        _data = words.ToArray();
    }
    
    public int GetValueFromDigit(char digit)
    {
        if (digit >= '0' && digit <= '9') return digit - '0';
        if (digit >= 'a' && digit <= 'z') return digit - 'a' + 10;
        if (digit >= 'A' && digit <= 'Z') return digit - 'A' + 10;
        return -1;
    }
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other == null)
        {
            return 1;
        }
        if (this.IsNegative && !other.IsNegative) return -1;
        if (!this.IsNegative && other.IsNegative) return 1;

        int res = CompareMagnitudes(other);
        return IsNegative ? -res : res;
    }

    private int CompareMagnitudes(IBigInteger other)
        => CompareMagnitudes(this.GetDigits(), other.GetDigits());

    private static int CompareMagnitudes(ReadOnlySpan<uint> thisDigits, ReadOnlySpan<uint> otherDigits)
    {
        if (thisDigits.Length != otherDigits.Length)
        {
            return thisDigits.Length < otherDigits.Length ? -1 : 1;
        }

        for (int i = thisDigits.Length - 1; i >= 0; i--)
        {
            if (thisDigits[i] < otherDigits[i]) return -1;
            if (thisDigits[i] > otherDigits[i]) return 1;
        }
        return 0;
    }

    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(_signBit);
        if (_data is null) hash.Add(_smallValue);
        else
        {
            foreach (uint item in _data)
            {
                hash.Add(item);
            }
        }
        return hash.ToHashCode();
    }
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        uint[] sum;
        if (a.IsNegative == b.IsNegative)
        {
            sum = AddMagnitudes(a, b);
            return new(sum, a.IsNegative);
        }

        int magnitudesComparison = a.CompareMagnitudes(b);
        if (magnitudesComparison == 0)
            return new("0", 10);
        if (magnitudesComparison <= 0)
        {
            sum = SubMagnitudes(b, a);
            return new(sum, b.IsNegative);
        }
        sum = SubMagnitudes(a, b);
        return new(sum, a.IsNegative);
    }

    private static uint[] AddMagnitudes(BetterBigInteger a, BetterBigInteger b)
        => AddMagnitudes(a.GetDigits(), b.GetDigits());

    public static uint[] AddMagnitudes(ReadOnlySpan<uint> aDigits, ReadOnlySpan<uint> bDigits)
    {
        uint[] sum = new uint[Math.Max(aDigits.Length, bDigits.Length) + 1];
        uint carry = 0;
        int minLength = Math.Min(aDigits.Length, bDigits.Length);
        int i = 0;
        for (; i < minLength; i++)
            sum[i] = AddDigits(aDigits[i], bDigits[i], ref carry);

        ReadOnlySpan<uint> leftOver = aDigits.Length >= bDigits.Length ? aDigits : bDigits;
        for (; i < leftOver.Length; i++)
            sum[i] = AddDigits(leftOver[i], 0, ref carry);

        if (carry > 0) sum[sum.Length - 1] = carry;

        return sum;
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

    private static uint[] AddNumber(ReadOnlySpan<uint> aDigits, uint digit)
    {
        uint[] sum = new uint[aDigits.Length + 1];
        uint carry = digit;
        for (int i = 0; i < aDigits.Length; i++)
            sum[i] = AddDigits(aDigits[i], 0, ref carry);

        if (carry > 0) sum[sum.Length - 1] = carry;

        return sum;
    }

    private static uint[] SubMagnitudes(BetterBigInteger a, BetterBigInteger b)
        => SubMagnitudes(a.GetDigits(), b.GetDigits());

    public static uint[] SubMagnitudes(ReadOnlySpan<uint> aDigits, ReadOnlySpan<uint> bDigits)
    {
        uint[] sum = new uint[aDigits.Length];
        uint borrow = 0;
        int i = 0;
        for (; i < bDigits.Length; i++)
        {
            sum[i] = aDigits[i] - borrow - bDigits[i];
            borrow = (aDigits[i] < borrow || (aDigits[i] - borrow < bDigits[i])) ? 1u : 0u;
        }
        for (; i < aDigits.Length; i++)
        {
            sum[i] = aDigits[i] - borrow;
            borrow = aDigits[i] < borrow ? 1u : 0u;
        }

        return sum;
    }

    private static uint[] SubNumber(ReadOnlySpan<uint> aDigits, uint digit)
    {
        uint[] sum = new uint[aDigits.Length];
        uint borrow = digit;
        for (int i = 0; i < aDigits.Length; i++)
        {
            sum[i] = aDigits[i] - borrow;
            borrow = (uint)(aDigits[i] < borrow ? 1 : 0);
        }

        return sum;
    }


    public static void SubMagnitudesInPlace(uint[] aDigits, uint[] bDigits)
    {
        uint borrow = 0;
        int i = 0;
        for (; i < bDigits.Length; i++)
        {
            uint new_borrow = (aDigits[i] < borrow || aDigits[i] - borrow < bDigits[i]) ? 1u : 0u;
            aDigits[i] = aDigits[i] - borrow - bDigits[i];
            borrow = new_borrow;
        }
        for (; i < aDigits.Length; i++)
        {
            uint new_borrow = aDigits[i] < borrow ? 1u : 0u;
            aDigits[i] -= borrow;
            borrow = new_borrow;
        }
    }


    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
        => a + (-b);
    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        return new(a.GetDigits().ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger Abs(BetterBigInteger a)
        => new(a.GetDigits().ToArray());

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        if (bDigits.Length == 1 && bDigits[0] == 0) throw new DivideByZeroException(nameof(b));

        int magnitudesComparison = a.CompareMagnitudes(b);
        if (magnitudesComparison < 0)
        {
            return new("0", 10);
        }

        if (magnitudesComparison == 0)
        {
            return new([1], a.IsNegative != b.IsNegative);
        }

        if (a.IsNegative == b.IsNegative)
            return DivideAlgorithm(Abs(a), Abs(b));

        return -DivideAlgorithm(Abs(a), Abs(b));
    }

    private static BetterBigInteger DivideAlgorithm(BetterBigInteger a, BetterBigInteger b)
        => DivideAlgorithm(a, b, out _);

    private static BetterBigInteger DivideAlgorithm(BetterBigInteger a, BetterBigInteger b, out BetterBigInteger rem)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        BetterBigInteger remainder = new([0]);

        int curDividendIndex = aDigits.Length - 1;
        while (remainder < b && curDividendIndex != -1)
        {
            remainder.AppendDigit(aDigits[curDividendIndex]);
            curDividendIndex--;
        }

        BetterBigInteger quotient = new([0]);
        while (remainder >= b)
        {
            uint curQuotient = BinarySearchQuotient(remainder, b);
            quotient.AppendDigit(curQuotient);

            BetterBigInteger betterCurQuotient = new([curQuotient]);

            remainder -= betterCurQuotient * b;

            while (remainder < b && curDividendIndex != -1)
            {
                remainder.AppendDigit(aDigits[curDividendIndex]);
                curDividendIndex--;

                if (remainder < b)
                    quotient.AppendDigit(0);
            }
        }

        rem = remainder;
        return quotient;
    }

    private static uint BinarySearchQuotient(BetterBigInteger carry, BetterBigInteger divisor)
    {
        uint l = 1;
        uint r = uint.MaxValue;
        uint res = l;
        while (l <= r)
        {
            uint m = l + (r - l) / 2;
            BetterBigInteger M = new BetterBigInteger([m]);
            BetterBigInteger mult = divisor * M;
            if (mult < carry)
            {
                res = m;
                l = m + 1;
            }
            else if (mult == carry) return m;
            else r = m - 1;
        }

        return res;
    }

    private void AppendDigit(uint digit)
    {
        if (_smallValue == 0 && _data == null)
        {
            _smallValue = digit;
            return;
        }

        uint[] newDigits = new uint[GetDigits().Length + 1];
        newDigits[0] = digit;
        Array.Copy(GetDigits().ToArray(), 0, newDigits, 1, GetDigits().Length);
        _data = newDigits;
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        if (bDigits.Length == 1 && bDigits[0] == 0) throw new DivideByZeroException(nameof(b));

        int magnitudesComparison = a.CompareMagnitudes(b);
        if (magnitudesComparison < 0)
        {
            return new(a.GetDigits().ToArray(), a.IsNegative);
        }

        if (magnitudesComparison == 0)
        {
            return new("0", 10);
        }
        if (a.IsNegative)
            return -GetRemainderDivideAlgorithm(Abs(a), Abs(b));

        return GetRemainderDivideAlgorithm(Abs(a), Abs(b));
    }

    private static BetterBigInteger GetRemainderDivideAlgorithm(BetterBigInteger a, BetterBigInteger b)
    {
        DivideAlgorithm(a, b, out BetterBigInteger rem);
        return rem;
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        IMultiplier multiplier;
        int maxLen = Math.Max(a.GetDigits().Length, b.GetDigits().Length);

        if (maxLen >= FftThreshold)
            multiplier = FftMultiplier;
        else if (maxLen >= KaratsubaThreshold)
            multiplier = KaratsubaMultiplier;
        else
            multiplier = SimpleMultiplier;
        return multiplier.Multiply(a, b);
    }
    
    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        return new(a.IsNegative
            ? SubNumber(a.GetDigits(), 1)
            : AddNumber(a.GetDigits(), 1), !a.IsNegative);
    }

    private enum BitwiseOperation
    {
        And, Or, Xor
    }

    private static BetterBigInteger ApplyBitwise(BetterBigInteger a, BetterBigInteger b, BitwiseOperation op)
    {
        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        ReadOnlySpan<uint> aDigits = ToTwosComplement(a, length);
        ReadOnlySpan<uint> bDigits = ToTwosComplement(b, length);

        uint[] res = new uint[length];
        if (op == BitwiseOperation.And)
        {
            for (int i = 0; i < length; i++)
                res[i] = aDigits[i] & bDigits[i];
        } else if (op == BitwiseOperation.Or)
        {
            for (int i = 0; i < length; i++)
                res[i] = aDigits[i] | bDigits[i];
        } else if (op == BitwiseOperation.Xor)
        {
            for (int i = 0; i < length; i++)
                res[i] = aDigits[i] ^ bDigits[i];
        }

        return FromTwosComplement(res);
    }

    private static ReadOnlySpan<uint> ToTwosComplement(BetterBigInteger a, int length)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();

        uint[] res = new uint[length];

        aDigits.CopyTo(res);

        if (!a.IsNegative) return res;

        for (int i = 0; i < res.Length; i++)
        {
            res[i] = ~res[i];
        }

        return AddNumber(res, 1);
    }

    private static BetterBigInteger FromTwosComplement(uint[] digits)
    {
        if (digits[digits.Length - 1] >> (NumOfBits - 1) == 0)
            return new(digits, isNegative: false);

        uint[] res = new uint[digits.Length - 1];

        for (int i = 0; i < res.Length; i++)
        {
            res[i] = ~digits[i];
        }

        return new(AddNumberInPlace(res, 1), isNegative: true);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
        => ApplyBitwise(a, b, BitwiseOperation.And);

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
        => ApplyBitwise(a, b, BitwiseOperation.Or);

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
        => ApplyBitwise(a, b, BitwiseOperation.Xor);

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift) => ShiftLeftSigned(a, shift);
    public static BetterBigInteger operator >>(BetterBigInteger a, int shift) => ShiftLeftSigned(a, -shift);

    private static BetterBigInteger ShiftLeftSigned(BetterBigInteger a, int shift)
        => new(ShiftLeftSigned(a.GetDigits(), shift, a.IsNegative), a.IsNegative);


    private static uint[] ShiftLeftSigned(ReadOnlySpan<uint> digits, int shift, bool isNegative = false)
    {
        if (shift == 0) return digits.ToArray();


        int unsignedShift = Math.Abs(shift);

        int newLength;
        if (shift < 0)
        {
            if (unsignedShift >= digits.Length * NumOfBits)
                return isNegative ? [1] : [0];

            newLength = digits.Length;
        }
        else
            newLength = digits.Length + (unsignedShift - 1) / NumOfBits + 1;

        uint[] newDigits = new uint[newLength];

        int indexShift = unsignedShift / NumOfBits;
        int localShift = unsignedShift % NumOfBits;
        uint carry = 0;
        bool add1ForTwosComplement = false;
        if (shift < 0 && isNegative)
        {
            for (int i = 0; i < indexShift; i++)
            {
                if (digits[i] != 0)
                {
                    add1ForTwosComplement = true;
                    break;
                }
            }

            if (!add1ForTwosComplement)
            {
                int mask = (1 << localShift) - 1;
                add1ForTwosComplement = (mask & digits[indexShift]) != 0;
            }
        }

        if (localShift == 0)
        {
            if (shift < 0)
            {
                for (int i = digits.Length - 1; i >= indexShift; i--)
                    newDigits[i - indexShift] = digits[i];
            }
            else
            {
                for (int i = 0; i + indexShift < newDigits.Length; i++)
                    newDigits[i + indexShift] = digits[i];
            }
        } else
        {
            if (shift < 0)
            {
                for (int i = digits.Length - 1; i >= indexShift; i--)
                {
                    newDigits[i - indexShift] = carry | (digits[i] >> localShift);
                    carry = digits[i] << (NumOfBits - localShift);
                }
            } else
            {
                for (int i = 0; i + indexShift < newDigits.Length - 1; i++)
                {
                    newDigits[i + indexShift] = (digits[i] << localShift) | carry;
                    carry = digits[i] >> (NumOfBits - localShift);
                }

                if (carry != 0) newDigits[newDigits.Length - 1] = carry;
            }
        }

        if (add1ForTwosComplement)
            return AddNumberInPlace(newDigits, 1);
        return newDigits;
    }

    private static uint[] AddNumberInPlace(uint[] aDigits, uint digit)
    {
        uint carry = digit;
        for (int i = 0; i < aDigits.Length; i++)
        {
            aDigits[i] = AddDigits(aDigits[i], 0, ref carry);
        }

        if (carry > 0)
        {
            uint[] newDigits = new uint[aDigits.Length + 1];
            Array.Copy(aDigits, newDigits, aDigits.Length);
            newDigits[newDigits.Length - 1] = carry;
            return newDigits;
        }
        return aDigits;
    }

    public static void AddMagnitudesInPlace(uint[] aDigits, ReadOnlySpan<uint> bDigits, int startIndex)
    {
        uint carry = 0;
        int i = 0;
        for (; i < bDigits.Length; i++)
        {
            aDigits[i + startIndex] = AddDigits(aDigits[i + startIndex], bDigits[i], ref carry);
        }
        i += startIndex;
        for (; i < aDigits.Length; i++)
        {
            aDigits[i] = AddDigits(aDigits[i], 0, ref carry);
        }
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), "radix must be in [2..36]");
        }

        if (IsZero()) return "0";

        List<char> res = new();

        uint[] digits = GetDigits().ToArray();

        int start = digits.Length - 1;
        while (true)
        {
            while (start >= 0 && digits[start] == 0) start--;
            if (start < 0) break;

            uint rem = DivideInPlaceAndGetRem(digits, (uint)radix, start);
            res.Add(GetDigitFromValue(rem));
        }

        if (IsNegative) res.Add('-');

        res.Reverse();
        return new string(res.ToArray());
    }

    private static char GetDigitFromValue(uint val)
    {
        if (val >= 36) throw new ArgumentOutOfRangeException(nameof(val), "each digit must be in [2..36]");

        if (val <= 9) return (char)('0' + val);

        return (char)(val - 10 + 'A');
    }


    private static uint DivideInPlaceAndGetRem(uint[] aDigits, uint bDigit, int start)
    {
        ulong carry = 0;
        for (int i = start; i >= 0; i--)
        {
            ulong cur = (carry << NumOfBits) | aDigits[i];
            carry = cur % bDigit;
            aDigits[i] = (uint)(cur / bDigit);
        }

        return (uint)carry;
    }

    public bool IsZero() => IsZero(GetDigits());

    private static bool IsZero(ReadOnlySpan<uint> digits)
    {
        foreach (uint item in digits)
        {
            if (item > 0) return false;
        }
        return true;
    }
}