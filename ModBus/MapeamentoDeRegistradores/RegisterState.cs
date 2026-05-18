namespace ModBus.MapeamentoDeRegistradores
{
    public class RegisterState
    {
        public ushort Address { get; set; }
        public string Name { get; set; } = string.Empty;
        public ushort Value { get; set; }
        public string Description { get; set; } = string.Empty;
        public RegisterType Type { get; set; }
    }

    public enum RegisterType
    {
        Input,
        Holding,
        Coil,
        DiscreteInput
    }
}
