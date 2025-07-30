namespace AutomaticGeneration_ST.Services.Generation.Interfaces
{
    /// <summary>
    /// 用于生成Modbus RTU (485) 通讯配置ST代码的专用接口。
    /// </summary>
    public interface IModbusRtuConfigGenerator : ICommunicationGenerator
    {
        // 此接口目前无需添加额外成员。
        // 它继承了ICommunicationGenerator的所有功能，其存在主要是为了
        // 在未来进行依赖注入时，能够清晰地区分RTU和TCP的实现。
    }
}