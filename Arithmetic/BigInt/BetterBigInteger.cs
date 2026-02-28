using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit = 0;
    
    private uint _smallValue = 0; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data = null;
    
    public bool IsNegative => _signBit == 1;
    private const ulong BaseValue = (ulong)uint.MaxValue + 1;
    private const int NumOfBits = 32;
    
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


        int start = 0;
        if (value[start] == '+' || value[start] == '-')
        {
            _signBit = value[start] == '+' ? 0 : 1;
            start++;
        }
        if (start == value.Length) throw new FormatException("Sign without digits");

        while (value[start] == '0') start++;

        List<uint> words = new();
        for (int i = 0; i < value.Length - start; i++)
        {
            int digitValue = GetValueFromDigit(value[i]);
            if (digitValue < 0)
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
    {
        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();
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
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        List<uint> sum = new();
        uint carry = 0;
        int minLength = Math.Min(aDigits.Length, bDigits.Length);
        int i = 0;
        for (; i < minLength; i++)
        {
            ulong cur = aDigits[i] + bDigits[i] + carry;
            sum.Add((uint)cur);
            carry = (uint)(cur >> 32);
        }
        ReadOnlySpan<uint> leftOver = aDigits.Length >= bDigits.Length ? aDigits : bDigits;
        for (; i < leftOver.Length; i++)
        {
            ulong cur = leftOver[i] + carry;
            sum.Add((uint)cur);
            carry = (uint)(cur >> 32);
        }

        if (carry > 0) sum.Add(carry);

        return sum.ToArray();
    }


    private static uint[] SubMagnitudes(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        List<uint> sum = new();
        uint borrow = 0;
        int minLength = Math.Min(aDigits.Length, bDigits.Length);
        int i = 0;
        for (; i < minLength; i++)
        {
            ulong cur;
            if (aDigits[i] < (ulong)bDigits[i] + borrow)
            {
                cur = aDigits[i] + BaseValue - bDigits[i] - borrow;
                borrow = 1;
            } else
            {
                cur = aDigits[i] - borrow - bDigits[i];
                borrow = 0;
            }
            sum.Add((uint)cur);
        }
        ReadOnlySpan<uint> leftOver = aDigits.Length >= bDigits.Length ? aDigits : bDigits;
        for (; i < leftOver.Length; i++)
        {
            ulong cur;
            if (leftOver[i] < borrow)
            {
                cur = leftOver[i] + BaseValue - borrow;
                borrow = 1;
            } else
            {
                cur = leftOver[i] - borrow;
                borrow = 0;
            }
            sum.Add((uint)cur);
        }

        return sum.ToArray();
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        uint[] sum;
        if (a.IsNegative != b.IsNegative)
        {
            sum = AddMagnitudes(a, b);
            return new(sum, a.IsNegative);
        }
        int magnitudesComparison = a.CompareMagnitudes(b);
        if (magnitudesComparison == 0) return new("0", 10);
        if (magnitudesComparison < 0)
        {
            sum = SubMagnitudes(b, a);
            return new(sum, !a.IsNegative);
        }
        sum = SubMagnitudes(a, b);
        return new(sum, a.IsNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        ReadOnlySpan<uint> digits = a.GetDigits();
        if (digits.Length == 1 && digits[0] == 0)
            return new(digits.ToArray(), false);
        return new(a.GetDigits().ToArray(), !a.IsNegative);
    }

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

        if (bDigits.Length == 1)
        {
            return DivideByOneWord(a, b);
        } else
        {
            return DivideAlgorithm(a, b);
        }
    }

    private static BetterBigInteger DivideByOneWord(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        uint bDigit = b.GetDigits()[0];

        ulong carry = 0;
        uint[] res = new uint[aDigits.Length];
        for (int i = aDigits.Length - 1; i >= 0; i--)
        {
            ulong cur = (carry << 32) | aDigits[i];
            carry = cur % bDigit;
            res[i] = (uint)(cur / bDigit);
        }

        return new(res, a.IsNegative != b.IsNegative);
    }

    private static BetterBigInteger DivideAlgorithm(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> bDigitsInitial = b.GetDigits();
        int shift = BitOperations.LeadingZeroCount(bDigitsInitial[bDigitsInitial.Length - 1]);
        BetterBigInteger newA = a << shift;
        BetterBigInteger newB = b << shift;
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        int resDigitsLen = aDigits.Length - bDigits.Length;
        uint[] res = new uint[resDigitsLen];

        ulong bDigit1 = bDigits[bDigits.Length - 1];
        ulong bDigit2 = bDigits[bDigits.Length - 2];
        for (int i = resDigitsLen - 1; i >= 0; i--)
        {
            ulong aDigit1 = aDigits[i + bDigits.Length];
            ulong aDigit2 = aDigits[i + bDigits.Length - 1];
            ulong aDigit3 = aDigits[i + bDigits.Length - 2];

            ulong numerator = aDigit1 * BaseValue + aDigit2;
            ulong quotient = numerator / bDigit1;
            ulong remainder = numerator % bDigit1;

            while (quotient == BaseValue || (bDigits.Length >= 2 && ((quotient * bDigit2) > (remainder * BaseValue + aDigit3))))
            {
                quotient--;
                remainder += bDigit1;
                if (remainder >= BaseValue) break;
            }
        }
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    
    
    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
       => throw new NotImplementedException("Умножение делегируется стратегии, выбирать необходимо в зависимости от размеров чисел");
    
    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        if (a.IsNegative)
        {
            BetterBigInteger integer = a - new BetterBigInteger("1", 10);
            return -integer;
        } else
        {
            BetterBigInteger integer = a + new BetterBigInteger("1", 10);
            return -integer;
        }
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) { }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator <<(BetterBigInteger a, int shift) => ShiftLeftSigned(a, shift);
    public static BetterBigInteger operator >>(BetterBigInteger a, int shift) => ShiftLeftSigned(a, -shift);
    private static BetterBigInteger ShiftLeftSigned(BetterBigInteger a, int shift)
    {
        if (shift == 0) return new(a.GetDigits().ToArray(), a.IsNegative);

        ReadOnlySpan<uint> digits = a.GetDigits();
        List<uint> newDigits = new(digits.Length);
        for (int i = 0; i < digits.Length; i++) newDigits.Add(0);

        int unsignedShift = Math.Abs(shift);
        if (unsignedShift >= digits.Length * NumOfBits) return new("0", 10);


        int indexShift = unsignedShift / NumOfBits;
        int localShift = unsignedShift % NumOfBits;
        uint carry = 0;
        if (shift < 0)
        {
            for (int i = indexShift; i < digits.Length; i++)
            {
                newDigits[i - indexShift] = (digits[i] << (NumOfBits - localShift)) | carry;
                carry = digits[i] >> localShift;
            }
        } else
        {
            for (int i = 0; i + indexShift < digits.Length; i++)
            {
                newDigits[i + indexShift] = (digits[i] << localShift) | carry;
                carry = digits[i] >> (NumOfBits - localShift);
            }
        }
        return new(digits.ToArray(), a.IsNegative);
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    public override string ToString() => ToString(10);
    public string ToString(int radix) => throw new NotImplementedException();
    
}