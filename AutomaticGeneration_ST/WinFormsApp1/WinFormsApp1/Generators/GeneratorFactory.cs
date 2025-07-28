using System;
using System.Collections.Generic;

namespace WinFormsApp1.Generators
{
    public static class GeneratorFactory
    {
        private static readonly Dictionary<string, Func<IPointGenerator>> _generators = 
            new Dictionary<string, Func<IPointGenerator>>(StringComparer.OrdinalIgnoreCase);
        
        static GeneratorFactory()
        {
            RegisterGenerators();
        }
        
        private static void RegisterGenerators()
        {
            _generators["AI"] = () => new AiGenerator();
            _generators["AO"] = () => new AoGenerator();
            _generators["DI"] = () => new DiGenerator();
            _generators["DO"] = () => new DoGenerator();
        }
        
        public static IPointGenerator GetGenerator(string pointType)
        {
            if (string.IsNullOrWhiteSpace(pointType))
            {
                throw new ArgumentException("点位类型不能为空", nameof(pointType));
            }
            
            var normalizedType = pointType.Trim().ToUpper();
            
            if (_generators.TryGetValue(normalizedType, out var generatorFactory))
            {
                return generatorFactory();
            }
            
            throw new NotSupportedException($"不支持的点位类型: {pointType}");
        }
        
        public static bool IsSupported(string pointType)
        {
            if (string.IsNullOrWhiteSpace(pointType))
                return false;
                
            return _generators.ContainsKey(pointType.Trim().ToUpper());
        }
        
        public static string[] GetSupportedTypes()
        {
            return new[] { "AI", "AO", "DI", "DO" };
        }
        
        public static void RegisterGenerator(string pointType, Func<IPointGenerator> generatorFactory)
        {
            if (string.IsNullOrWhiteSpace(pointType))
                throw new ArgumentException("点位类型不能为空", nameof(pointType));
                
            if (generatorFactory == null)
                throw new ArgumentNullException(nameof(generatorFactory));
                
            _generators[pointType.Trim().ToUpper()] = generatorFactory;
        }
    }
}