using System;
using System.Reflection;
using UnityEngine;

public static class SerializedFieldValidator
{
    public static void Validate(object target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            if (field.IsDefined(typeof(SerializeField), false) && !field.FieldType.IsValueType && field.GetValue(target) == null)
                throw new InvalidOperationException($"{target.GetType().Name} is missing serialized field reference: {field.Name}");
        }
    }
}