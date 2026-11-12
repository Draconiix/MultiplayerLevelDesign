using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector
{
    public class MinMaxSliderAttribute : PropertyAttribute
    {
        public readonly float minLimit = 0;
        public readonly float maxLimit = 1;
        public readonly bool showFields = true;

        /// <summary>
        /// Displays a dual-handle slider that allows selecting minimum and maximum
        /// values within a defined range, ideal for limits and randomization ranges.
        /// </summary>
        /// <param name="MinLimit">Minimum allowed value of the slider.</param>
        /// <param name="MaxLimit">Maximum allowed value of the slider.</param>
        public MinMaxSliderAttribute(float MinLimit, float MaxLimit)
        {
            this.minLimit = MinLimit;
            this.maxLimit = MaxLimit;
            this.showFields = true;
        }
    }
}
