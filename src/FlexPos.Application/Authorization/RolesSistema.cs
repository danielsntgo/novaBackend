namespace FlexPos.Application.Authorization;

public static class RolesSistema
{
    public const string Administrador = "Administrador";
    public const string Recepcionista = "Recepcionista";
}

public static class PoliticasAutorizacion
{
    public const string SoloAdministrador = "SoloAdministrador";
    public const string OperacionDiaria = "OperacionDiaria";
    public const string UsuarioAutenticado = "UsuarioAutenticado";
}
