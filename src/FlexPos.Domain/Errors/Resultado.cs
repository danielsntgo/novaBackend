namespace FlexPos.Domain.Errors;

public sealed class Resultado<T>
{
    private Resultado(T? valor, ErrorDominio? error)
    {
        Valor = valor;
        Error = error;
    }

    public T? Valor { get; }
    public ErrorDominio? Error { get; }
    public bool EsExitoso => Error is null;

    public static Resultado<T> Exito(T valor) => new(valor, null);

    public static Resultado<T> Fallo(ErrorDominio error) => new(default, error);
}
