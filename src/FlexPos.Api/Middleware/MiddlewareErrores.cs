using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Api.Middleware;

public sealed class MiddlewareErrores(
    RequestDelegate siguiente,
    ILogger<MiddlewareErrores> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await siguiente(contexto);
        }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Conflicto de concurrencia al procesar {Metodo} {Ruta}.",
                contexto.Request.Method,
                contexto.Request.Path);

            if (contexto.Response.HasStarted)
            {
                throw;
            }

            contexto.Response.Clear();
            contexto.Response.StatusCode = StatusCodes.Status409Conflict;
            await contexto.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "about:blank",
                Title = "El registro cambió en otra operación. Vuelva a cargarlo e intente de nuevo.",
                Status = StatusCodes.Status409Conflict,
                Instance = contexto.Request.Path,
                Extensions = { ["traceId"] = contexto.TraceIdentifier }
            });
        }
        catch (Exception excepcion)
        {
            logger.LogError(excepcion, "Error no controlado al procesar {Metodo} {Ruta}.",
                contexto.Request.Method,
                contexto.Request.Path);

            if (contexto.Response.HasStarted)
            {
                throw;
            }

            contexto.Response.Clear();
            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await contexto.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "about:blank",
                Title = "Ocurrió un error interno.",
                Status = StatusCodes.Status500InternalServerError,
                Instance = contexto.Request.Path,
                Extensions = { ["traceId"] = contexto.TraceIdentifier }
            });
        }
    }
}
