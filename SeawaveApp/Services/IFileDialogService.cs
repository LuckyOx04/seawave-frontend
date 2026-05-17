using System.Threading.Tasks;

namespace SeawaveApp.Services;

public interface IFileDialogService
{
    Task<string[]> SelectPathsAsync();
}