namespace WinFormsApp1.Models
{
    /// <summary>
    /// TCP状态量通讯点位
    /// </summary>
    public class TcpDigitalPoint : TcpCommunicationPoint
    {
        /// <summary>
        /// 初始状态
        /// </summary>
        public bool InitialState { get; set; } = false;

        /// <summary>
        /// 位地址（在寄存器中的位位置）
        /// </summary>
        public int BitAddress { get; set; } = 0;

        /// <summary>
        /// 寄存器地址
        /// </summary>
        public string RegisterAddress { get; set; } = "";

        /// <summary>
        /// 状态反转（是否需要取反）
        /// </summary>
        public bool StateInvert { get; set; } = false;

        /// <summary>
        /// 状态描述映射
        /// </summary>
        public string TrueStateDescription { get; set; } = "ON";

        /// <summary>
        /// 假状态描述
        /// </summary>
        public string FalseStateDescription { get; set; } = "OFF";

        public override bool IsValid()
        {
            return base.IsValid() && 
                   DataType?.ToUpper() == "BOOL";
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        public string GetStateDescription(bool state)
        {
            bool actualState = StateInvert ? !state : state;
            return actualState ? TrueStateDescription : FalseStateDescription;
        }
    }
}