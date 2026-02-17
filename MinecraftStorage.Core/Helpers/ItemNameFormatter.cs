using System.Globalization;

namespace MinecraftStorage.Core.Helpers
{
    public static class ItemNameFormatter
    {
        public static string Format(string internalName)
        {
            string name = internalName;

            if (name.Contains(":"))
                name = name.Split(':')[1];

            name = name.Replace("_", " ");

            return CultureInfo
                .CurrentCulture
                .TextInfo
                .ToTitleCase(name);
        }
    }
}