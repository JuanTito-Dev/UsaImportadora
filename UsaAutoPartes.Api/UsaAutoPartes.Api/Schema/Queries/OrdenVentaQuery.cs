using HotChocolate.Authorization;
using System.Security.Claims;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;
using UsaAutoPartes.Domain.Enum.VentaEnums;

namespace UsaAutoPartes.Api.Schema.Queries
{
    [ExtendObjectType("Query")]
    public class OrdenVentaQuery
    {
        // Cajero: sus propias órdenes
        [Authorize(Roles = new[] { UsuarioRoles.Cajero, UsuarioRoles.Admin })]
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<OrdenVenta> MisOrdenes([Service] IOrdenVentaRepositorio _db, ClaimsPrincipal claims)
        {
            var userId = Guid.Parse(claims.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return _db.OrdenQuery().Where(x => x.Id_Cajero == userId);
        }

        // Almacenero: órdenes pendientes disponibles para aceptar
        [Authorize(Roles = new[] { UsuarioRoles.Almacenero, UsuarioRoles.Admin })]
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<OrdenVenta> OrdenesPendientes([Service] IOrdenVentaRepositorio _db)
        {
            return _db.OrdenQuery().Where(x => x.Estado == EstadosOrden.Pendiente);
        }

        // Almacenero: órdenes que aceptó
        [Authorize(Roles = new[] { UsuarioRoles.Almacenero, UsuarioRoles.Admin })]
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<OrdenVenta> MisOrdenesAlmacen([Service] IOrdenVentaRepositorio _db, ClaimsPrincipal claims)
        {
            var userId = Guid.Parse(claims.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return _db.OrdenQuery().Where(x => x.Id_Almacenero == userId);
        }

        // Operador: órdenes listas para escaneo
        [Authorize(Roles = new[] { UsuarioRoles.Operador, UsuarioRoles.Admin })]
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<OrdenVenta> OrdenesListas([Service] IOrdenVentaRepositorio _db)
        {
            return _db.OrdenQuery().Where(x => x.Estado == EstadosOrden.Lista);
        }

        // Admin: todas las órdenes
        [Authorize(Roles = new[] { UsuarioRoles.Admin })]
        [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
        [UseProjection]
        [UseSorting]
        [UseFiltering]
        public IQueryable<OrdenVenta> TodasOrdenes([Service] IOrdenVentaRepositorio _db)
        {
            return _db.OrdenQuery();
        }
    }
}
