using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Hidwizards.IOWrapper.Libraries.DeviceLibrary;
using HidWizards.IOWrapper.DataTransferObjects;

namespace Np_Provider.DeviceLibrary
{
    public class NpDeviceLibrary : IInputDeviceLibrary<int>
    {
        private readonly ProviderDescriptor _providerDescriptor;
        private static DeviceReportNode _buttonInfo;
        private static DeviceReportNode _axisInfo;
        private static DeviceReportNode _povInfo;
        private static List<DeviceReport> _deviceReports;
        private ConcurrentDictionary<BindingDescriptor, BindingReport> _bindingReports;

        public NpDeviceLibrary(ProviderDescriptor providerDescriptor)
        {
            _providerDescriptor = providerDescriptor;
            BuildInputList();
            BuildDeviceList();
        }

        public int GetInputDeviceIdentifier(DeviceDescriptor deviceDescriptor)
        {
            return deviceDescriptor.DeviceInstance;
        }

        public void RefreshConnectedDevices()
        {
            // Do nothing for XI
        }

        public ProviderReport GetInputList()
        {
            var providerReport = new ProviderReport
            {
                Title = "Named Pipe Reader",
                Description = "Reads inputs from a named pipe",
                API = "Named Pipe",
                ProviderDescriptor = _providerDescriptor,
                Devices = _deviceReports
            };

            return providerReport;
        }

        public DeviceReport GetInputDeviceReport(DeviceDescriptor deviceDescriptor)
        {
            var id = deviceDescriptor.DeviceInstance;
            return new DeviceReport
            {
                DeviceName = "Named Pipe Reader " + (id + 1),
                DeviceDescriptor = new DeviceDescriptor
                {
                    DeviceHandle = "npr",
                    DeviceInstance = id
                },
                Nodes = {_buttonInfo, _axisInfo, _povInfo}
                //ButtonCount = 11,
                //ButtonList = buttonInfo,
                //AxisList = axisInfo,
            };
        }

        public BindingReport GetInputBindingReport(DeviceDescriptor deviceDescriptor, BindingDescriptor bindingDescriptor)
        {
            return _bindingReports[bindingDescriptor];
        }

        private void BuildInputList()
        {
            _buttonInfo = new DeviceReportNode
            {
                Title = "Buttons"
            };
            _bindingReports = new ConcurrentDictionary<BindingDescriptor, BindingReport>();
            foreach(var button in Utilities.buttonNames)
            {
                var bd = new BindingDescriptor
                {
                    Index = button.Value,
                    Type = BindingType.Button
                };
                var name = button.Key;
                var br = new BindingReport
                {
                    Title = name,
                    Path = $"Button: {name}",
                    Category = BindingCategory.Momentary,
                    BindingDescriptor = bd
                };
                _bindingReports.TryAdd(bd, br);
                _buttonInfo.Bindings.Add(br);
            }

            _axisInfo = new DeviceReportNode
            {
                Title = "Axes"
            };
            foreach (var axes in Utilities.axisNames)
            {
                var bd = new BindingDescriptor
                {
                    Index = axes.Value,
                    Type = BindingType.Axis
                };
                var name = axes.Key;
                var br = new BindingReport
                {
                    Title = name,
                    Path = $"Axis: {name}",
                    Category = (bd.Index < 4 ? BindingCategory.Signed : BindingCategory.Unsigned),
                    BindingDescriptor = bd
                };
                _bindingReports.TryAdd(bd, br);
                _axisInfo.Bindings.Add(br);
            }

            _povInfo = new DeviceReportNode
            {
                Title = "DPad"
            };
            foreach (var pov in Utilities.povNames)
            {
                var bd = new BindingDescriptor
                {
                    Index = 0,
                    SubIndex = pov.Value - Utilities.buttonNames.Count,
                    Type = BindingType.POV
                };
                var name = pov.Key;
                var br = new BindingReport
                {
                    Title = name,
                    Path = $"DPad: {name}",
                    Category = BindingCategory.Momentary,
                    BindingDescriptor = bd
                };
                _bindingReports.TryAdd(bd, br);
                _povInfo.Bindings.Add(br);
            }

        }

        private static void BuildDeviceList()
        {
            // Add 4 selectable named pipes for connection
            _deviceReports = new List<DeviceReport>();
            for (var i = 0; i < 4; i++)
            {
                _deviceReports.Add(BuildNpDevice(i));
            }
        }

        private static DeviceReport BuildNpDevice(int id)
        {
            return new DeviceReport
            {
                DeviceName = "Named Pipe Reader " + (id + 1),
                DeviceDescriptor = new DeviceDescriptor
                {
                    DeviceHandle = "npr",
                    DeviceInstance = id
                },
                Nodes = { _buttonInfo, _axisInfo, _povInfo }
                //ButtonCount = 11,
                //ButtonList = buttonInfo,
                //AxisList = axisInfo,
            };
        }
    }
}
