using System.Threading.Tasks;

using Models;

namespace Services;

public interface ISectionService<T>
{
    Task<Section<T>[]> GetSections();
    Task<Section<T>> GetSection(string sectionRef);
    Task<Section<T>> GetSectionForItem(string articleRef);
}
