using System.Numerics;
using Arithmetic.BigInt;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.Tests;


[TestFixture]
public class BigIntArithmeticTests
{
    [Test]
    [Category("Base")]
    public void Test_Addition_Random()
    {
        Random rnd = new(52);
        for (int i = 0; i < 100; i++)
        {
            long valA = (long)rnd.Next() * rnd.Next();
            long valB = (long)rnd.Next() * rnd.Next();
            
            BetterBigInteger myA = new(valA.ToString(), 10);
            BetterBigInteger myB = new(valB.ToString(), 10);
            
            BetterBigInteger result = myA + myB;
            
            BigInteger expected = BigInteger.Parse(valA.ToString()) + BigInteger.Parse(valB.ToString());
            Assert.That(result.ToString(), Is.EqualTo(expected.ToString()));
        }
    }
    
    [Test]
    [Category("Base")]
    public void Test_Comparison_Logic()
    {
        Random rnd = new(52);
        for (int i = 0; i < 100; i++)
        {
            string s1 = GenerateLargeRandomString(rnd, rnd.Next(1, 50));
            string s2 = GenerateLargeRandomString(rnd, rnd.Next(1, 50));
            
            BetterBigInteger myA = new(s1, 10);
            BetterBigInteger myB = new(s2, 10);
            BigInteger expA = BigInteger.Parse(s1);
            BigInteger expB = BigInteger.Parse(s2);
            
            Assert.That(myA == myB, Is.EqualTo(expA == expB), $"Equality failed for {s1} and {s2}");
            Assert.That(myA < myB, Is.EqualTo(expA < expB), $"Less failed for {s1} and {s2}");
            Assert.That(myA > myB, Is.EqualTo(expA > expB), $"Greater failed for {s1} and {s2}");
            Assert.That(myA <= myB, Is.EqualTo(expA <= expB), $"LessOrEqual failed for {s1} and {s2}");
            Assert.That(myA >= myB, Is.EqualTo(expA >= expB), $"GreaterOrEqual failed for {s1} and {s2}");
        }
    }
    
    
    
    [Test]
    [Category("Base")]
    public void Test_Division_Random()
    {
        Random rnd = new (52);
        for (int i = 0; i < 50; i++)
        {
            string s1 = GenerateLargeRandomString(rnd, rnd.Next(10, 40));
            string s2 = GenerateLargeRandomString(rnd, rnd.Next(1, 10));
            Console.WriteLine(s1);
            Console.WriteLine(s2);
            if (s2 is "0" or "-0")
            {
                s2 = "1";
            }
            
            BetterBigInteger myA = new(s1, 10);
            BetterBigInteger myB = new(s2, 10);
            
            BetterBigInteger result = myA / myB;
            BigInteger expected = BigInteger.Parse(s1) / BigInteger.Parse(s2);
            
            Assert.That(result.ToString(), Is.EqualTo(expected.ToString()), $"Division failed: {s1} / {s2}");
        }
    }
    
    [Test]
    public void Test_DivideByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() =>
        {
            BetterBigInteger x = new BetterBigInteger([1]) / new BetterBigInteger([0]);
        });
    }
    
    [Test]
    [Category("Base")]
    public void Test_UnaryMinus_And_Modulo()
    {
        Random rnd = new (52);
        for (int i = 0; i < 50; i++)
        {
            string s1 = GenerateLargeRandomString(rnd, rnd.Next(5, 20));
            string s2 = GenerateLargeRandomString(rnd, rnd.Next(1, 5));
            if (s2 == "0" || s2 == "-0") s2 = "1";
            
            BetterBigInteger myA = new(s1, 10);
            BetterBigInteger myB = new(s2, 10);
            BigInteger expA = BigInteger.Parse(s1);
            BigInteger expB = BigInteger.Parse(s2);
            
            Assert.That((-myA).ToString(), Is.EqualTo((-expA).ToString()));
            Assert.That((myA % myB).ToString(), Is.EqualTo((expA % expB).ToString()));
        }
    }
    
    [Test]
    [Category("Base")]
    public void Test_Constructors_And_SSO_Threshold()
    {
        uint maxUint = uint.MaxValue;
        
        BetterBigInteger mySmall = new([maxUint], false);
        Assert.That(mySmall.ToString(), Is.EqualTo(maxUint.ToString()));
        
        uint[] bigDigits = [1, 1];
        BetterBigInteger myBig = new(bigDigits, false);
        BigInteger manualExpected = (BigInteger)1 * ((BigInteger)uint.MaxValue + 1) + 1;
        Assert.That(myBig.ToString(), Is.EqualTo(manualExpected.ToString()));
    }
    
    [Test]
    [Category("Base")]
    public void Test_Radix_Conversion()
    {
        string hexVal = "ABCDEF123456";
        BigInteger expected = BigInteger.Parse("0" + hexVal, System.Globalization.NumberStyles.HexNumber);
        
        BetterBigInteger my = new(hexVal, 16);
        
        Assert.That(my.ToString(10), Is.EqualTo(expected.ToString()));
    }
    
    
    [Test]
    [Category("Bitwise")]
    public void Test_Bitwise_Logic()
    {
        Random rnd = new(52);
        for (int i = 0; i < 50; i++)
        {
            string s1 = GenerateLargeRandomString(rnd, rnd.Next(5, 15));
            string s2 = GenerateLargeRandomString(rnd, rnd.Next(5, 15));
            
            BetterBigInteger myA = new(s1, 10);
            BetterBigInteger myB = new(s2, 10);
            BigInteger expA = BigInteger.Parse(s1);
            BigInteger expB = BigInteger.Parse(s2);
            
            Assert.That((myA & myB).ToString(), Is.EqualTo((expA & expB).ToString()),
                $"AND failed for {s1}, {s2}");
            Assert.That((myA | myB).ToString(), Is.EqualTo((expA | expB).ToString()),
                $"OR failed for {s1}, {s2}");
            Assert.That((myA ^ myB).ToString(), Is.EqualTo((expA ^ expB).ToString()),
                $"XOR failed for {s1}, {s2}");
            Assert.That((~myA).ToString(), Is.EqualTo((~expA).ToString()),
                $"NOT failed for {s1}");
        }
    }
    
    
    [Test]
    [Category("Bitwise")]
    public void Test_Shifts()
    {
        Random rnd = new(52);
        for (int i = 0; i < 50; i++)
        {
            string s1 = GenerateLargeRandomString(rnd, rnd.Next(1, 20));
            int shift = rnd.Next(1, 128);
            
            BetterBigInteger myA = new(s1, 10);
            BigInteger expA = BigInteger.Parse(s1);
            Assert.Multiple(() =>
            {
                Assert.That((myA << shift).ToString(), Is.EqualTo((expA << shift).ToString()),
                    $"Left shift {shift} failed for {s1}");
                Assert.That((myA >> shift).ToString(), Is.EqualTo((expA >> shift).ToString()),
                    $"Right shift {shift} failed for {s1}");
            });
        }
    }
    
    
    [Test]
    [Category("Base")]
    public void Test_EdgeCases()
    {
        BetterBigInteger zero = new("0", 10);
        BetterBigInteger one = new("1", 10);
        BetterBigInteger negOne = new("-1", 10);
        
        Assert.Multiple(() =>
        {
            Assert.That((zero * one).ToString(), Is.EqualTo("0"));
            Assert.That((one + negOne).ToString(), Is.EqualTo("0"));
            Assert.That(zero == new BetterBigInteger("0", 10), Is.True);
            Assert.That(one > negOne, Is.True);
            Assert.That((one << 0).ToString(), Is.EqualTo("1"));
        });
    }
    
    
    [Test]
    [Category("MultiplicationKaratsuba")]
    public void Test_Multiplication_Karatsuba()
    {
        string s1 = "123456789012345678901234567890";
        string s2 = "987654321098765432109876543210";
        
        BetterBigInteger myA = new(s1, 10);
        BetterBigInteger myB = new(s2, 10);
        
        KaratsubaMultiplier multiplier = new();
        BetterBigInteger myRes = multiplier.Multiply(myA, myB);
        
        BigInteger expected = BigInteger.Parse(s1) * BigInteger.Parse(s2);
        Assert.That(myRes.ToString(), Is.EqualTo(expected.ToString()));
    }
    
    
    [Test]
    [Category("MultiplicationFFT")]
    public void Test_Multiplication_FFT()
    {
        string s1 = "123456789012345678901234567890";
        string s2 = "987654321098765432109876543210";
        
        BetterBigInteger myA = new(s1, 10);
        BetterBigInteger myB = new(s2, 10);
        
        FftMultiplier multiplier = new();
        BetterBigInteger myRes = multiplier.Multiply(myA, myB);
        
        BigInteger expected = BigInteger.Parse(s1) * BigInteger.Parse(s2);
        Assert.That(myRes.ToString(), Is.EqualTo(expected.ToString()));
    }
    
    
    [Test]
    [Category("MultiplicationSimple")]
    public void Test_Multiplication_Simple()
    {
        string s1 = "123456789012345678901234567890";
        string s2 = "987654321098765432109876543210";
        
        BetterBigInteger myA = new(s1, 10);
        BetterBigInteger myB = new(s2, 10);
        SimpleMultiplier multiplier = new();
        
        BetterBigInteger myRes = multiplier.Multiply(myA, myB);
        
        BigInteger expected = BigInteger.Parse(s1) * BigInteger.Parse(s2);
        Assert.That(myRes.ToString(), Is.EqualTo(expected.ToString()));
    }
    
    // TODO: more tests for multiplications cases, one day
    
    private static string GenerateLargeRandomString(Random rnd, int length)
    {
        char[] digits = new char[length];
        digits[0] = (char)rnd.Next('1', '9' + 1);
        for (int i = 1; i < length; i++)
        {
            digits[i] = (char)rnd.Next('0', '9' + 1);
        }
        
        return (rnd.Next(2) == 0 ? "-" : "") + new string(digits);
    }
}