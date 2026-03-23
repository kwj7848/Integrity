using System;
using UnityEngine;

namespace Integrity
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AllowEmptyAttribute : PropertyAttribute { }
}
