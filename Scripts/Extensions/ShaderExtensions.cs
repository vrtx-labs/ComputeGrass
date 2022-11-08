using UnityEngine;

namespace VRTX.ComputeGrass.Extensions
{
    public static class Shaders
    {
        public static bool TryReleaseAndDispose(this ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer.Dispose();
                return true;
            }
            return false;
        }
        public static void SetKeyword(this ComputeShader shader, string keyword, bool state)
        {
            if (shader != null)
            {
                if (state)
                    shader.EnableKeyword(keyword);
                else
                    shader.DisableKeyword(keyword);
            }
        }
        public static void SetKeyword(this Material material, string keyword, bool state)
        {
            if (material != null)
            {
                if (state)
                    material.EnableKeyword(keyword);
                else
                    material.DisableKeyword(keyword);
            }
        }
        public static void SetKeyword(this ComputeShader shader, string[] keywords, int index = -1)
        {
            if (shader != null)
            {
                if (index < 0)
                    foreach (string keyword in keywords)
                        shader.SetKeyword(keyword, false);

                if (index >= 0 && keywords.Length > index)
                    shader.SetKeyword(keywords[index], true);
            }
        }
        public static void SetKeyword(this Material material, string[] keywords, int index = -1)
        {
            if (material != null)
            {
                if (index < 0)
                    foreach (string keyword in keywords)
                        material.SetKeyword(keyword, false);

                if (index >= 0 && keywords.Length > index)
                    material.SetKeyword(keywords[index], true);
            }
        }

        public enum ByteUnits : int
        {
            Base = 0,
            Kilo = 1,
            Mega = 2,
            Giga = 3,
            Tera = 4,
            Peta = 5,
            Exa = 6,
            Zetta = 7,
            Yotta = 8,
        }
        public static float ToByteSize(this int sizeInBytes, ByteUnits toUnit, bool useDecimal = false) => ((long)sizeInBytes).ToByteSize(toUnit, useDecimal);
        public static float ToByteSize(this long sizeInBytes, ByteUnits toUnit, bool useDecimal = false)
        {
            float baseFactor = useDecimal ? 1000 : 1024;
            float unitFactor = Mathf.Pow(baseFactor, (int)toUnit);
            float result = ((float)sizeInBytes) / unitFactor;
            return result;
        }


    }


}