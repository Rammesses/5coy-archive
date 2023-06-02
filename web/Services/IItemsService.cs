using System.Threading.Tasks;

namespace Services;

public interface IItemsService<T>
{
    Task<T[]> GetItems(string sectionRef);
    Task<T[]> GetAllItemsInTree(string sectionRef);
    Task<T> GetItem(string sectionRef, string articleRef);
    Task<T[]> GetItemsByMissionRef(string missionRef);
}
