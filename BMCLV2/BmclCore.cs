using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BMCLV2.Auth;
using BMCLV2.Game;
using BMCLV2.I18N;
using BMCLV2.Mirrors;
using BMCLV2.Plugin;
using BMCLV2.Windows;

namespace BMCLV2
{
  public static class BmclCore
  {
    public static readonly Config Config;
    public static readonly GameManager GameManager;
    public static readonly string BmclVersion;
    public static readonly NotiIcon NIcon = new NotiIcon();
    public static FrmMain MainWindow = null;
    public static readonly Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;
    public static gameinfo GameInfo;
    public static readonly Dictionary<string, object> Language = new Dictionary<string, object>();
    public static readonly string BaseDirectory = Environment.CurrentDirectory + '\\';
    public static readonly string MinecraftDirectory = Path.Combine(BaseDirectory, ".minecraft");
    public static readonly string LibrariesDirectory = Path.Combine(MinecraftDirectory, "libraries");
    public static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "BMCL");
    private static readonly Application ThisApplication = Application.Current;
    private static readonly string Cfgfile = Path.Combine(BaseDirectory, "bmcl.xml");
    public static readonly MirrorManager MirrorManager = new MirrorManager();
    public static readonly PluginManager PluginManager = new PluginManager();
    public static readonly AuthManager AuthManager = new AuthManager();
    public static readonly string Platform = "windows";

    static BmclCore()
    {
      BmclVersion = Application.ResourceAssembly.GetName().Version.ToString();
      Logger.Log("BMCLNG Ver." + BmclVersion + "正在启动");
      if (!Directory.Exists(MinecraftDirectory))
      {
        Logger.Log($"{MinecraftDirectory}不存在，正在创建");
        Directory.CreateDirectory(MinecraftDirectory);
      }

      if (!Directory.Exists(TempDirectory)) Directory.CreateDirectory(TempDirectory);

      GameManager = new GameManager();
      Config = Config.Load(Cfgfile);
      Config.Passwd = Config.Passwd ?? Array.Empty<byte>();

      Logger.Log($"加载{Cfgfile}文件");
      Logger.Log(Config);
      LangManager.LoadLanguage();
      LangManager.ChangeLanguage(Config.Lang);
      Logger.Log("加载默认配置");
      if (!Directory.Exists(BaseDirectory + ".minecraft")) Directory.CreateDirectory(BaseDirectory + ".minecraft");

      if (Config.Javaw == "autosearch") Config.Javaw = Config.GetJavaDir();

      if (Config.Javaxmx == "autosearch")
        Config.Javaxmx = (Config.GetMemory() / 4).ToString(CultureInfo.InvariantCulture);

      LangManager.UseLanguage(Config.Lang);
      if (!App.SkipPlugin) PluginManager.LoadOldAuthPlugin(LangManager.GetLangFromResource("LangName"));
      ServicePointManager.DefaultConnectionLimit = int.MaxValue;
      ReleaseCheck();
    }

    private static async Task ReleaseCheck()
    {
      if (Config.Report)
      {
        var reporter = new Report();
        reporter.RunBackGround();
      }

      if (Config.CheckUpdate)
      {
        var updateChecker = new UpdateChecker();
        var updateInfo = await updateChecker.Run();
        if (updateInfo == null) return;
        var a = MessageBox.Show(MainWindow, updateInfo.Description, "更新", MessageBoxButton.OKCancel,
          MessageBoxImage.Information);
        if (a == MessageBoxResult.OK)
        {
          var updater = new FrmUpdater(updateInfo.LastBuild, updateInfo.Url);
          updater.ShowDialog();
        }
      }
    }

    public static void Invoke(Delegate invoke, object[] argObjects = null)
    {
      Dispatcher.Invoke(invoke, argObjects);
    }


    public static void Halt(int code = 0)
    {
      ThisApplication.Shutdown(code);
    }

    public static void SingleInstance(Window window)
    {
      ThreadPool.RegisterWaitForSingleObject(App.ProgramStarted, OnAnotherProgramStarted, window, -1, false);
    }

    private static void OnAnotherProgramStarted(object state, bool timedout)
    {
      var window = MainWindow;
      NIcon.ShowBalloonTip(2000, LangManager.GetLangFromResource("BMCLHiddenInfo"));
      if (window != null) Dispatcher.Invoke(window.Show);
    }

    public static void Notify(string message)
    {
      NIcon?.ShowBalloonTip(5, message);
    }
  }
}
