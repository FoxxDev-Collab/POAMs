using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public interface IExportService
{
    byte[] ExportToXacta(IEnumerable<POAM> poams, SystemInfo system);
    byte[] ExportSingleToXacta(POAM poam);
}
