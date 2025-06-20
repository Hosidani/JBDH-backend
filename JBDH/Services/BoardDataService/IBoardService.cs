using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.Kanban;

namespace JBDH.Services.BoardDataService
{
    public interface IBoardService
    {
        Task<IEnumerable<Board>> GetAllAsync();
        Task<Board?> GetAsync(int id);
        Task CreateAsync(Board board);
        Task UpdateAsync(Board board);
        Task DeleteAsync(int id);
    }
} 