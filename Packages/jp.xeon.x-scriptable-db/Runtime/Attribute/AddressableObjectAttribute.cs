using System;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class AddressableObjectAttribute : PropertyAttribute
    {
    }
}
