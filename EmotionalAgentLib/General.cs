using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmotionalAgentLib
{
    [Serializable]
    public class Tuple<T1, T2>
    {
        public T1 First { get;  set; }
        public T2 Second { get;  set; }
        public Tuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }

    public class ProportionValue<T>
    {
        public double Proportion { get; set; }
        public T Value { get; set; }
    }

    public static class ProportionValue
    {
        public static ProportionValue<T> Create<T>(double proportion, T value)
        {
            return new ProportionValue<T> { Proportion = proportion, Value = value };
        }

        static Random random = new Random();
        public static T ChooseByRandom<T>(
            this IEnumerable<ProportionValue<T>> collection)
        {
            var rnd = random.NextDouble();
            foreach (var item in collection)
            {
                if (rnd < item.Proportion)
                    return item.Value;
                rnd -= item.Proportion;
            }
            throw new InvalidOperationException(
                "The proportions in the collection do not add up to 1.");
        }
    }

    public static class General
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
