namespace JewerlySystem.Modules.Contracts.Domain;

public enum ContractState
{
    Registrado = 1,
    Asignado = 2,
    Recibido = 3,
    Entregado = 4
}

public enum PaymentType
{
    Contrato = 1,
    Factura = 2
}

public enum PaymentMethod
{
    EFECTIVO = 1,
    TARJETA_CREDITO = 2,
    TARJETA_DEBITO = 3,
    TRANSFERENCIA = 4,
    ORO = 5
}
