using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TinySerializationAttribute : Attribute
    {
        public bool ReadOnly;
    }
}
