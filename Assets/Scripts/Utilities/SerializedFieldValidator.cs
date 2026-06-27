using System;
using System.Reflection;
using UnityEngine;

public static class SerializedFieldValidator
{
    public static void Validate(object target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        Type type = target.GetType();

        while (type != null && type != typeof(MonoBehaviour))
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(target);
                bool isMissing =
                    value == null ||
                    value is UnityEngine.Object unityObject && unityObject == null;
                if (field.IsDefined(typeof(SerializeField), false) &&
                    !field.FieldType.IsValueType &&
                    isMissing)
                {
                    throw new InvalidOperationException(
                        $"{target.GetType().Name} is missing serialized field reference: {field.Name}"
                    );
                }
            }
            type = type.BaseType;
        }
    }
}