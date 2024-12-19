using System;
using System.Collections.Generic;
using System.Threading;
using Hidwizards.IOWrapper.Libraries.DeviceHandlers.Devices;
using Hidwizards.IOWrapper.Libraries.DeviceLibrary;
using HidWizards.IOWrapper.DataTransferObjects;
using Hidwizards.IOWrapper.Libraries.ProviderLogger;
using Np_Provider;
using static Np_Provider.NamedPipeHandler;

namespace Np_Provider
{
    // ToDo: Replace tuples with struct?
    public class NpDeviceHandler : PollingDeviceHandlerBase<InputCommand, (BindingType, int)>
    {
        
        private readonly IInputDeviceLibrary<int> _deviceLibrary;
        private InputCommand _lastCommand;
        private readonly int _namedPipeIndex;
        private readonly Logger _logger = new Logger("Named Pipe DeviceHandler");
        private readonly NamedPipeHandler _namedPipeHandler;

        public NpDeviceHandler(DeviceDescriptor deviceDescriptor, EventHandler<DeviceDescriptor> deviceEmptyHandler, EventHandler<BindModeUpdate> bindModeHandler, IInputDeviceLibrary<int> deviceLibrary)
            : base(deviceDescriptor, deviceEmptyHandler, bindModeHandler)
        {
            _deviceLibrary = deviceLibrary;
            _namedPipeIndex = _deviceLibrary.GetInputDeviceIdentifier(deviceDescriptor);
            _namedPipeHandler = new NamedPipeHandler(_namedPipeIndex, System.IO.Pipes.PipeDirection.In, _logger);
            // All Buttons share one Update Processor
            UpdateProcessors.Add((BindingType.Button, 0), new NpButtonProcessor());
            // LS and RS share one Update Processor
            UpdateProcessors.Add((BindingType.Axis, 0), new NpAxisProcessor());
            // Triggers have their own Update Processor
            UpdateProcessors.Add((BindingType.Axis, 1), new NpTriggerProcessor());
            // DPad directions are buttons, so share one Button Update Processor
            UpdateProcessors.Add((BindingType.POV, 0), new NpButtonProcessor());
        }

        protected override BindingReport GetInputBindingReport(BindingUpdate bindingUpdate)
        {
            return _deviceLibrary.GetInputBindingReport(DeviceDescriptor, bindingUpdate.Binding);
        }

        protected override BindingUpdate[] PreProcessUpdate(InputCommand update)
        {
            var updates = new List<BindingUpdate>();

            // Standard Buttons
            if (update.A.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["A"], SubIndex = 0 }, Value = update.A.Value ? 1 : 0 });
            if (update.B.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["B"], SubIndex = 0 }, Value = update.B.Value ? 1 : 0 });
            if (update.X.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["X"], SubIndex = 0 }, Value = update.X.Value ? 1 : 0 });
            if (update.Y.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["Y"], SubIndex = 0 }, Value = update.Y.Value ? 1 : 0 });
            if (update.LB.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["LB"], SubIndex = 0 }, Value = update.LB.Value ? 1 : 0 });
            if (update.RB.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["RB"], SubIndex = 0 }, Value = update.RB.Value ? 1 : 0 });
            if (update.LS.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["LS"], SubIndex = 0 }, Value = update.LS.Value ? 1 : 0 });
            if (update.RS.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["RS"], SubIndex = 0 }, Value = update.RS.Value ? 1 : 0 });
            if (update.Back.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["Back"], SubIndex = 0 }, Value = update.Back.Value ? 1 : 0 });
            if (update.Start.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Button, Index = Utilities.buttonNames["Start"], SubIndex = 0 }, Value = update.Start.Value ? 1 : 0 });

            // Pov buttons
            int povSubIndexStart = Utilities.povNames["Up"];
            if (update.DpadUp.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.POV, Index = 0, SubIndex = Utilities.povNames["Up"] - povSubIndexStart }, Value = update.DpadUp.Value ? 1 : 0 });
            if (update.DpadRight.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.POV, Index = 0, SubIndex = Utilities.povNames["Right"] - povSubIndexStart }, Value = update.DpadRight.Value ? 1 : 0 });
            if (update.DpadDown.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.POV, Index = 0, SubIndex = Utilities.povNames["Down"] - povSubIndexStart }, Value = update.DpadDown.Value ? 1 : 0 });
            if (update.DpadLeft.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.POV, Index = 0, SubIndex = Utilities.povNames["Left"] - povSubIndexStart }, Value = update.DpadLeft.Value ? 1 : 0 });

            // Axis
            if (update.LeftThumbX.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Axis, Index = Utilities.axisNames["LX"], SubIndex = 0 }, Value = update.LeftThumbX.Value });
            if (update.LeftThumbY.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Axis, Index = Utilities.axisNames["LY"], SubIndex = 0 }, Value = update.LeftThumbY.Value });
            if (update.RightThumbX.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Axis, Index = Utilities.axisNames["RX"], SubIndex = 0 }, Value = update.RightThumbX.Value });
            if (update.RightThumbY.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Axis, Index = Utilities.axisNames["RY"], SubIndex = 0 }, Value = update.RightThumbY.Value });
            if (update.LeftTrigger.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Axis, Index = Utilities.axisNames["LT"], SubIndex = 0 }, Value = update.LeftTrigger.Value });
            if (update.RightTrigger.HasValue)
                updates.Add(new BindingUpdate { Binding = new BindingDescriptor { Type = BindingType.Axis, Index = Utilities.axisNames["RT"], SubIndex = 0 }, Value = update.RightTrigger.Value });

            _lastCommand = update;

            return updates.ToArray();
        }



        protected override (BindingType, int) GetUpdateProcessorKey(BindingDescriptor bindingDescriptor)
        {
            var index = bindingDescriptor.Type == BindingType.Axis && bindingDescriptor.Index > 3 ? 1 : 0;
            return (bindingDescriptor.Type, index);
        }

        protected override void PollThread()
        {
            PollThreadPolling = true;
            _logger.Log($"PollThread started for device {DeviceDescriptor.ToString()}");
            _namedPipeHandler.Start();
            while (PollThreadDesired)
            {
                if (_namedPipeHandler.Commands.Count > 0)
                {
                    InputCommand dequeuedCommand = _namedPipeHandler.Commands.Dequeue();
                    _logger.Log("Got a Named Pipe input command");
                    ProcessUpdate(dequeuedCommand);
                }

                Thread.Sleep(10);
            }
            _namedPipeHandler.Stop();
            _logger.Log($"PollThread ended for device {DeviceDescriptor.ToString()}");
        }

        public override void Dispose()
        {
            if (_namedPipeHandler != null) 
                _namedPipeHandler.Dispose();
        }

    }
}
