namespace UsaAutoPartes.Application.IServicios
{
    public interface IFacturaExtractorServicio
    {
        Task<byte[]> ExtraerProductosAsync(Stream excelStream);
    }
}
