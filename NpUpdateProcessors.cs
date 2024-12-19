using Hidwizards.IOWrapper.Libraries.DeviceHandlers.Updates;
using HidWizards.IOWrapper.DataTransferObjects;

namespace Np_Provider
{
    public class NpButtonProcessor : IUpdateProcessor
    {
        public BindingUpdate[] Process(BindingUpdate update)
        {
            return new[] { update };
        }
    }

    public class NpAxisProcessor : IUpdateProcessor
    {
        public BindingUpdate[] Process(BindingUpdate update)
        {
            return new[] { update };
        }
    }

    public class NpTriggerProcessor : IUpdateProcessor
    {
        public BindingUpdate[] Process(BindingUpdate update)
        {
            update.Value = (update.Value * 257) - 32768;
            return new[] { update };
        }
    }
}
