using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class WrapAttribute : PropertyAttribute
    {
        public double Min { get; }
        public double Max { get; }

        /// <summary>
        /// Constrains a numeric value to a specified range and wraps it
        /// to the opposite limit when it exceeds the bounds.
        /// </summary>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        public WrapAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Constrains a numeric value to a specified range and wraps it
        /// to the opposite limit when it exceeds the bounds.
        /// </summary>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        public WrapAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Constrains a numeric value to a specified range and wraps it
        /// to the opposite limit when it exceeds the bounds.
        /// </summary>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        public WrapAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }
}
