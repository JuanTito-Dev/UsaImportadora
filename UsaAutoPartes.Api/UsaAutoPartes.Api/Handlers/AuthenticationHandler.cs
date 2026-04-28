using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
using UsaAutoPartes.Application.Exceptions.Autentication;
using UsaAutoPartes.Application.Exceptions.AuthenticationExceptions;
using UsaAutoPartes.Application.Exceptions.DataBaseException;
using UsaAutoPartes.Application.Exceptions.GenericExceptions;
using UsaAutoPartes.Domain.Entities.IdentityDb;

namespace UsaAutoPartes.Api.Handlers
{
    public class AuthenticationHandler : IExceptionHandler
    {
        private readonly ILogger<AuthenticationHandler> _logger;
        public AuthenticationHandler(ILogger<AuthenticationHandler> logger)
        {
            _logger = logger;
        }
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is DbUpdateException dbEx &&
                dbEx.InnerException is PostgresException pgEx)
            {
                exception = pgEx.SqlState switch
                {
                    "23505" => new UniqueConstraintException(ResolverUnico(pgEx.ConstraintName)),
                    "23503" => new ForeignKeyException(ResolverFK(pgEx.ConstraintName)),
                    _ => exception
                };
            }

            var (statuscode, message) = GetExceptionDetails(exception);

            _logger.LogError(exception, exception.Message);

            httpContext.Response.StatusCode = (int)statuscode;
            await httpContext.Response.WriteAsJsonAsync(new { message = message }, cancellationToken);
    
            return true;
        }

        private (HttpStatusCode statusCode, string message) GetExceptionDetails(Exception exception)
        {
            return exception switch
            {
                LoginFailException => (HttpStatusCode.Unauthorized, exception.Message),
                RefreshTokenFailException => (HttpStatusCode.BadRequest, exception.Message),
                RegistroTransaccionFailException => (HttpStatusCode.BadRequest, exception.Message),
                UsuarioExisteException => (HttpStatusCode.Conflict, exception.Message),
                EntidadNoEncontradaException => (HttpStatusCode.Conflict, exception.Message),
                UniqueConstraintException => (HttpStatusCode.Conflict, exception.Message),
                ForeignKeyException => (HttpStatusCode.BadRequest, exception.Message),
                _ => (HttpStatusCode.InternalServerError, $"Ocurrio un error inesperado. {exception.Message}")
            };
        }

        private string ResolverUnico(string? constraintName)
        {
            return constraintName switch
            {
                "IX_Producto_Codigo" => "El código ya existe.",
                "IX_Producto_CodigoAux" => "El código auxiliar ya existe.",
                "IX_Producto_CodigoAux2" => "El código auxiliar 2 ya existe.",
                "IX_Importacion_Codigo" => "El código de importación ya existe.",
                "IX_Proveedor_Nombre" => "El nombre del proveedor ya existe.",
                "IX_Usuario_Email" => "El email del usuario ya existe.",
                "IX_Usuario_UserName" => "El nombre de usuario ya existe.",
                "Id_nombre_descuento" => "El nombre de descuento ya existe",
                _ => "Ya existe un registro con esos datos."
            };
        }

        private string ResolverFK(string? constraintName)
        {
            return constraintName switch
            {
                "FK_Importacion_Proveedor" => "Este proveedor tiene importaciones asociadas y no puede ser eliminado.",
                _ => "La referencia indicada no existe."
            };
        }
    }
}
