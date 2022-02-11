﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml;

using MicaForEveryone.Interfaces;
using MicaForEveryone.UI;
using MicaForEveryone.UI.ViewModels;
using MicaForEveryone.Win32;
using MicaForEveryone.Win32.PInvoke;
using MicaForEveryone.Xaml;

namespace MicaForEveryone.Views
{
    internal class MainWindow : XamlWindow
    {
        public const string OpenSettingsMessage = "MicaForEveryone_OpenSettings";

        private const uint WM_APP_NOTIFYICON = Macros.WM_APP + 1;

        private readonly NotifyIcon _notifyIcon;
        private uint _openSettingsMessage;

        public MainWindow() : this(new())
        {
        }

        private MainWindow(TrayIconView view) : base(view)
        {
            Style = WindowStyles.WS_POPUPWINDOW;
            StyleEx = WindowStylesEx.WS_EX_TOPMOST;

            Destroy += MainWindow_Destroy;

            var resources = ResourceLoader.GetForCurrentView();
            Title = resources.GetString("AppName");

            _notifyIcon = new NotifyIcon
            {
                CallbackMessage = WM_APP_NOTIFYICON,
                Id = 0,
                Title = Title,
            };

            _notifyIcon.Click += NotifyIcon_Click;
            _notifyIcon.ContextMenu += NotifyIcon_ContextMenu;
            _notifyIcon.OpenPopup += NotifyIcon_OpenPopup;
            _notifyIcon.ClosePopup += NotifyIcon_ClosePopup;

            view.ViewModel = ViewModel;
            view.Loaded += View_Loaded;
        }

        public ITrayIconViewModel ViewModel { get; } =
            Program.CurrentApp.Container.GetService<ITrayIconViewModel>();

        public override void Activate()
        {
            base.Activate();

            _openSettingsMessage = RegisterWindowMessage(OpenSettingsMessage);

            _notifyIcon.Parent = Handle;
            _notifyIcon.Activate();
            _notifyIcon.ShowNotifyIcon();
        }

        public override void Dispose()
        {
            _notifyIcon.Dispose();
            base.Dispose();
        }

        // event handlers
        private void MainWindow_Destroy(object sender, WndProcEventArgs e)
        {
            _notifyIcon.HideNotifyIcon();
        }

        private void NotifyIcon_Click(object sender, TrayIconClickEventArgs e)
        {
            ViewModel.OpenSettingsCommand.Execute(null);
        }

        private void NotifyIcon_ContextMenu(object sender, TrayIconClickEventArgs e)
        {
            var notifyIconRect = _notifyIcon.GetRect();
            ViewModel.ShowContextMenu(e.Point, notifyIconRect);
        }

        private void NotifyIcon_OpenPopup(object sender, EventArgs e)
        {
            var notifyIconRect = _notifyIcon.GetRect();
            ViewModel.ShowTooltipPopup(notifyIconRect);
        }

        private void NotifyIcon_ClosePopup(object sender, EventArgs e)
        {
            ViewModel.HideTooltipPopup();
        }

        private async void View_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.InitializeAsync(this);
            }
#if DEBUG
            catch 
            {
                throw;
            }
#else
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var title = ResourceLoader.GetForCurrentView().GetString("AppInitializationError/Title");
                    var dialogService = Program.CurrentApp.Container.GetService<IDialogService>();
                    dialogService.ShowErrorDialog(null, title, ex, 500, 320).Destroy += (sender, args) =>
                    {
                        Program.CurrentApp.Exit();
                    };
                });
            }
#endif
        }

        protected override IntPtr WndProc(IntPtr hwnd, uint umsg, IntPtr wParam, IntPtr lParam)
        {
            if (umsg == _openSettingsMessage)
            {
                ViewModel.OpenSettingsCommand.Execute(null);
                return IntPtr.Zero;
            }
            return base.WndProc(hwnd, umsg, wParam, lParam);
        }
    }
}
