﻿using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace CeremonyClient.Dialogs.NotifyMessage
{
    public class NotifyMessageManager
    {
        private readonly object _syncRoot = new object();
        protected int MaxPopup { get; set; }
        protected List<AnimatedLocation> DisplayLocations { get; set; }
        protected ConcurrentQueue<NotifyMessage> QueuedMessages { get; set; }
        protected NotifyMessageViewModel[] DisplayMessages { get; set; }
        private CancellationTokenSource _cts;
        private bool _isStarted;

        private delegate void MethodInvoker();

        public NotifyMessageManager(double windowStartPosX, double windowStartPosY, double screenWidth, double screenHeight, double popupWidth, double popupHeight)
        {
            MaxPopup = 3;
            DisplayLocations = new List<AnimatedLocation>(MaxPopup);
            DisplayMessages = new NotifyMessageViewModel[MaxPopup];
            QueuedMessages = new ConcurrentQueue<NotifyMessage>();

            double left = (screenWidth - popupWidth) / 2;
            double top = windowStartPosY + screenHeight - 20;

            for (int index = 0; index < MaxPopup; index++)
            {
                if (index == 0)
                {
                    DisplayLocations.Add(new AnimatedLocation(windowStartPosX + left, windowStartPosX + left, top, top - popupHeight));
                }
                else
                {
                    var previousLocation = DisplayLocations[index - 1];
                    DisplayLocations.Add(new AnimatedLocation(
                        windowStartPosX + left, windowStartPosX + left, previousLocation.ToTop, previousLocation.ToTop - popupHeight));
                }
            }
            _isStarted = false;
        }

        public void ResetNotifyMessageLocations(double windowStartPosX, double windowStartPosY, double windowWidth, double windowHeight, double popupWidth, double popupHeight)
        {
            DisplayLocations.Clear();

            double left = (windowWidth - popupWidth) / 2;
            double top = windowStartPosY + windowHeight - 20;

            for (int index = 0; index < MaxPopup; index++)
            {
                if (index == 0)
                {
                    DisplayLocations.Add(new AnimatedLocation(windowStartPosX + left, windowStartPosX + left, top, top - popupHeight));
                }
                else
                {
                    var previousLocation = DisplayLocations[index - 1];
                    DisplayLocations.Add(new AnimatedLocation(
                        windowStartPosX + left, windowStartPosX + left, previousLocation.ToTop, previousLocation.ToTop - popupHeight));
                }
            }
        }

        public bool IsStarted
        {
            get { return _isStarted; }
        }

        public void Start()
        {
            lock (_syncRoot)
            {
                if (!_isStarted)
                {
                    _cts = new CancellationTokenSource();
                    StartService(_cts.Token);
                    _isStarted = true;
                }
            }
        }

        private Task StartService(CancellationToken cancellationToken)
        {
            try
            {
                var dispatcher = Application.Current.MainWindow.Dispatcher;

                return Task.Factory.StartNew(async () =>
                {
                    do
                    {
                        // Gets the next display location in the screen
                        int nextLocation = FindNextLocation();

                        if (nextLocation > -1)
                        {
                            NotifyMessage msg = null;
                            //  Retrieve the message from the queue
                            if (QueuedMessages.TryDequeue(out msg))
                            {
                                //  construct a View Model and binds it to the Popup Window
                                var viewModel = new NotifyMessageViewModel(msg,
                                    DisplayLocations[nextLocation],
                                    () => DisplayMessages[nextLocation] = null);    // Free the display location when the popup window is closed
                                DisplayMessages[nextLocation] = viewModel;

                                //  Use Application.Current.MainWindow.Dispatcher to switch back to the UI Thread to create popup window
                                await dispatcher.BeginInvoke(
                                    new MethodInvoker(() =>
                                    {
                                        var window = new AlertDialog()
                                        {
                                            Owner = Application.Current.MainWindow,
                                            DataContext = viewModel,
                                            ShowInTaskbar = false
                                        };
                                        window.Show();
                                    }), DispatcherPriority.Background);
                            }
                        }
                        await Task.Delay(1000);

                    } while (QueuedMessages.Count > 0 && !cancellationToken.IsCancellationRequested);

                    Stop();
                });
            }
            catch
            {
                return null;
            }
            
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                if (_isStarted)
                {
                    StopService();
                    _isStarted = false;
                }
            }
        }

        private void StopService()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        private int FindNextLocation()
        {
            for (int index = 0; index < DisplayMessages.Length; index++)
            {
                if (DisplayMessages[index] == null)
                    return index;
            }
            return -1;
        }

        public void EnqueueMessage(NotifyMessage msg)
        {
            QueuedMessages.Enqueue(msg);
            Start();
        }
    }
}
