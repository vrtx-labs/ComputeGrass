using System.Collections.Generic;

namespace VRTX.ComputeGrass.Extensions
{
    public static class Collections
    {
        public static void Clear<T>(this T[] array)
        {
            if (array != null)
                for (int i = 0; i < array.Length; i++)
                    array[i] = default(T);
        }
        public static T GetRandom<T>(this T[] array) => array[UnityEngine.Random.Range(0, array.Length)];
        public static T GetRandom<T>(this IList<T> list) => list[UnityEngine.Random.Range(0, list.Count)];

        public static string Output<T>(this T[] array)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < array.Length; i++)
            { sb.AppendLine(array[i].ToString()); }
            return sb.ToString();
        }
        public static string Output<T, U>(this T[] array, U[] otherArray, string delimiter = ":")
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append($"{array[i]}{delimiter}");
                if (otherArray.Length > i)
                    sb.AppendLine(otherArray[i].ToString());
            }
            return sb.ToString();
        }


        public static void RemoveRange<T>(this List<T> list, List<T> items)
        {
            if (list != items)
            {
                foreach (T item in items)
                    list.Remove(item);
            }
        }
        public static void Replace<T>(this List<T> list, T item)
        {
            list.Clear();
            if (item != null)
                list.Add(item);
        }
        public static void Replace<T>(this List<T> list, List<T> items)
        {
            list.Clear();
            if (items != null)
                list.AddRange(items);
        }
        public static void Replace<T>(this List<T> list, IEnumerable<T> items)
        {
            list.Clear();
            if (items != null)
                list.AddRange(items);
        }

        public static void Shuffle<T>(this T[] array)
        {
            for (var i = array.Length; i > 0; i--)
                array.Swap(0, UnityEngine.Random.Range(0, i));
        }
        public static void Shuffle<T>(this IList<T> list)
        {
            for (var i = list.Count; i > 0; i--)
                list.Swap(0, UnityEngine.Random.Range(0, i));
        }
        public static void Shuffle<T>(this T[] array, System.Random rnd)
        {
            for (var i = array.Length; i > 0; i--)
                array.Swap(0, rnd.Next(0, i));
        }
        public static void Shuffle<T>(this IList<T> list, System.Random rnd)
        {
            for (var i = list.Count; i > 0; i--)
                list.Swap(0, rnd.Next(0, i));
        }
        public static void Swap<T>(this T[] array, int i, int j)
        {
            var temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

    }

}