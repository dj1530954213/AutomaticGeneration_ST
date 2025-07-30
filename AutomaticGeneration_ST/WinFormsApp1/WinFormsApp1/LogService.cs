using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LogService
    {
        private RichTextBox? _logTextBox;
        private static LogService? _instance;
        private static readonly object _lock = new object();
        private LogLevel _currentLogLevel = LogLevel.Info;

        private LogService() { }

        public static LogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new LogService();
                    }
                }
                return _instance;
            }
        }

        public void Initialize(RichTextBox logTextBox)
        {
            _logTextBox = logTextBox;
        }

        public void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
        }

        public LogLevel GetLogLevel()
        {
            return _currentLogLevel;
        }

        public void LogInfo(string message)
        {
            if (_currentLogLevel <= LogLevel.Info)
                Log("信息", message, Color.Black);
        }

        public void LogSuccess(string message)
        {
            if (_currentLogLevel <= LogLevel.Info)
                Log("成功", message, Color.Green);
        }

        public void LogWarning(string message)
        {
            if (_currentLogLevel <= LogLevel.Warning)
                Log("警告", message, Color.Orange);
        }

        public void LogError(string message)
        {
            if (_currentLogLevel <= LogLevel.Error)
                Log("错误", message, Color.Red);
        }

        public void LogDebug(string message)
        {
            if (_currentLogLevel <= LogLevel.Debug)
                Log("调试", message, Color.Gray);
        }

        private void Log(string level, string message, Color color)
        {
            if (_logTextBox == null) return;

            if (_logTextBox.InvokeRequired)
            {
                _logTextBox.Invoke(new Action(() => Log(level, message, color)));
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] [{level}] {message}\n";

            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
            _logTextBox.SelectionColor = color;
            _logTextBox.AppendText(logMessage);
            _logTextBox.SelectionColor = _logTextBox.ForeColor;
            _logTextBox.ScrollToCaret();
        }

        public void Clear()
        {
            if (_logTextBox != null)
            {
                if (_logTextBox.InvokeRequired)
                {
                    _logTextBox.Invoke(new Action(() => _logTextBox.Clear()));
                }
                else
                {
                    _logTextBox.Clear();
                }
            }
        }
    }
}