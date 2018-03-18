
namespace Spike.SDK.General.Enums
{
    using System;
    using System.ComponentModel;

    public static class EnumWorker
    {
        public static string ToDescription<TEnum>(this TEnum enumValue)
        {
            ValidateTypeAsEnum<TEnum>();
            return GetEnumDescription((Enum) (object) (enumValue));
        }

        public static TEnum ParseEnum<TEnum>(string value)
        {
            ValidateTypeAsEnum<TEnum>();
            return (TEnum) Enum.Parse(typeof(TEnum), value, true);
        }

        public static T ParseEnum<T>(string value, T defaultEnum)
        {
            try
            {
                return ParseEnum<T>(value);
            }
            catch (Exception)
            {
                return defaultEnum;
            }
        }

        private static string GetEnumDescription(Enum value)
        {
            var enumItem = value.GetType().GetField(value.ToString());

            var attributes = (DescriptionAttribute[]) enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static TEnum ParseEnumFromDescription<TEnum>(string enumDescription, TEnum defaultEnum)
        {
            ValidateTypeAsEnum<TEnum>();
            foreach (Enum enumItem in Enum.GetValues(typeof(TEnum)))
            {
                if (!string.Equals(enumDescription,
                    GetEnumDescription(enumItem),
                    StringComparison.CurrentCultureIgnoreCase)) continue;

                object result = enumItem;
                return (TEnum) result;
            }

            return defaultEnum;
        }

        private static void ValidateTypeAsEnum<TEnum>()
        {
            var theType = typeof(TEnum);

            if (!theType.IsEnum)
            {
                throw new InvalidCastException(
                    $"Enum Worker opperations can only be applied to Enums not [{theType}]");
            }
        }
    }
}