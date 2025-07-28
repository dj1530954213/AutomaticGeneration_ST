using System.Collections.Generic;

namespace WinFormsApp1.Generators
{
    public interface IPointGenerator
    {
        string Generate(Dictionary<string, object> row);
        string PointType { get; }
        bool CanGenerate(Dictionary<string, object> row);
    }
}