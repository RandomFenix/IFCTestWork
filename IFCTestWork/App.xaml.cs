using NLog;
using System;
using System.Windows;

namespace IFCTestWork
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Log.Info("Запуск приложения");

            DispatcherUnhandledException += (error, args) =>
            {
                Log.Error(args.Exception, "Ошибка");
                MessageBox.Show($"Произошла ошибка:\n{args.Exception.Message}\n\n",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (error, args) =>
            {
                Log.Fatal(args.ExceptionObject as Exception, "Критическое необработанное исключение");
                LogManager.Flush();
            };

            //TaskScheduler.UnobservedTaskException += (error, args) =>
            //{
            //    Log.Error(args.Exception, "Необработанное исключение в Task");
            //    args.SetObserved();
            //};
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Info("Выход");
            LogManager.Shutdown();
            base.OnExit(e);
        }
    }
}
