using System.Linq;

namespace SeaMonkey.ProbabilitySets
{
    public class FibonacciProbability : IntProbability
    {
        private readonly int[] _values;

        public enum Limit
        {
            _0 = 0,
            _1 = 1,
            _2 = 2,
            _3 = 3,
            _5 = 5,
            _8 = 8,
            _13 = 13,
            _21 = 21,
            _34 = 34,
            _55 = 55,
            _89 = 89,
            _144 = 144,
            _233 = 233,
            _377 = 377
        }

        public FibonacciProbability(Limit min = Limit._1, Limit max = Limit._13)
        {
            _values = new[]
                {
                    0,
                    1,
                    1,
                    2,
                    3,
                    5,
                    8,
                    13,
                    21,
                    34,
                    55,
                    89,
                    144,
                    233,
                    377
                }
                .Where(n => n >= (int) min)
                .Where(n => n <= (int) max)
                .ToArray();
        }

        public override int Get()
        {
            return _values[Program.Rnd.Next(0, _values.Length)];
        }
    }

    public class StaticValue : IntProbability
    {
        private readonly int _value;

        public StaticValue(int value)
        {
            _value = value;
        }
        public override int Get()
        {
            return _value;
        }
    }
}