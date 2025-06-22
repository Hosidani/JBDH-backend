using JBDH.Models.Kanban;
using System.Threading.Tasks;

namespace JBDH.Services.ListService
{
    public interface IListService
    {
        Task<KanbanList> CreateAsync(KanbanList list);
        Task<KanbanList> EditAsync(int id, KanbanList list);
        Task DeleteAsync(int id);
        Task<KanbanList?> GetAsync(int id);
        Task<IEnumerable<KanbanList>> GetListsByBoardIdAsync(int boardId);
        // Add more methods as needed (e.g., GetAll, etc.)
    }
} 