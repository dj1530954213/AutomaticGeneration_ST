// //NEED DELETE: 多语言/语言管理（UI增强），非核心
// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Text.Json;
// using System.Windows.Forms;

// namespace WinFormsApp1.Config
// {
//     public enum SupportedLanguage
//     {
//         Chinese = 0,    // 中文 (默认)
//         English = 1,    // 英语
//         Japanese = 2    // 日语 (预留)
//     }

//     public static class LanguageManager
//     {
//         private static SupportedLanguage currentLanguage = SupportedLanguage.Chinese;
//         private static Dictionary<string, Dictionary<SupportedLanguage, string>> translations = new();
//         private static readonly string ConfigPath = Path.Combine(
//             Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//             "STGenerator", "language.json");

//         // 事件：语言改变时触发
//         public static event Action<SupportedLanguage>? LanguageChanged;

//         static LanguageManager()
//         {
//             InitializeDefaultTranslations();
//             LoadLanguageSettings();
//         }

//         public static SupportedLanguage CurrentLanguage 
//         { 
//             get => currentLanguage; 
//             private set => currentLanguage = value; 
//         }

//         private static void InitializeDefaultTranslations()
//         {
//             // UI控件文本
//             translations["AppTitle"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "ST脚本自动生成器",
//                 [SupportedLanguage.English] = "ST Script Auto Generator",
//                 [SupportedLanguage.Japanese] = "STスクリプト自動生成器"
//             };

//             translations["MenuFile"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "文件(&F)",
//                 [SupportedLanguage.English] = "&File",
//                 [SupportedLanguage.Japanese] = "ファイル(&F)"
//             };

//             translations["MenuEdit"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "编辑(&E)",
//                 [SupportedLanguage.English] = "&Edit",
//                 [SupportedLanguage.Japanese] = "編集(&E)"
//             };

//             translations["MenuView"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "视图(&V)",
//                 [SupportedLanguage.English] = "&View",
//                 [SupportedLanguage.Japanese] = "表示(&V)"
//             };

//             translations["MenuTools"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "工具(&T)",
//                 [SupportedLanguage.English] = "&Tools",
//                 [SupportedLanguage.Japanese] = "ツール(&T)"
//             };

//             translations["MenuHelp"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "帮助(&H)",
//                 [SupportedLanguage.English] = "&Help",
//                 [SupportedLanguage.Japanese] = "ヘルプ(&H)"
//             };

//             // 按钮文本
//             translations["ButtonUpload"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "📁 上传点表",
//                 [SupportedLanguage.English] = "📁 Upload Points",
//                 [SupportedLanguage.Japanese] = "📁 ポイント表アップロード"
//             };

//             translations["ButtonExport"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "💾 导出结果",
//                 [SupportedLanguage.English] = "💾 Export Results",
//                 [SupportedLanguage.Japanese] = "💾 結果エクスポート"
//             };

//             // 状态消息
//             translations["StatusReady"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "就绪",
//                 [SupportedLanguage.English] = "Ready",
//                 [SupportedLanguage.Japanese] = "準備完了"
//             };

//             translations["StatusProcessing"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "处理中...",
//                 [SupportedLanguage.English] = "Processing...",
//                 [SupportedLanguage.Japanese] = "処理中..."
//             };

//             // 标签页标题
//             translations["TabCodePreview"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "📄 代码预览",
//                 [SupportedLanguage.English] = "📄 Code Preview",
//                 [SupportedLanguage.Japanese] = "📄 コードプレビュー"
//             };

//             translations["TabStatistics"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "📊 统计信息",
//                 [SupportedLanguage.English] = "📊 Statistics",
//                 [SupportedLanguage.Japanese] = "📊 統計情報"
//             };

//             translations["TabFileInfo"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "📁 文件信息",
//                 [SupportedLanguage.English] = "📁 File Info",
//                 [SupportedLanguage.Japanese] = "📁 ファイル情報"
//             };

//             translations["TabTemplateInfo"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "🎨 模板信息",
//                 [SupportedLanguage.English] = "🎨 Template Info",
//                 [SupportedLanguage.Japanese] = "🎨 テンプレート情報"
//             };

//             // 主题相关
//             translations["ThemeLight"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "浅色主题(&L)",
//                 [SupportedLanguage.English] = "&Light Theme",
//                 [SupportedLanguage.Japanese] = "ライトテーマ(&L)"
//             };

//             translations["ThemeDark"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "深色主题(&D)",
//                 [SupportedLanguage.English] = "&Dark Theme",
//                 [SupportedLanguage.Japanese] = "ダークテーマ(&D)"
//             };

//             translations["ThemeSystem"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "跟随系统(&S)",
//                 [SupportedLanguage.English] = "&Follow System",
//                 [SupportedLanguage.Japanese] = "システムに従う(&S)"
//             };

//             // 对话框标题
//             translations["DialogSettings"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "设置",
//                 [SupportedLanguage.English] = "Settings",
//                 [SupportedLanguage.Japanese] = "設定"
//             };

//             translations["DialogAbout"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "关于 ST脚本自动生成器",
//                 [SupportedLanguage.English] = "About ST Script Auto Generator",
//                 [SupportedLanguage.Japanese] = "STスクリプト自動生成器について"
//             };

//             // 常用按钮
//             translations["ButtonOK"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "确定",
//                 [SupportedLanguage.English] = "OK",
//                 [SupportedLanguage.Japanese] = "OK"
//             };

//             translations["ButtonCancel"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "取消",
//                 [SupportedLanguage.English] = "Cancel",
//                 [SupportedLanguage.Japanese] = "キャンセル"
//             };

//             translations["ButtonApply"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "应用",
//                 [SupportedLanguage.English] = "Apply",
//                 [SupportedLanguage.Japanese] = "適用"
//             };

//             // 日志消息
//             translations["LogAppStarted"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "ST脚本自动生成器 v2.0 已启动",
//                 [SupportedLanguage.English] = "ST Script Auto Generator v2.0 started",
//                 [SupportedLanguage.Japanese] = "STスクリプト自動生成器 v2.0 開始"
//             };

//             translations["LogSupportedTypes"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "支持的点位类型: AI, AO, DI, DO",
//                 [SupportedLanguage.English] = "Supported point types: AI, AO, DI, DO",
//                 [SupportedLanguage.Japanese] = "サポートされているポイントタイプ: AI, AO, DI, DO"
//             };

//             // 错误消息
//             translations["ErrorFileNotFound"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "文件未找到",
//                 [SupportedLanguage.English] = "File not found",
//                 [SupportedLanguage.Japanese] = "ファイルが見つかりません"
//             };

//             translations["ErrorProcessingFile"] = new()
//             {
//                 [SupportedLanguage.Chinese] = "处理文件时出错",
//                 [SupportedLanguage.English] = "Error processing file",
//                 [SupportedLanguage.Japanese] = "ファイル処理中にエラー"
//             };
//         }

//         public static string GetText(string key)
//         {
//             if (translations.ContainsKey(key) && translations[key].ContainsKey(currentLanguage))
//             {
//                 return translations[key][currentLanguage];
//             }

//             // 如果当前语言没有翻译，尝试使用中文
//             if (currentLanguage != SupportedLanguage.Chinese &&
//                 translations.ContainsKey(key) && 
//                 translations[key].ContainsKey(SupportedLanguage.Chinese))
//             {
//                 return translations[key][SupportedLanguage.Chinese];
//             }

//             // 如果都没有，返回键名
//             return key;
//         }

//         public static void SetLanguage(SupportedLanguage language)
//         {
//             if (currentLanguage != language)
//             {
//                 currentLanguage = language;
//                 SaveLanguageSettings();
//                 LanguageChanged?.Invoke(language);
//             }
//         }

//         public static void ApplyLanguageToForm(Form form)
//         {
//             if (form == null) return;

//             try
//             {
//                 // 递归应用语言到所有控件
//                 ApplyLanguageToControl(form);
//             }
//             catch (Exception ex)
//             {
//                 System.Diagnostics.Debug.WriteLine($"应用语言时出错: {ex.Message}");
//             }
//         }

//         private static void ApplyLanguageToControl(Control control)
//         {
//             // 根据控件名称或类型应用翻译
//             var controlName = control.Name;
//             var translationKey = GetTranslationKeyForControl(control, controlName);

//             if (!string.IsNullOrEmpty(translationKey))
//             {
//                 var translatedText = GetText(translationKey);
//                 if (!string.IsNullOrEmpty(translatedText) && translatedText != translationKey)
//                 {
//                     control.Text = translatedText;
//                 }
//             }

//             // 递归处理子控件
//             foreach (Control child in control.Controls)
//             {
//                 ApplyLanguageToControl(child);
//             }
//         }

//         private static string GetTranslationKeyForControl(Control control, string? controlName)
//         {
//             // 根据控件名称匹配翻译键
//             if (!string.IsNullOrEmpty(controlName))
//             {
//                 if (controlName.Contains("upload", StringComparison.OrdinalIgnoreCase))
//                     return "ButtonUpload";
//                 if (controlName.Contains("export", StringComparison.OrdinalIgnoreCase))
//                     return "ButtonExport";
//                 if (controlName.Contains("fileMenu", StringComparison.OrdinalIgnoreCase))
//                     return "MenuFile";
//                 if (controlName.Contains("editMenu", StringComparison.OrdinalIgnoreCase))
//                     return "MenuEdit";
//                 if (controlName.Contains("viewMenu", StringComparison.OrdinalIgnoreCase))
//                     return "MenuView";
//                 if (controlName.Contains("toolsMenu", StringComparison.OrdinalIgnoreCase))
//                     return "MenuTools";
//                 if (controlName.Contains("helpMenu", StringComparison.OrdinalIgnoreCase))
//                     return "MenuHelp";
//             }

//             // 根据控件类型和文本内容推断
//             if (control is Form form && form.Text.Contains("ST"))
//                 return "AppTitle";

//             return "";
//         }

//         public static List<(SupportedLanguage Language, string DisplayName)> GetSupportedLanguages()
//         {
//             return new List<(SupportedLanguage, string)>
//             {
//                 (SupportedLanguage.Chinese, "中文简体"),
//                 (SupportedLanguage.English, "English"),
//                 (SupportedLanguage.Japanese, "日本語")
//             };
//         }

//         public static string GetLanguageDisplayName(SupportedLanguage language)
//         {
//             return language switch
//             {
//                 SupportedLanguage.Chinese => "中文简体",
//                 SupportedLanguage.English => "English", 
//                 SupportedLanguage.Japanese => "日本語",
//                 _ => "中文简体"
//             };
//         }

//         public static SupportedLanguage DetectSystemLanguage()
//         {
//             try
//             {
//                 var culture = CultureInfo.CurrentUICulture;
//                 var languageCode = culture.TwoLetterISOLanguageName.ToLower();

//                 return languageCode switch
//                 {
//                     "en" => SupportedLanguage.English,
//                     "ja" => SupportedLanguage.Japanese,
//                     "zh" => SupportedLanguage.Chinese,
//                     _ => SupportedLanguage.Chinese
//                 };
//             }
//             catch
//             {
//                 return SupportedLanguage.Chinese;
//             }
//         }

//         private static void LoadLanguageSettings()
//         {
//             try
//             {
//                 if (File.Exists(ConfigPath))
//                 {
//                     var json = File.ReadAllText(ConfigPath);
//                     var settings = JsonSerializer.Deserialize<LanguageSettings>(json);
//                     if (settings != null && Enum.IsDefined(typeof(SupportedLanguage), settings.Language))
//                     {
//                         currentLanguage = settings.Language;
//                     }
//                 }
//                 else
//                 {
//                     // 首次运行，检测系统语言
//                     currentLanguage = DetectSystemLanguage();
//                     SaveLanguageSettings();
//                 }
//             }
//             catch (Exception ex)
//             {
//                 System.Diagnostics.Debug.WriteLine($"加载语言设置时出错: {ex.Message}");
//                 currentLanguage = SupportedLanguage.Chinese;
//             }
//         }

//         private static void SaveLanguageSettings()
//         {
//             try
//             {
//                 var configDir = Path.GetDirectoryName(ConfigPath);
//                 if (!string.IsNullOrEmpty(configDir))
//                 {
//                     Directory.CreateDirectory(configDir);
//                 }

//                 var settings = new LanguageSettings { Language = currentLanguage };
//                 var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
//                 { 
//                     WriteIndented = true 
//                 });
//                 File.WriteAllText(ConfigPath, json);
//             }
//             catch (Exception ex)
//             {
//                 System.Diagnostics.Debug.WriteLine($"保存语言设置时出错: {ex.Message}");
//             }
//         }

//         public static void AddCustomTranslation(string key, SupportedLanguage language, string text)
//         {
//             if (!translations.ContainsKey(key))
//             {
//                 translations[key] = new Dictionary<SupportedLanguage, string>();
//             }
//             translations[key][language] = text;
//         }

//         private class LanguageSettings
//         {
//             public SupportedLanguage Language { get; set; } = SupportedLanguage.Chinese;
//         }
//     }
// }
