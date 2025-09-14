using System;
using System.ComponentModel;
using System.Globalization;

namespace TrueMapper.Core.Converters
{
    /// <summary>
    /// Smart type converter with advanced conversion capabilities
    /// </summary>
    public class SmartTypeConverter
    {
        /// <summary>
        /// Converts a value to the specified target type
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type to convert to</param>
        /// <returns>Converted value</returns>
        public object? Convert(object? value, Type targetType)
        {
            if (value == null)
            {
                return GetDefaultValue(targetType);
            }

            var sourceType = value.GetType();
            
            // Handle nullable types
            var nullableTargetType = Nullable.GetUnderlyingType(targetType);
            if (nullableTargetType != null)
            {
                if (value == null)
                    return null;
                targetType = nullableTargetType;
            }

            // Same type or assignable
            if (sourceType == targetType || targetType.IsAssignableFrom(sourceType))
            {
                return value;
            }

            // Handle enum conversions
            if (targetType.IsEnum)
            {
                return ConvertToEnum(value, targetType);
            }

            if (sourceType.IsEnum && (targetType == typeof(string) || targetType.IsPrimitive))
            {
                return ConvertFromEnum(value, targetType);
            }

            // Handle string conversions
            if (sourceType == typeof(string))
            {
                return ConvertFromString((string)value, targetType);
            }

            if (targetType == typeof(string))
            {
                return ConvertToString(value);
            }

            // Handle numeric conversions with range checking
            if (IsNumericType(sourceType) && IsNumericType(targetType))
            {
                return ConvertNumeric(value, targetType);
            }

            // Handle DateTime conversions
            if (IsDateTimeType(sourceType) || IsDateTimeType(targetType))
            {
                return ConvertDateTime(value, sourceType, targetType);
            }

            // Handle boolean conversions
            if (targetType == typeof(bool))
            {
                return ConvertToBoolean(value);
            }

            if (sourceType == typeof(bool) && IsNumericType(targetType))
            {
                return ConvertBooleanToNumeric(value, targetType);
            }

            // Handle GUID conversions
            if (targetType == typeof(Guid) && sourceType == typeof(string))
            {
                return Guid.TryParse((string)value, out var guid) ? guid : Guid.Empty;
            }

            if (sourceType == typeof(Guid) && targetType == typeof(string))
            {
                return value.ToString();
            }

            // Use TypeConverter as fallback
            try
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter.CanConvertFrom(sourceType))
                {
                    return converter.ConvertFrom(value);
                }

                converter = TypeDescriptor.GetConverter(sourceType);
                if (converter.CanConvertTo(targetType))
                {
                    return converter.ConvertTo(value, targetType);
                }
            }
            catch
            {
                // Fallback failed, return default
            }

            // Last resort: try System.Convert
            try
            {
                return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private object? ConvertToEnum(object value, Type enumType)
        {
            try
            {
                if (value is string stringValue)
                {
                    return Enum.Parse(enumType, stringValue, true);
                }

                if (IsNumericType(value.GetType()))
                {
                    return Enum.ToObject(enumType, value);
                }
            }
            catch
            {
                // Return default enum value
            }

            var enumValues = Enum.GetValues(enumType);
            return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
        }

        private object ConvertFromEnum(object enumValue, Type targetType)
        {
            if (targetType == typeof(string))
            {
                return enumValue.ToString() ?? string.Empty;
            }

            if (IsNumericType(targetType))
            {
                var underlyingValue = System.Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()));
                return System.Convert.ChangeType(underlyingValue, targetType);
            }

            return enumValue;
        }

        private object? ConvertFromString(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetDefaultValue(targetType);
            }

            // Handle common types
            if (targetType == typeof(int) && int.TryParse(value, out var intValue))
                return intValue;

            if (targetType == typeof(long) && long.TryParse(value, out var longValue))
                return longValue;

            if (targetType == typeof(double) && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
                return doubleValue;

            if (targetType == typeof(decimal) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                return decimalValue;

            if (targetType == typeof(float) && float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                return floatValue;

            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolValue))
                    return boolValue;
                
                // Handle common boolean string representations
                var lowerValue = value.ToLowerInvariant();
                if (lowerValue == "yes" || lowerValue == "y" || lowerValue == "1" || lowerValue == "on" || lowerValue == "true")
                    return true;
                if (lowerValue == "no" || lowerValue == "n" || lowerValue == "0" || lowerValue == "off" || lowerValue == "false")
                    return false;
            }

            if (targetType == typeof(DateTime) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                return dateValue;

            if (targetType == typeof(Guid) && Guid.TryParse(value, out var guidValue))
                return guidValue;

            if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(value, out var timeSpanValue))
                return timeSpanValue;

            return GetDefaultValue(targetType);
        }

        private string ConvertToString(object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        private object? ConvertNumeric(object value, Type targetType)
        {
            try
            {
                // Check for potential overflow
                var doubleValue = System.Convert.ToDouble(value);
                
                if (targetType == typeof(byte) && (doubleValue < byte.MinValue || doubleValue > byte.MaxValue))
                    return (byte)0;
                
                if (targetType == typeof(sbyte) && (doubleValue < sbyte.MinValue || doubleValue > sbyte.MaxValue))
                    return (sbyte)0;
                
                if (targetType == typeof(short) && (doubleValue < short.MinValue || doubleValue > short.MaxValue))
                    return (short)0;
                
                if (targetType == typeof(ushort) && (doubleValue < ushort.MinValue || doubleValue > ushort.MaxValue))
                    return (ushort)0;
                
                if (targetType == typeof(int) && (doubleValue < int.MinValue || doubleValue > int.MaxValue))
                    return 0;
                
                if (targetType == typeof(uint) && (doubleValue < uint.MinValue || doubleValue > uint.MaxValue))
                    return 0u;
                
                if (targetType == typeof(long) && (doubleValue < long.MinValue || doubleValue > long.MaxValue))
                    return 0L;
                
                if (targetType == typeof(ulong) && (doubleValue < ulong.MinValue || doubleValue > ulong.MaxValue))
                    return 0UL;

                return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private object? ConvertDateTime(object value, Type sourceType, Type targetType)
        {
            try
            {
                if (sourceType == typeof(DateTime) && targetType == typeof(DateTimeOffset))
                {
                    return new DateTimeOffset((DateTime)value);
                }

                if (sourceType == typeof(DateTimeOffset) && targetType == typeof(DateTime))
                {
                    return ((DateTimeOffset)value).DateTime;
                }

                if (sourceType == typeof(string) && targetType == typeof(DateTime))
                {
                    return DateTime.TryParse((string)value, out var result) ? result : DateTime.MinValue;
                }

                if (sourceType == typeof(DateTime) && targetType == typeof(string))
                {
                    return ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                // Fall through to default
            }

            return GetDefaultValue(targetType);
        }

        private bool ConvertToBoolean(object value)
        {
            var stringValue = value.ToString()?.ToLowerInvariant();
            
            return stringValue switch
            {
                "true" or "yes" or "y" or "1" or "on" => true,
                "false" or "no" or "n" or "0" or "off" => false,
                _ when IsNumericType(value.GetType()) => System.Convert.ToDouble(value) != 0,
                _ => false
            };
        }

        private object ConvertBooleanToNumeric(object boolValue, Type targetType)
        {
            var numericValue = (bool)boolValue ? 1 : 0;
            return System.Convert.ChangeType(numericValue, targetType);
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        private static bool IsDateTimeType(Type type)
        {
            return type == typeof(DateTime) || 
                   type == typeof(DateTimeOffset) || 
                   type == typeof(TimeSpan);
        }

        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}