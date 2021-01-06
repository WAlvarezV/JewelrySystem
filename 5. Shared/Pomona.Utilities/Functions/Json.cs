using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Pomona.Utilities.Functions
{
    internal static class Json<T> where T : class
    {
        public static List<T> GetSeed()
        {
            var type = typeof(T);
            var entityData = File.ReadAllText($"{ AppDomain.CurrentDomain.BaseDirectory}/Context/Seeds/{type.Name}SeedData.json", Encoding.UTF8);
            return JsonSerializer.Deserialize<List<T>>(entityData);
        }
    }
}
