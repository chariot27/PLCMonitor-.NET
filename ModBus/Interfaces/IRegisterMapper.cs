using System.Collections.Generic;

namespace ModBus.Interfaces
{
    public interface IRegisterMapper
    {
        IDictionary<string, ushort> GetMappings();
        string MapAddressToName(ushort address);
        ushort MapNameToAddress(string name);
    }
}
