using System.Threading.Tasks;

namespace SeawaveApp.Services;

public interface IFileDialogService
{
    Task<string[]> OpenFilesAsync();
    Task<string?> OpenFolderAsync();
}