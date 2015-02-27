using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// Extensions and LINQ stuff. Handy dandy.

namespace libCgfx
{
    public static class Util
    {
        public static IEnumerable<int> For(int end)
        {
            for (int i = 0; i < end; ++i)
                yield return i;
        }

        public static IEnumerable<int> For(int start, int end)
        {
            for (int i = start; i <= end; i++)
                yield return i;
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T item in collection)
                action(item);
        }

        #region IsBitSet

        public static bool IsBitSet(this ulong value, int bit)
        {
            return value.GetBit(bit) == 1;
        }
        public static bool IsBitSet(this uint value, int bit)
        {
            return GetBit(value, bit) == 1;
        }
        public static bool IsBitSet(this ushort value, int bit)
        {
            return GetBit(value, bit) == 1;
        }
        public static bool IsBitSet(this byte value, int bit)
        {
            return GetBit(value, bit) == 1;
        }

        #endregion IsBitSet

        public static int GetBit(this ulong value, int bit)
        {
            return (int)(value >> bit) & 1;
        }

        public static Vector3 Multiply(this Vector3 v, Matrix m)
        {
            return new Vector3(
                (m.M11 * v.X) + (m.M12 * v.Y) + (m.M13 * v.Z),
                (m.M21 * v.X) + (m.M22 * v.Y) + (m.M23 * v.Z),
                (m.M31 * v.X) + (m.M32 * v.Y) + (m.M33 * v.Z))
                   + new Vector3(m.M14, m.M24, m.M34);
        }
        
        // TODO: NEEDS MORE TESTING
        public static Quaternion ToRotationQuaternion(this Vector3 vDirection)
        {
            // Step 1. Setup basis vectors describing the rotation given the input vector and assuming an initial up direction of (0, 1, 0)
            var vUp = new Vector3(0, 1.0f, 0.0f);
            // Y Up vector
            Vector3 vRight = Vector3.Cross(vUp, vDirection);
            // The perpendicular vector to Up and Direction
            vUp = Vector3.Cross(vDirection, vRight);
            // The actual up vector given the direction and the right vector
            // Step 2. Put the three vectors into the matrix to bulid a basis rotation matrix
            // This step isnt necessary, but im adding it because often you would want to
            // convert from matricies to quaternions instead of vectors to quaternions
            // If you want to skip this step, you can use the vector values directly in the quaternion setup below
            var mBasis = new Matrix(vRight.X, vRight.Y, vRight.Z, 0.0f,
                vUp.X, vUp.Y, vUp.Z, 0.0f,
                vDirection.X, vDirection.Y, vDirection.Z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            // Step 3. Build a quaternion from the matrix
            var qrot = new Quaternion();
            qrot.W = (float)Math.Sqrt(1.0f + mBasis.M11 + mBasis.M22 + mBasis.M33) / 2.0f;
            double dfWScale = qrot.W * 4.0;
            qrot.X = (float)((mBasis.M32 - mBasis.M23) / dfWScale);
            qrot.Y = (float)((mBasis.M13 - mBasis.M31) / dfWScale);
            qrot.Z = (float)((mBasis.M21 - mBasis.M12) / dfWScale);
            return qrot;
        }
    }


    /*
     * Credit: Jeff Mercado
     * http://stackoverflow.com/questions/4171140/iterate-over-values-in-flags-enum
     */
    public static class Extensions
    {

        public static IEnumerable<Enum> GetFlags(this Enum value)
        {
            return GetFlags(value, Enum.GetValues(value.GetType()).Cast<Enum>().ToArray());
        }

        public static IEnumerable<Enum> GetIndividualFlags(this Enum value)
        {
            return GetFlags(value, GetFlagValues(value.GetType()).ToArray());
        }

        private static IEnumerable<Enum> GetFlags(Enum value, Enum[] values)
        {
            ulong bits = Convert.ToUInt64(value);
            List<Enum> results = new List<Enum>();
            for (int i = values.Length - 1; i >= 0; i--)
            {
                ulong mask = Convert.ToUInt64(values[i]);
                if (i == 0 && mask == 0L)
                    break;
                if ((bits & mask) == mask)
                {
                    results.Add(values[i]);
                    bits -= mask;
                }
            }
            if (bits != 0L)
                return Enumerable.Empty<Enum>();
            if (Convert.ToUInt64(value) != 0L)
                return results.Reverse<Enum>();
            if (bits == Convert.ToUInt64(value) && values.Length > 0 && Convert.ToUInt64(values[0]) == 0L)
                return values.Take(1);
            return Enumerable.Empty<Enum>();
        }

        private static IEnumerable<Enum> GetFlagValues(Type enumType)
        {
            ulong flag = 0x1;
            foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
            {
                ulong bits = Convert.ToUInt64(value);
                if (bits == 0L)
                    //yield return value;
                    continue; // skip the zero value
                while (flag < bits) flag <<= 1;
                if (flag == bits)
                    yield return value;
            }
        }
    }

}
