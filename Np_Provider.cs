using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using HidWizards.IOWrapper.ProviderInterface;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Hidwizards.IOWrapper.Libraries.DeviceHandlers.Devices;
using Hidwizards.IOWrapper.Libraries.DeviceLibrary;
using Hidwizards.IOWrapper.Libraries.ProviderLogger;
using Hidwizards.IOWrapper.Libraries.SubscriptionHandlers;
using HidWizards.IOWrapper.DataTransferObjects;
using HidWizards.IOWrapper.ProviderInterface.Interfaces;
using static Np_Provider.NamedPipeHandler;
using Np_Provider.DeviceLibrary;

namespace Np_Provider
{
    [Export(typeof(IProvider))]
    public class Np_Provider : IInputProvider, IBindModeProvider
    {
        private readonly ConcurrentDictionary<DeviceDescriptor, IDeviceHandler<InputCommand>> _activeDevices
            = new ConcurrentDictionary<DeviceDescriptor, IDeviceHandler<InputCommand>>();
        private readonly IInputDeviceLibrary<int> _deviceLibrary;
        private readonly object _lockObj = new object();  // When changing mode (Bind / Sub) or adding / removing devices, lock this object
        private Action<ProviderDescriptor, DeviceDescriptor, BindingReport, short> _bindModeCallback;


        public bool IsLive { get { return isLive; } }
        private bool isLive = true;

        private readonly Logger _logger;

        bool disposed;

        public Np_Provider()
        {
            _logger = new Logger(ProviderName);
            _deviceLibrary = new NpDeviceLibrary(new ProviderDescriptor { ProviderName = ProviderName });

        }

        public void SetDetectionMode(DetectionMode detectionMode, DeviceDescriptor deviceDescriptor, Action<ProviderDescriptor, DeviceDescriptor, BindingReport, short> callback = null)
        {
            lock (_lockObj)
            {
                var deviceExists = _activeDevices.TryGetValue(deviceDescriptor, out var deviceHandler);
                if (detectionMode == DetectionMode.Subscription)
                {
                    // Subscription Mode
                    if (!deviceExists) return;
                    deviceHandler.SetDetectionMode(DetectionMode.Subscription);
                }
                else
                {
                    // Bind Mode
                    if (!deviceExists)
                    {
                        deviceHandler = new NpDeviceHandler(deviceDescriptor, DeviceEmptyHandler, BindModeHandler, _deviceLibrary);
                        deviceHandler.Init();
                        _activeDevices.TryAdd(deviceDescriptor, deviceHandler);
                    }
                    _bindModeCallback = callback;
                    deviceHandler.SetDetectionMode(DetectionMode.Bind);
                }
            }
        }

        private void BindModeHandler(object sender, BindModeUpdate e)
        {
            _bindModeCallback?.Invoke(new ProviderDescriptor { ProviderName = ProviderName }, e.Device, e.Binding, e.Value);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                foreach (var device in _activeDevices.Values)
                {
                    device.Dispose();
                }
            }
            disposed = true;
            _logger.Log("Disposed");
        }

        #region IProvider Members
        public string ProviderName { get { return typeof(Np_Provider).Namespace; } }

        public ProviderReport GetInputList()
        {
            return _deviceLibrary.GetInputList();
        }

        public DeviceReport GetInputDeviceReport(DeviceDescriptor deviceDescriptor)
        {
            return _deviceLibrary.GetInputDeviceReport(deviceDescriptor);
        }

        public bool SubscribeInput(InputSubscriptionRequest subReq)
        {
            _logger.Log("attempt to subscribe us...");
            lock (_lockObj)
            {
                if (!_activeDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
                {
                    deviceHandler = new NpDeviceHandler(subReq.DeviceDescriptor, DeviceEmptyHandler, BindModeHandler, _deviceLibrary);
                    deviceHandler.Init();
                    _activeDevices.TryAdd(subReq.DeviceDescriptor, deviceHandler);
                }
                deviceHandler.SubscribeInput(subReq);
                return true;
            }
        }

        public bool UnsubscribeInput(InputSubscriptionRequest subReq)
        {
            lock (_lockObj)
            {
                if (_activeDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
                {
                    deviceHandler.UnsubscribeInput(subReq);
                }
                return true;
            }
        }

        public void RefreshLiveState()
        {
            // Built-in API, take no action
        }

        public void RefreshDevices()
        {
            _deviceLibrary.RefreshConnectedDevices();
        }

        private void DeviceEmptyHandler(object sender, DeviceDescriptor deviceDescriptor)
        {
            if (_activeDevices.TryRemove(deviceDescriptor, out var device))
            {
                device.Dispose();
            }
            else
            {
                throw new Exception($"Remove device {deviceDescriptor.ToString()} failed");
            }
        }
        #endregion
    }
}
